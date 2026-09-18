using FintechPlatform.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FintechPlatform.Api.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        AppDbContext db,
        string demoPassword,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(demoPassword) || demoPassword.Length < 12)
            throw new ArgumentException("Demo password must be at least 12 characters.", nameof(demoPassword));

        await db.Database.EnsureCreatedAsync(cancellationToken);

        if (await db.Users.AnyAsync(cancellationToken)) return;

        var admin = new AppUser
        {
            Email = "admin@demo.local",
            PasswordHash = string.Empty,
            Role = Roles.Admin
        };

        var hasher = new PasswordHasher<AppUser>();
        admin.PasswordHash = hasher.HashPassword(admin, demoPassword);

        var user = new AppUser
        {
            Email = "user@demo.local",
            PasswordHash = string.Empty,
            Role = Roles.User
        };
        user.PasswordHash = hasher.HashPassword(user, demoPassword);

        db.Users.AddRange(admin, user);
        db.Wallets.AddRange(
            new Wallet { OwnerId = user.Id, Currency = "NGN", Balance = 250_000m },
            new Wallet { OwnerId = admin.Id, Currency = "NGN", Balance = 100_000m }
        );

        await db.SaveChangesAsync(cancellationToken);
    }
}
