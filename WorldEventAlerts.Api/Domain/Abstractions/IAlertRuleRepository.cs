using WorldEventAlerts.Api.Domain.Models;

namespace WorldEventAlerts.Api.Domain.Abstractions;

public interface IAlertRuleRepository
{
    Task<IReadOnlyCollection<AlertRule>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<AlertRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task UpsertAsync(AlertRule alertRule, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}