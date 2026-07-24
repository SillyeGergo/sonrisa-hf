using WorldEventAlerts.Api.Domain.Abstractions;
using WorldEventAlerts.Api.Domain.Models;

namespace WorldEventAlerts.Api.Infrastructure.Notifications;

public sealed class EmailNotificationProvider : INotificationProvider
{
    public string ChannelName => "Email";

    public async Task<NotificationLog> SendAsync(
        WorldEvent worldEvent,
        AlertRule alertRule,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(worldEvent);
        ArgumentNullException.ThrowIfNull(alertRule);

        await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);

        return new NotificationLog
        {
            AlertRuleId = alertRule.Id,
            AlertRuleName = alertRule.Name,
            AlertBody = BuildAlertBody(worldEvent, alertRule),
            WorldEventId = worldEvent.Id,
            WorldEventTitle = worldEvent.EventType,
            WorldEventSource = worldEvent.Source,
            PayloadJson = worldEvent.PayloadJson,
            ChannelName = ChannelName,
            ProviderName = nameof(EmailNotificationProvider),
            Succeeded = true,
            AttemptedAtUtc = DateTimeOffset.UtcNow,
            CompletedAtUtc = DateTimeOffset.UtcNow
        };
    }

    private static string BuildAlertBody(WorldEvent worldEvent, AlertRule alertRule)
    {
        return $"{alertRule.Name}: {worldEvent.EventType} from {worldEvent.Source}";
    }
}