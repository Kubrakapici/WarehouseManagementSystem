using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Application.Common;
using WarehouseManagement.Application.Contracts.Services;
using WarehouseManagement.Application.DTOs.Locations;

namespace WarehouseManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager},{RoleNames.WarehouseStaff},{RoleNames.Operations},{RoleNames.Viewer}")]
public class LocationsController : ControllerBase
{
    private readonly ILocationService _locations;

    public LocationsController(ILocationService locations)
    {
        _locations = locations;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<LocationDto>>>> GetPaged([FromQuery] PaginationParameters parameters, [FromQuery] Guid? warehouseId, CancellationToken cancellationToken)
    {
        var data = await _locations.GetPagedAsync(parameters, warehouseId, cancellationToken);
        return Ok(ApiResponse<PagedResult<LocationDto>>.Ok(data));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<LocationDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await _locations.GetByIdAsync(id, cancellationToken);
        return item == null ? NotFound(ApiResponse<LocationDto>.Fail("Lokasyon bulunamadı.")) : Ok(ApiResponse<LocationDto>.Ok(item));
    }

    [HttpPost]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager},{RoleNames.WarehouseStaff}")]
    public async Task<ActionResult<ApiResponse<LocationDto>>> Create([FromBody] CreateLocationDto dto, CancellationToken cancellationToken)
    {
        var created = await _locations.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ApiResponse<LocationDto>.Ok(created));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager},{RoleNames.WarehouseStaff}")]
    public async Task<ActionResult<ApiResponse<LocationDto>>> Update(Guid id, [FromBody] UpdateLocationDto dto, CancellationToken cancellationToken)
    {
        var updated = await _locations.UpdateAsync(id, dto, cancellationToken);
        return updated == null ? NotFound(ApiResponse<LocationDto>.Fail("Lokasyon bulunamadı.")) : Ok(ApiResponse<LocationDto>.Ok(updated));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var ok = await _locations.DeleteAsync(id, cancellationToken);
        return ok ? Ok(ApiResponse.Ok("Silindi.")) : BadRequest(ApiResponse.Fail("Silinemedi (stok mevcut olabilir)."));
    }
}
