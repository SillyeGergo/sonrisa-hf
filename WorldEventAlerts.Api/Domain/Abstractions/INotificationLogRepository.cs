using WorldEventAlerts.Api.Domain.Models;

namespace WorldEventAlerts.Api.Domain.Abstractions;

public interface INotificationLogRepository
{
    Task AddAsync(NotificationLog notificationLog, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<NotificationLog>> GetRecentAsync(int take, CancellationToken cancellationToken = default);
}