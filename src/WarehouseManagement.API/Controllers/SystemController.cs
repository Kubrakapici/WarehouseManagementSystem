using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Application.Common;
using WarehouseManagement.Application.Contracts;

namespace WarehouseManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SystemController : ControllerBase
{
    private readonly IPresenceService _presence;

    public SystemController(IPresenceService presence)
    {
        _presence = presence;
    }

    [HttpGet("presence/online-count")]
    public ActionResult<ApiResponse<int>> OnlineCount()
    {
        return Ok(ApiResponse<int>.Ok(_presence.GetOnlineUserCount()));
    }
}
