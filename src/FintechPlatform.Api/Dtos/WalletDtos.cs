namespace FintechPlatform.Api.Dtos;

public sealed record WalletResponse(Guid Id, Guid OwnerId, string Currency, decimal Balance, long Version);
public sealed record LedgerEntryResponse(Guid Id, Guid TransferId, string Type, string Currency, decimal Amount, decimal BalanceAfter, DateTimeOffset CreatedAt);
