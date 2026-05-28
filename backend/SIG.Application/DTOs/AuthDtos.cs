namespace SIG.Application.DTOs;

public record LoginRequest(string Email, string Password);
public record LoginResponse(string AccessToken, string RefreshToken, UsuarioBriefDto User);
public record RefreshRequest(string RefreshToken);
public record RefreshResponse(string AccessToken, string RefreshToken);
public record LogoutRequest(string? RefreshToken);
public record UsuarioBriefDto(int Id, string Nombre, string Apellidos, string Email, string[] Roles);
