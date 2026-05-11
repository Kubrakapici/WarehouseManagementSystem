using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Application.Common;
using WarehouseManagement.Application.Contracts.Services;
using WarehouseManagement.Application.DTOs.Warehouses;

namespace WarehouseManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager},{RoleNames.WarehouseStaff},{RoleNames.Operations},{RoleNames.Viewer}")]
public class WarehousesController : ControllerBase
{
    private readonly IWarehouseService _warehouses;

    public WarehousesController(IWarehouseService warehouses)
    {
        _warehouses = warehouses;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<WarehouseDto>>>> GetPaged([FromQuery] PaginationParameters parameters, CancellationToken cancellationToken)
    {
        var data = await _warehouses.GetPagedAsync(parameters, cancellationToken);
        return Ok(ApiResponse<PagedResult<WarehouseDto>>.Ok(data));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<WarehouseDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await _warehouses.GetByIdAsync(id, cancellationToken);
        return item == null ? NotFound(ApiResponse<WarehouseDto>.Fail("Depo bulunamadı.")) : Ok(ApiResponse<WarehouseDto>.Ok(item));
    }

    [HttpPost]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager}")]
    public async Task<ActionResult<ApiResponse<WarehouseDto>>> Create([FromBody] CreateWarehouseDto dto, CancellationToken cancellationToken)
    {
        var created = await _warehouses.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ApiResponse<WarehouseDto>.Ok(created));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager}")]
    public async Task<ActionResult<ApiResponse<WarehouseDto>>> Update(Guid id, [FromBody] UpdateWarehouseDto dto, CancellationToken cancellationToken)
    {
        var updated = await _warehouses.UpdateAsync(id, dto, cancellationToken);
        return updated == null ? NotFound(ApiResponse<WarehouseDto>.Fail("Depo bulunamadı.")) : Ok(ApiResponse<WarehouseDto>.Ok(updated));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var ok = await _warehouses.DeleteAsync(id, cancellationToken);
        return ok ? Ok(ApiResponse.Ok("Silindi.")) : BadRequest(ApiResponse.Fail("Silinemedi (lokasyonlar dolu olabilir)."));
    }
}
