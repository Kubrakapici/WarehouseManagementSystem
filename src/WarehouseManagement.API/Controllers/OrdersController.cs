using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Application.Common;
using WarehouseManagement.Application.Contracts.Services;
using WarehouseManagement.Application.DTOs.Orders;
using WarehouseManagement.Domain.Enums;

namespace WarehouseManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager},{RoleNames.WarehouseStaff},{RoleNames.Operations},{RoleNames.Viewer}")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders)
    {
        _orders = orders;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<OrderDto>>>> GetPaged([FromQuery] PaginationParameters parameters, [FromQuery] OrderStatus? status, CancellationToken cancellationToken)
    {
        var data = await _orders.GetPagedAsync(parameters, status, cancellationToken);
        return Ok(ApiResponse<PagedResult<OrderDto>>.Ok(data));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await _orders.GetByIdAsync(id, cancellationToken);
        return item == null ? NotFound(ApiResponse<OrderDto>.Fail("Sipariş bulunamadı.")) : Ok(ApiResponse<OrderDto>.Ok(item));
    }

    [HttpPost]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager},{RoleNames.Operations}")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> Create([FromBody] CreateOrderDto dto, CancellationToken cancellationToken)
    {
        var created = await _orders.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ApiResponse<OrderDto>.Ok(created));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager},{RoleNames.Operations}")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusDto dto, CancellationToken cancellationToken)
    {
        var updated = await _orders.UpdateStatusAsync(id, dto.Status, cancellationToken);
        return updated == null ? NotFound(ApiResponse<OrderDto>.Fail("Sipariş bulunamadı.")) : Ok(ApiResponse<OrderDto>.Ok(updated));
    }
}
