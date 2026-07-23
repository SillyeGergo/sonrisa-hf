using System.Collections.Concurrent;
using WorldEventAlerts.Api.Domain.Abstractions;
using WorldEventAlerts.Api.Domain.Models;

namespace WorldEventAlerts.Api.Infrastructure.Repositories;

public sealed class InMemoryAlertRuleRepository : IAlertRuleRepository
{
    private readonly ConcurrentDictionary<Guid, AlertRule> _alertRules = new();

    public Task<IReadOnlyCollection<AlertRule>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyCollection<AlertRule> snapshot = _alertRules.Values.ToArray();
        return Task.FromResult(snapshot);
    }

    public Task<AlertRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _alertRules.TryGetValue(id, out var alertRule);
        return Task.FromResult(alertRule);
    }

    public Task UpsertAsync(AlertRule alertRule, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentNullException.ThrowIfNull(alertRule);

        _alertRules.AddOrUpdate(alertRule.Id, alertRule, (_, _) => alertRule);
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var removed = _alertRules.TryRemove(id, out _);
        return Task.FromResult(removed);
    }
}