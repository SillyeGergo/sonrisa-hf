using Microsoft.AspNetCore.Mvc;
using WorldEventAlerts.Api.Domain.Abstractions;
using WorldEventAlerts.Api.Domain.Models;

namespace WorldEventAlerts.Api.Controllers;

[ApiController]
[Route("api/alert-rules")]
public sealed class AlertRulesController : ControllerBase
{
    private readonly IAlertRuleRepository _alertRuleRepository;

    public AlertRulesController(IAlertRuleRepository alertRuleRepository)
    {
        _alertRuleRepository = alertRuleRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<AlertRule>>> GetAllAsync(CancellationToken cancellationToken)
    {
        var alertRules = await _alertRuleRepository.GetAllAsync(cancellationToken);
        return Ok(alertRules);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AlertRule>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var alertRule = await _alertRuleRepository.GetByIdAsync(id, cancellationToken);
        return alertRule is null ? NotFound() : Ok(alertRule);
    }

    [HttpPost]
    public async Task<ActionResult<AlertRule>> CreateAsync([FromBody] AlertRule alertRule, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(alertRule);

        var ruleToSave = alertRule with
        {
            Id = alertRule.Id == Guid.Empty ? Guid.NewGuid() : alertRule.Id,
            CreatedAtUtc = alertRule.CreatedAtUtc == default ? DateTimeOffset.UtcNow : alertRule.CreatedAtUtc
        };

        await _alertRuleRepository.UpsertAsync(ruleToSave, cancellationToken);
        return Ok(ruleToSave);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AlertRule>> UpsertAsync(Guid id, [FromBody] AlertRule alertRule, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(alertRule);

        var ruleToSave = alertRule with
        {
            Id = id,
            CreatedAtUtc = alertRule.CreatedAtUtc == default ? DateTimeOffset.UtcNow : alertRule.CreatedAtUtc,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        await _alertRuleRepository.UpsertAsync(ruleToSave, cancellationToken);
        return Ok(ruleToSave);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _alertRuleRepository.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}