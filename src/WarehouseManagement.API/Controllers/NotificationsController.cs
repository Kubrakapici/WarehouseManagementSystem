using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Application.Common;
using WarehouseManagement.Application.Contracts.Services;
using WarehouseManagement.Application.DTOs.Notifications;

namespace WarehouseManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notifications;

    public NotificationsController(INotificationService notifications)
    {
        _notifications = notifications;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<NotificationDto>>>> GetMine([FromQuery] PaginationParameters parameters, CancellationToken cancellationToken)
    {
        var data = await _notifications.GetMyNotificationsAsync(parameters, cancellationToken);
        return Ok(ApiResponse<PagedResult<NotificationDto>>.Ok(data));
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<ApiResponse<int>>> UnreadCount(CancellationToken cancellationToken)
    {
        var count = await _notifications.GetUnreadCountAsync(cancellationToken);
        return Ok(ApiResponse<int>.Ok(count));
    }

    [HttpPost("{id:guid}/read")]
    public async Task<ActionResult<ApiResponse>> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        await _notifications.MarkReadAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok("Okundu olarak işaretlendi."));
    }
}
