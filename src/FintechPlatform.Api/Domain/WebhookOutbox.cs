namespace FintechPlatform.Api.Domain;

public enum WebhookStatus
{
    Pending,
    Delivered,
    DeadLetter
}

public sealed class WebhookOutbox
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string EventType { get; set; }
    public Guid AggregateId { get; set; }
    public required string DestinationUrl { get; set; }
    public required string PayloadJson { get; set; }
    public WebhookStatus Status { get; set; } = WebhookStatus.Pending;
    public int AttemptCount { get; set; }
    public DateTimeOffset NextAttemptAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DeliveredAt { get; set; }
    public string? LastError { get; set; }
}
