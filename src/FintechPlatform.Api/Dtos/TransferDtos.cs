namespace FintechPlatform.Api.Dtos;

public sealed record CreateTransferRequest(Guid FromWalletId, Guid ToWalletId, decimal Amount, string? WebhookUrl);
public sealed record TransferResponse(Guid Id, string Reference, string IdempotencyKey, Guid FromWalletId, Guid ToWalletId, string Currency, decimal Amount, string Status, DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt);
