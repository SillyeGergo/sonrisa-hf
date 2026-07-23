using WorldEventAlerts.Api.Domain.Models;

namespace WorldEventAlerts.Api.Infrastructure.Generation;

public interface IMockDataGenerator
{
    IReadOnlyCollection<WorldEvent> GenerateWorldEvents(int count);

    IReadOnlyCollection<AlertRule> GenerateAlertRules(int count);
}