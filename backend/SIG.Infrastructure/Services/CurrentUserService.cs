using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SIG.Application.Interfaces.Services;

namespace SIG.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;
    public CurrentUserService(IHttpContextAccessor accessor) { _accessor = accessor; }

    public int UserId
    {
        get
        {
            var v = _accessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(v))
                throw new InvalidOperationException("User ID not found in context");
            if (!int.TryParse(v, out var id))
                throw new InvalidOperationException("User ID is not a valid integer");
            return id;
        }
    }

    public string? Email => _accessor.HttpContext?.User.FindFirst(ClaimTypes.Email)?.Value;

    public IReadOnlyList<string> Roles =>
        _accessor.HttpContext?.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList()
        ?? (IReadOnlyList<string>)Array.Empty<string>();

    public string? Ip => _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public bool IsInRole(string role) => Roles.Contains(role);
    public bool IsInAnyRole(params string[] roles) => Roles.Any(r => roles.Contains(r));
}
