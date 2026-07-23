using System.Threading.Channels;
using WorldEventAlerts.Api.Domain.Abstractions;
using WorldEventAlerts.Api.Domain.Models;

namespace WorldEventAlerts.Api.Infrastructure.Messaging;

public sealed class InMemoryEventBus : IEventBus
{
    private readonly Channel<WorldEvent> _channel;

    public InMemoryEventBus()
    {
        _channel = Channel.CreateUnbounded<WorldEvent>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
    }

    public ValueTask PublishAsync(WorldEvent worldEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(worldEvent);

        return _channel.Writer.WriteAsync(worldEvent, cancellationToken);
    }

    public IAsyncEnumerable<WorldEvent> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}