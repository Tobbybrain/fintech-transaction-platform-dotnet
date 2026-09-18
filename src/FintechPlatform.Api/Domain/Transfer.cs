namespace FintechPlatform.Api.Domain;

public enum TransferStatus
{
    Pending,
    Completed,
    Failed
}

public sealed class Transfer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Reference { get; set; }
    public required string IdempotencyKey { get; set; }
    public Guid FromWalletId { get; set; }
    public Guid ToWalletId { get; set; }
    public required string Currency { get; set; }
    public decimal Amount { get; set; }
    public TransferStatus Status { get; set; } = TransferStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
}
