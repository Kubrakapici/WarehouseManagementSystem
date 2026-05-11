using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Application.Common;
using WarehouseManagement.Application.Contracts.Services;
using WarehouseManagement.Application.DTOs.Customers;

namespace WarehouseManagement.API.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager},{RoleNames.Operations},{RoleNames.Viewer}")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customers;

    public CustomersController(ICustomerService customers)
    {
        _customers = customers;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<CustomerDto>>>> GetPaged([FromQuery] PaginationParameters parameters, CancellationToken cancellationToken)
    {
        var data = await _customers.GetPagedAsync(parameters, cancellationToken);
        return Ok(ApiResponse<PagedResult<CustomerDto>>.Ok(data));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await _customers.GetByIdAsync(id, cancellationToken);
        return item == null ? NotFound(ApiResponse<CustomerDto>.Fail("M\u00fc\u015fteri bulunamad\u0131.")) : Ok(ApiResponse<CustomerDto>.Ok(item));
    }

    [HttpPost]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager}")]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> Create([FromBody] CreateCustomerDto dto, CancellationToken cancellationToken)
    {
        var created = await _customers.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ApiResponse<CustomerDto>.Ok(created));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager}")]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> Update(Guid id, [FromBody] UpdateCustomerDto dto, CancellationToken cancellationToken)
    {
        var updated = await _customers.UpdateAsync(id, dto, cancellationToken);
        return updated == null ? NotFound(ApiResponse<CustomerDto>.Fail("M\u00fc\u015fteri bulunamad\u0131.")) : Ok(ApiResponse<CustomerDto>.Ok(updated));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var ok = await _customers.DeleteAsync(id, cancellationToken);
        return ok ? Ok(ApiResponse.Ok("Silindi.")) : NotFound(ApiResponse.Fail("M\u00fc\u015fteri bulunamad\u0131."));
    }
}
