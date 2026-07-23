using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using WorldEventAlerts.Api.Domain.Abstractions;
using WorldEventAlerts.Api.Domain.Models;

namespace WorldEventAlerts.Api.Workers;

public sealed class EventProcessingWorker : BackgroundService
{
    private readonly IEventBus _eventBus;
    private readonly IAlertRuleRepository _alertRuleRepository;
    private readonly INotificationLogRepository _notificationLogRepository;
    private readonly IReadOnlyDictionary<string, INotificationProvider> _providersByChannel;
    private readonly ResiliencePipeline _providerPipeline;
    private readonly ILogger<EventProcessingWorker> _logger;

    public EventProcessingWorker(
        IEventBus eventBus,
        IAlertRuleRepository alertRuleRepository,
        INotificationLogRepository notificationLogRepository,
        IEnumerable<INotificationProvider> notificationProviders,
        ILogger<EventProcessingWorker> logger)
    {
        _eventBus = eventBus;
        _alertRuleRepository = alertRuleRepository;
        _notificationLogRepository = notificationLogRepository;
        _logger = logger;

        _providersByChannel = notificationProviders
            .Where(provider => !string.IsNullOrWhiteSpace(provider.ChannelName))
            .GroupBy(provider => provider.ChannelName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        _providerPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(200),
                UseJitter = true
            })
            .AddTimeout(TimeSpan.FromSeconds(5))
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 3,
                BreakDuration = TimeSpan.FromSeconds(10)
            })
            .Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var worldEvent in _eventBus.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessEventAsync(worldEvent, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unhandled error while processing event {EventId}", worldEvent.Id);
            }
        }
    }

    private async Task ProcessEventAsync(WorldEvent worldEvent, CancellationToken cancellationToken)
    {
        var alertRules = await _alertRuleRepository.GetAllAsync(cancellationToken);
        var matchingAlertRules = alertRules
            .Where(alertRule => alertRule.IsActive && Matches(alertRule, worldEvent))
            .ToArray();

        foreach (var alertRule in matchingAlertRules)
        {
            await DispatchAlertRuleAsync(worldEvent, alertRule, cancellationToken);
        }
    }

    private async Task DispatchAlertRuleAsync(WorldEvent worldEvent, AlertRule alertRule, CancellationToken cancellationToken)
    {
        if (alertRule.NotificationChannels.Count == 0)
        {
            _logger.LogWarning("Alert rule {AlertRuleId} has no notification channels configured", alertRule.Id);
            return;
        }

        foreach (var channelName in alertRule.NotificationChannels)
        {
            if (!_providersByChannel.TryGetValue(channelName, out var provider))
            {
                _logger.LogWarning(
                    "No notification provider registered for channel {ChannelName} (AlertRuleId: {AlertRuleId})",
                    channelName,
                    alertRule.Id);

                await _notificationLogRepository.AddAsync(CreateFailureLog(worldEvent, alertRule, channelName, channelName, "No provider registered for channel"), cancellationToken);
                continue;
            }

            try
            {
                var notificationLog = await _providerPipeline.ExecuteAsync(
                    async innerCancellationToken => await provider.SendAsync(worldEvent, alertRule, innerCancellationToken),
                    cancellationToken);

                await _notificationLogRepository.AddAsync(notificationLog, cancellationToken);
            }
            catch (TimeoutRejectedException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Notification timed out for channel {ChannelName} (AlertRuleId: {AlertRuleId}, EventId: {EventId})",
                    channelName,
                    alertRule.Id,
                    worldEvent.Id);

                await _notificationLogRepository.AddAsync(
                    CreateFailureLog(worldEvent, alertRule, channelName, provider.ChannelName, exception.Message),
                    cancellationToken);
            }
            catch (BrokenCircuitException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Notification circuit is open for channel {ChannelName} (AlertRuleId: {AlertRuleId}, EventId: {EventId})",
                    channelName,
                    alertRule.Id,
                    worldEvent.Id);

                await _notificationLogRepository.AddAsync(
                    CreateFailureLog(worldEvent, alertRule, channelName, provider.ChannelName, exception.Message),
                    cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Notification failed for channel {ChannelName} (AlertRuleId: {AlertRuleId}, EventId: {EventId})",
                    channelName,
                    alertRule.Id,
                    worldEvent.Id);

                await _notificationLogRepository.AddAsync(
                    CreateFailureLog(worldEvent, alertRule, channelName, provider.ChannelName, exception.Message),
                    cancellationToken);
            }
        }
    }

    private static bool Matches(AlertRule alertRule, WorldEvent worldEvent)
    {
        if (string.IsNullOrWhiteSpace(alertRule.MatchExpression))
        {
            return true;
        }

        var comparison = StringComparison.OrdinalIgnoreCase;

        return worldEvent.EventType.Contains(alertRule.MatchExpression, comparison)
            || worldEvent.Source.Contains(alertRule.MatchExpression, comparison)
            || worldEvent.PayloadJson.Contains(alertRule.MatchExpression, comparison)
            || worldEvent.Metadata.Any(entry =>
                entry.Key.Contains(alertRule.MatchExpression, comparison)
                || entry.Value.Contains(alertRule.MatchExpression, comparison));
    }

    private static NotificationLog CreateFailureLog(
        WorldEvent worldEvent,
        AlertRule alertRule,
        string channelName,
        string providerName,
        string errorMessage)
    {
        return new NotificationLog
        {
            AlertRuleId = alertRule.Id,
            WorldEventId = worldEvent.Id,
            ChannelName = channelName,
            ProviderName = providerName,
            Succeeded = false,
            ErrorMessage = errorMessage,
            AttemptedAtUtc = DateTimeOffset.UtcNow,
            CompletedAtUtc = DateTimeOffset.UtcNow
        };
    }
}