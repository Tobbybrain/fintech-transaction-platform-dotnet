using FintechPlatform.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace FintechPlatform.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<Transfer> Transfers => Set<Transfer>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<WebhookOutbox> WebhookOutbox => Set<WebhookOutbox>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.Email).HasMaxLength(320);
            entity.Property(x => x.Role).HasMaxLength(32);
        });

        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.OwnerId, x.Currency }).IsUnique();
            entity.Property(x => x.Currency).HasMaxLength(3);
            entity.Property(x => x.Balance).HasPrecision(18, 2);
            entity.Property(x => x.Version).IsConcurrencyToken();
        });

        modelBuilder.Entity<Transfer>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Reference).IsUnique();
            entity.HasIndex(x => x.IdempotencyKey).IsUnique();
            entity.Property(x => x.Reference).HasMaxLength(64);
            entity.Property(x => x.IdempotencyKey).HasMaxLength(128);
            entity.Property(x => x.Currency).HasMaxLength(3);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        });

        modelBuilder.Entity<LedgerEntry>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.WalletId, x.CreatedAt });
            entity.HasIndex(x => x.TransferId);
            entity.Property(x => x.Currency).HasMaxLength(3);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.BalanceAfter).HasPrecision(18, 2);
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(16);
        });

        modelBuilder.Entity<WebhookOutbox>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.Status, x.NextAttemptAt });
            entity.Property(x => x.EventType).HasMaxLength(100);
            entity.Property(x => x.DestinationUrl).HasMaxLength(2048);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        });
    }
}
