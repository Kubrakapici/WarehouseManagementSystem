using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Application.Common;
using WarehouseManagement.Application.Contracts.Services;
using WarehouseManagement.Application.DTOs.Suppliers;

namespace WarehouseManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager},{RoleNames.Operations},{RoleNames.Viewer}")]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierService _suppliers;

    public SuppliersController(ISupplierService suppliers)
    {
        _suppliers = suppliers;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<SupplierDto>>>> GetPaged([FromQuery] PaginationParameters parameters, CancellationToken cancellationToken)
    {
        var data = await _suppliers.GetPagedAsync(parameters, cancellationToken);
        return Ok(ApiResponse<PagedResult<SupplierDto>>.Ok(data));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SupplierDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await _suppliers.GetByIdAsync(id, cancellationToken);
        return item == null ? NotFound(ApiResponse<SupplierDto>.Fail("Tedarikçi bulunamadı.")) : Ok(ApiResponse<SupplierDto>.Ok(item));
    }

    [HttpPost]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager}")]
    public async Task<ActionResult<ApiResponse<SupplierDto>>> Create([FromBody] CreateSupplierDto dto, CancellationToken cancellationToken)
    {
        var created = await _suppliers.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ApiResponse<SupplierDto>.Ok(created));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager}")]
    public async Task<ActionResult<ApiResponse<SupplierDto>>> Update(Guid id, [FromBody] UpdateSupplierDto dto, CancellationToken cancellationToken)
    {
        var updated = await _suppliers.UpdateAsync(id, dto, cancellationToken);
        return updated == null ? NotFound(ApiResponse<SupplierDto>.Fail("Tedarikçi bulunamadı.")) : Ok(ApiResponse<SupplierDto>.Ok(updated));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var ok = await _suppliers.DeleteAsync(id, cancellationToken);
        return ok ? Ok(ApiResponse.Ok("Silindi.")) : BadRequest(ApiResponse.Fail("Silinemedi (sipariş bağlantısı olabilir)."));
    }
}
