using Microsoft.AspNetCore.Mvc;
using WorldEventAlerts.Api.Domain.Abstractions;
using WorldEventAlerts.Api.Domain.Models;
using WorldEventAlerts.Api.Infrastructure.Generation;

namespace WorldEventAlerts.Api.Controllers;

[ApiController]
[Route("api/simulation")]
public sealed class SimulationController : ControllerBase
{
    private readonly IMockDataGenerator _mockDataGenerator;
    private readonly IAlertRuleRepository _alertRuleRepository;
    private readonly IEventBus _eventBus;

    public SimulationController(
        IMockDataGenerator mockDataGenerator,
        IAlertRuleRepository alertRuleRepository,
        IEventBus eventBus)
    {
        _mockDataGenerator = mockDataGenerator;
        _alertRuleRepository = alertRuleRepository;
        _eventBus = eventBus;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<SimulationGenerateResponse>> GenerateAsync(
        [FromQuery] int alertRules = 5,
        [FromQuery] int worldEvents = 10,
        CancellationToken cancellationToken = default)
    {
        var generatedAlertRules = _mockDataGenerator.GenerateAlertRules(alertRules);
        var generatedWorldEvents = _mockDataGenerator.GenerateWorldEvents(worldEvents);

        foreach (var alertRule in generatedAlertRules)
        {
            await _alertRuleRepository.UpsertAsync(alertRule, cancellationToken);
        }

        foreach (var worldEvent in generatedWorldEvents)
        {
            await _eventBus.PublishAsync(worldEvent, cancellationToken);
        }

        return Ok(new SimulationGenerateResponse(
            generatedAlertRules.Count,
            generatedWorldEvents.Count,
            generatedAlertRules.Select(rule => rule.Id).ToArray(),
            generatedWorldEvents.Select(worldEvent => worldEvent.Id).ToArray()));
    }
}

public sealed record SimulationGenerateResponse(
    int AlertRulesCreated,
    int WorldEventsPublished,
    IReadOnlyCollection<Guid> AlertRuleIds,
    IReadOnlyCollection<Guid> WorldEventIds);