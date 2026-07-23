using WorldEventAlerts.Api.Domain.Models;

namespace WorldEventAlerts.Api.Domain.Abstractions;

public interface IEventBus
{
    ValueTask PublishAsync(WorldEvent worldEvent, CancellationToken cancellationToken = default);

    IAsyncEnumerable<WorldEvent> ReadAllAsync(CancellationToken cancellationToken = default);
}