using System.Collections.Generic;

namespace WorldEventAlerts.Api.Domain.Models;

public sealed record AlertRule
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; init; } = string.Empty;

    public bool IsActive { get; init; } = true;

    public string MatchExpression { get; init; } = string.Empty;

    public IReadOnlyCollection<string> NotificationChannels { get; init; } = Array.Empty<string>();

    public string? Description { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAtUtc { get; init; }
}