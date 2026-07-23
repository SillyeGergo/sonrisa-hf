using Microsoft.AspNetCore.Mvc;
using WorldEventAlerts.Api.Domain.Abstractions;
using WorldEventAlerts.Api.Domain.Models;

namespace WorldEventAlerts.Api.Controllers;

[ApiController]
[Route("api/world-events")]
public sealed class WorldEventsController : ControllerBase
{
    private readonly IEventBus _eventBus;

    public WorldEventsController(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    [HttpPost]
    public async Task<IActionResult> IngestAsync([FromBody] WorldEvent worldEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(worldEvent);

        await _eventBus.PublishAsync(worldEvent, cancellationToken);
        return Accepted(new { worldEvent.Id });
    }
}