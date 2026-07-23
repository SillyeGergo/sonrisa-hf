using WorldEventAlerts.Api.Domain.Models;

namespace WorldEventAlerts.Api.Domain.Abstractions;

public interface INotificationProvider
{
    string ChannelName { get; }

    Task<NotificationLog> SendAsync(
        WorldEvent worldEvent,
        AlertRule alertRule,
        CancellationToken cancellationToken = default);
}