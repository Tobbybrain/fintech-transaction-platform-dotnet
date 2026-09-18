using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FintechPlatform.Api.Data;
using FintechPlatform.Api.Domain;
using FintechPlatform.Api.Dtos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace FintechPlatform.Api.Services;

public sealed class AuthService(AppDbContext db, IConfiguration configuration)
{
    public async Task<TokenResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var normalized = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email.ToLower() == normalized, cancellationToken);
        if (user is null) return null;

        var hasher = new PasswordHasher<AppUser>();
        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed) return null;

        var jwt = configuration.GetSection("Jwt");
        var key = jwt["Key"] ?? throw new InvalidOperationException("Jwt:Key is missing.");
        var issuer = jwt["Issuer"] ?? "FintechPlatform.Api";
        var audience = jwt["Audience"] ?? "FintechPlatform.Client";
        var minutes = int.TryParse(jwt["AccessTokenMinutes"], out var parsed) ? parsed : 15;
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(minutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256));

        return new TokenResponse(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
