namespace FintechPlatform.Api.Dtos;

public sealed record LoginRequest(string Email, string Password);
public sealed record TokenResponse(string AccessToken, DateTimeOffset ExpiresAt);
