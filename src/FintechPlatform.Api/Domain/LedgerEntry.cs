namespace FintechPlatform.Api.Domain;

public enum LedgerEntryType
{
    Debit,
    Credit
}

public sealed class LedgerEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TransferId { get; set; }
    public Guid WalletId { get; set; }
    public LedgerEntryType Type { get; set; }
    public required string Currency { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
