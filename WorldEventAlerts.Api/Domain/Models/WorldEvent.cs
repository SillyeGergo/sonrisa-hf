using System.Collections.Generic;

namespace WorldEventAlerts.Api.Domain.Models;

public sealed record WorldEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string EventType { get; init; } = string.Empty;

    public string Source { get; init; } = string.Empty;

    public string PayloadJson { get; init; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}