using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Application.Common;
using WarehouseManagement.Application.Contracts.Services;
using WarehouseManagement.Application.DTOs.Procurement;
using WarehouseManagement.Domain.Enums;

namespace WarehouseManagement.API.Controllers;

[ApiController]
[Route("api/purchase-requisitions")]
[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager},{RoleNames.Operations},{RoleNames.Viewer}")]
public class PurchaseRequisitionsController : ControllerBase
{
    private readonly IPurchaseRequisitionService _purchase;

    public PurchaseRequisitionsController(IPurchaseRequisitionService purchase)
    {
        _purchase = purchase;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<PurchaseRequisitionDto>>>> GetPaged(
        [FromQuery] PaginationParameters parameters,
        [FromQuery] PurchaseRequisitionStatus? status,
        [FromQuery] Guid? warehouseId,
        CancellationToken cancellationToken)
    {
        var data = await _purchase.GetPagedAsync(parameters, status, warehouseId, cancellationToken);
        return Ok(ApiResponse<PagedResult<PurchaseRequisitionDto>>.Ok(data));
    }

    [HttpGet("suggestions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PurchaseSuggestionLineDto>>>> Suggestions(
        [FromQuery] Guid? warehouseId,
        CancellationToken cancellationToken)
    {
        var data = await _purchase.GetSuggestionsAsync(warehouseId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PurchaseSuggestionLineDto>>.Ok(data));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PurchaseRequisitionDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await _purchase.GetByIdAsync(id, cancellationToken);
        return item == null ? NotFound(ApiResponse<PurchaseRequisitionDto>.Fail("Talep bulunamadı.")) : Ok(ApiResponse<PurchaseRequisitionDto>.Ok(item));
    }

    [HttpPost]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager},{RoleNames.Operations}")]
    public async Task<ActionResult<ApiResponse<PurchaseRequisitionDto>>> Create([FromBody] CreatePurchaseRequisitionDto dto, CancellationToken cancellationToken)
    {
        var created = await _purchase.CreateDraftAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ApiResponse<PurchaseRequisitionDto>.Ok(created));
    }

    [HttpPost("{id:guid}/submit")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager},{RoleNames.Operations}")]
    public async Task<ActionResult<ApiResponse>> Submit(Guid id, CancellationToken cancellationToken)
    {
        await _purchase.SubmitForApprovalAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok("Onaya gönderildi."));
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager}")]
    public async Task<ActionResult<ApiResponse>> Approve(Guid id, CancellationToken cancellationToken)
    {
        await _purchase.ApproveAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok("Onaylandı."));
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager}")]
    public async Task<ActionResult<ApiResponse>> Reject(Guid id, [FromBody] RejectPurchaseRequisitionDto? body, CancellationToken cancellationToken)
    {
        await _purchase.RejectAsync(id, body?.Reason, cancellationToken);
        return Ok(ApiResponse.Ok("Reddedildi."));
    }

    [HttpPost("{id:guid}/quotes")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager}")]
    public async Task<ActionResult<ApiResponse<SupplierQuoteDto>>> AddQuote(Guid id, [FromBody] AddSupplierQuoteDto dto, CancellationToken cancellationToken)
    {
        var q = await _purchase.AddSupplierQuoteAsync(id, dto, cancellationToken);
        return Ok(ApiResponse<SupplierQuoteDto>.Ok(q));
    }

    [HttpPost("{id:guid}/quotes/{quoteId:guid}/accept")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager}")]
    public async Task<ActionResult<ApiResponse>> AcceptQuote(Guid id, Guid quoteId, CancellationToken cancellationToken)
    {
        await _purchase.AcceptSupplierQuoteAsync(id, quoteId, cancellationToken);
        return Ok(ApiResponse.Ok("Teklif kabul edildi."));
    }
}
