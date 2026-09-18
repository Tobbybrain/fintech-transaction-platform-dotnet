using System.Data;
using System.Text.Json;
using FintechPlatform.Api.Data;
using FintechPlatform.Api.Domain;
using FintechPlatform.Api.Dtos;
using FintechPlatform.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FintechPlatform.Api.Services;

public sealed class TransferService(AppDbContext db)
{
    public async Task<Transfer> CreateAsync(
        CreateTransferRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("Idempotency-Key header is required.", nameof(idempotencyKey));
        if (idempotencyKey.Length > 128)
            throw new ArgumentException("Idempotency-Key must be 128 characters or fewer.", nameof(idempotencyKey));
        if (request.FromWalletId == request.ToWalletId)
            throw new InvalidOperationException("Source and destination wallets must be different.");

        var amount = MoneyRules.RequireValidAmount(request.Amount);

        var previous = await db.Transfers.AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
        if (previous is not null) return previous;

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            previous = await db.Transfers
                .SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
            if (previous is not null)
            {
                await tx.RollbackAsync(cancellationToken);
                return previous;
            }

            var firstId = request.FromWalletId.CompareTo(request.ToWalletId) < 0 ? request.FromWalletId : request.ToWalletId;
            var secondId = firstId == request.FromWalletId ? request.ToWalletId : request.FromWalletId;

            var lockedWallets = await db.Wallets
                .FromSqlInterpolated($"SELECT * FROM \"Wallets\" WHERE \"Id\" = {firstId} OR \"Id\" = {secondId} ORDER BY \"Id\" FOR UPDATE")
                .ToListAsync(cancellationToken);

            var from = lockedWallets.SingleOrDefault(x => x.Id == request.FromWalletId)
                ?? throw new KeyNotFoundException("Source wallet not found.");
            var to = lockedWallets.SingleOrDefault(x => x.Id == request.ToWalletId)
                ?? throw new KeyNotFoundException("Destination wallet not found.");

            if (!string.Equals(from.Currency, to.Currency, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Wallet currencies do not match.");
            if (from.Balance < amount)
                throw new InvalidOperationException("Insufficient funds.");

            from.Balance -= amount;
            to.Balance += amount;
            from.Version++;
            to.Version++;
            from.UpdatedAt = DateTimeOffset.UtcNow;
            to.UpdatedAt = DateTimeOffset.UtcNow;

            var transfer = new Transfer
            {
                Reference = $"TRF-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..32],
                IdempotencyKey = idempotencyKey,
                FromWalletId = from.Id,
                ToWalletId = to.Id,
                Currency = from.Currency,
                Amount = amount,
                Status = TransferStatus.Completed,
                CompletedAt = DateTimeOffset.UtcNow
            };

            db.Transfers.Add(transfer);
            db.LedgerEntries.AddRange(
                new LedgerEntry
                {
                    TransferId = transfer.Id,
                    WalletId = from.Id,
                    Type = LedgerEntryType.Debit,
                    Currency = from.Currency,
                    Amount = amount,
                    BalanceAfter = from.Balance
                },
                new LedgerEntry
                {
                    TransferId = transfer.Id,
                    WalletId = to.Id,
                    Type = LedgerEntryType.Credit,
                    Currency = to.Currency,
                    Amount = amount,
                    BalanceAfter = to.Balance
                });

            if (!string.IsNullOrWhiteSpace(request.WebhookUrl))
            {
                var payload = JsonSerializer.Serialize(new
                {
                    id = transfer.Id,
                    reference = transfer.Reference,
                    eventType = "transfer.completed",
                    amount = transfer.Amount,
                    currency = transfer.Currency,
                    status = transfer.Status.ToString(),
                    occurredAt = transfer.CompletedAt
                });

                db.WebhookOutbox.Add(new WebhookOutbox
                {
                    EventType = "transfer.completed",
                    AggregateId = transfer.Id,
                    DestinationUrl = request.WebhookUrl,
                    PayloadJson = payload
                });
            }

            await db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return transfer;
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync(cancellationToken);
            db.ChangeTracker.Clear();

            var winner = await db.Transfers.AsNoTracking()
                .SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
            if (winner is not null) return winner;
            throw;
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
