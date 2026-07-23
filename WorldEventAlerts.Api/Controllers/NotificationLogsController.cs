using Microsoft.AspNetCore.Mvc;
using WorldEventAlerts.Api.Domain.Abstractions;
using WorldEventAlerts.Api.Domain.Models;

namespace WorldEventAlerts.Api.Controllers;

[ApiController]
[Route("api/notification-logs")]
public sealed class NotificationLogsController : ControllerBase
{
    private readonly INotificationLogRepository _notificationLogRepository;

    public NotificationLogsController(INotificationLogRepository notificationLogRepository)
    {
        _notificationLogRepository = notificationLogRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<NotificationLog>>> GetRecentAsync(
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var notificationLogs = await _notificationLogRepository.GetRecentAsync(take, cancellationToken);
        return Ok(notificationLogs);
    }
}