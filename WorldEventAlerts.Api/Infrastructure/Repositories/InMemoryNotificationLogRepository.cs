using System.Collections.Concurrent;
using WorldEventAlerts.Api.Domain.Abstractions;
using WorldEventAlerts.Api.Domain.Models;

namespace WorldEventAlerts.Api.Infrastructure.Repositories;

public sealed class InMemoryNotificationLogRepository : INotificationLogRepository
{
    private readonly ConcurrentQueue<NotificationLog> _notificationLogs = new();
    private readonly object _sync = new();
    private readonly int _maxEntries;

    public InMemoryNotificationLogRepository(int maxEntries = 500)
    {
        if (maxEntries <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxEntries));
        }

        _maxEntries = maxEntries;
    }

    public Task AddAsync(NotificationLog notificationLog, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentNullException.ThrowIfNull(notificationLog);

        lock (_sync)
        {
            _notificationLogs.Enqueue(notificationLog);

            while (_notificationLogs.Count > _maxEntries && _notificationLogs.TryDequeue(out _))
            {
            }
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<NotificationLog>> GetRecentAsync(int take, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (take <= 0)
        {
            return Task.FromResult<IReadOnlyCollection<NotificationLog>>(Array.Empty<NotificationLog>());
        }

        NotificationLog[] snapshot;

        lock (_sync)
        {
            snapshot = _notificationLogs.ToArray();
        }

        IReadOnlyCollection<NotificationLog> recent = snapshot
            .Reverse()
            .Take(take)
            .ToArray();

        return Task.FromResult(recent);
    }
}