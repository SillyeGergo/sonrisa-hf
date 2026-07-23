using Microsoft.AspNetCore.Mvc;
using WorldEventAlerts.Api.Domain.Abstractions;
using WorldEventAlerts.Api.Domain.Models;
using WorldEventAlerts.Api.Infrastructure.Generation;

namespace WorldEventAlerts.Api.Controllers;

[ApiController]
[Route("api/mock-data")]
public sealed class MockDataController : ControllerBase
{
    private readonly IMockDataGenerator _mockDataGenerator;
    private readonly IAlertRuleRepository _alertRuleRepository;
    private readonly IEventBus _eventBus;

    public MockDataController(
        IMockDataGenerator mockDataGenerator,
        IAlertRuleRepository alertRuleRepository,
        IEventBus eventBus)
    {
        _mockDataGenerator = mockDataGenerator;
        _alertRuleRepository = alertRuleRepository;
        _eventBus = eventBus;
    }

    [HttpPost("seed")]
    public async Task<ActionResult<MockDataSeedResponse>> SeedAsync(
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

        return Ok(new MockDataSeedResponse(
            generatedAlertRules.Count,
            generatedWorldEvents.Count,
            generatedAlertRules.Select(rule => rule.Id).ToArray(),
            generatedWorldEvents.Select(worldEvent => worldEvent.Id).ToArray()));
    }
}

public sealed record MockDataSeedResponse(
    int AlertRulesCreated,
    int WorldEventsPublished,
    IReadOnlyCollection<Guid> AlertRuleIds,
    IReadOnlyCollection<Guid> WorldEventIds);