namespace WorldEventAlerts.Api.Domain.Models;

public sealed record NotificationLog
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid AlertRuleId { get; init; }

    public Guid WorldEventId { get; init; }

    public string ProviderName { get; init; } = string.Empty;

    public string ChannelName { get; init; } = string.Empty;

    public bool Succeeded { get; init; }

    public string? ErrorMessage { get; init; }

    public DateTimeOffset AttemptedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? CompletedAtUtc { get; init; }
}