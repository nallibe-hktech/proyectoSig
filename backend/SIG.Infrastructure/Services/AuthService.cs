using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Interfaces.Services;
using SIG.Application.Services;
using SIG.Domain.Entities;
using SIG.Domain.Enums;
using SIG.Domain.Exceptions;

namespace SIG.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly IRefreshTokenRepository _refreshRepo;
    private readonly IAuditLogRepository _auditRepo;
    private readonly IPasswordHasher _hasher;
    private readonly IConfiguration _config;

    public AuthService(IUserRepository userRepo, IRefreshTokenRepository refreshRepo, IAuditLogRepository auditRepo, IPasswordHasher hasher, IConfiguration config)
    {
        _userRepo = userRepo;
        _refreshRepo = refreshRepo;
        _auditRepo = auditRepo;
        _hasher = hasher;
        _config = config;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest req, string? ip, CancellationToken ct)
    {
        var user = await _userRepo.GetByEmailAsync(req.Email, ct)
                   ?? throw new UnauthorizedException();
        if (user.Estado != EstadoUsuario.Activo)
            throw new UnauthorizedException("Usuario inactivo");
        if (!_hasher.Verify(req.Password, user.PasswordHash))
            throw new UnauthorizedException();

        var roles = user.UserRoles.Select(ur => ur.Role?.Nombre ?? "").Where(r => !string.IsNullOrEmpty(r)).ToArray();
        var access = GenerateAccessToken(user, roles);
        var refresh = GenerateRefreshTokenString();
        var refreshHash = Sha256(refresh);

        var refreshEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshHash,
            ExpiresAt = DateTime.UtcNow.AddDays(_config.GetValue<int>("JwtSettings:RefreshExpirationDays", 7)),
            CreatedAt = DateTime.UtcNow,
            Ip = ip
        };
        await _refreshRepo.AddAsync(refreshEntity, ct);

        await _auditRepo.AddAsync(new AuditLog
        {
            UserId = user.Id,
            EntityType = nameof(User),
            EntityId = user.Id.ToString(),
            Action = AuditAction.Login,
            Timestamp = DateTime.UtcNow,
            Ip = ip
        }, ct);
        await _refreshRepo.SaveChangesAsync(ct);

        var brief = new UsuarioBriefDto(user.Id, user.Nombre, user.Apellidos, user.Email, roles);
        return new LoginResponse(access, refresh, brief);
    }

    public async Task<RefreshResponse> RefreshAsync(RefreshRequest req, string? ip, CancellationToken ct)
    {
        var hash = Sha256(req.RefreshToken);
        var existing = await _refreshRepo.GetByHashAsync(hash, ct)
                       ?? throw new UnauthorizedException("Refresh token inválido");
        if (existing.RevokedAt != null || existing.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedException("Refresh token expirado o revocado");

        // rotación: revocar el actual y emitir uno nuevo
        existing.RevokedAt = DateTime.UtcNow;
        var newRefresh = GenerateRefreshTokenString();
        var newRefreshHash = Sha256(newRefresh);
        var user = existing.User;
        var roles = user.UserRoles.Select(ur => ur.Role?.Nombre ?? "").Where(r => !string.IsNullOrEmpty(r)).ToArray();
        var access = GenerateAccessToken(user, roles);

        await _refreshRepo.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = newRefreshHash,
            ExpiresAt = DateTime.UtcNow.AddDays(_config.GetValue<int>("JwtSettings:RefreshExpirationDays", 7)),
            CreatedAt = DateTime.UtcNow,
            Ip = ip
        }, ct);
        await _refreshRepo.SaveChangesAsync(ct);

        return new RefreshResponse(access, newRefresh);
    }

    public async Task LogoutAsync(int userId, string? refreshToken, CancellationToken ct)
    {
        await _refreshRepo.RevokeAllByUserAsync(userId, ct);
        await _auditRepo.AddAsync(new AuditLog
        {
            UserId = userId,
            EntityType = nameof(User),
            EntityId = userId.ToString(),
            Action = AuditAction.Logout,
            Timestamp = DateTime.UtcNow
        }, ct);
        await _auditRepo.SaveChangesAsync(ct);
    }

    public async Task<UsuarioBriefDto> GetMeAsync(int userId, CancellationToken ct)
    {
        var u = await _userRepo.GetByIdAsync(userId, ct) ?? throw new UnauthorizedException();
        var roles = u.UserRoles.Select(ur => ur.Role?.Nombre ?? "").Where(r => !string.IsNullOrEmpty(r)).ToArray();
        return new UsuarioBriefDto(u.Id, u.Nombre, u.Apellidos, u.Email, roles);
    }

    private string GenerateAccessToken(User user, string[] roles)
    {
        var key = _config["JwtSettings:SigningKey"]
                  ?? throw new InvalidOperationException("JwtSettings:SigningKey no configurada.");
        var issuer = _config["JwtSettings:Issuer"] ?? "sig-plataforma";
        var audience = _config["JwtSettings:Audience"] ?? "sig-plataforma";
        var minutes = _config.GetValue<int>("JwtSettings:AccessExpirationMinutes", 30);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        foreach (var r in roles) claims.Add(new Claim(ClaimTypes.Role, r));
        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer, audience, claims,
            expires: DateTime.UtcNow.AddMinutes(minutes), signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshTokenString()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string Sha256(string s)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
        return Convert.ToHexString(bytes);
    }
}
