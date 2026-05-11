using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Application.Common;
using WarehouseManagement.Application.Contracts.Services;
using WarehouseManagement.Application.DTOs.Stock;
using WarehouseManagement.Domain.Enums;

namespace WarehouseManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager},{RoleNames.WarehouseStaff},{RoleNames.Operations},{RoleNames.Viewer}")]
public class StockController : ControllerBase
{
    private readonly IStockService _stock;

    public StockController(IStockService stock)
    {
        _stock = stock;
    }

    [HttpGet("balances")]
    public async Task<ActionResult<ApiResponse<PagedResult<StockBalanceDto>>>> Balances([FromQuery] PaginationParameters parameters, [FromQuery] Guid? warehouseId, CancellationToken cancellationToken)
    {
        var data = await _stock.GetBalancesPagedAsync(parameters, warehouseId, cancellationToken);
        return Ok(ApiResponse<PagedResult<StockBalanceDto>>.Ok(data));
    }

    [HttpGet("movements")]
    public async Task<ActionResult<ApiResponse<PagedResult<StockMovementDto>>>> Movements(
        [FromQuery] PaginationParameters parameters,
        [FromQuery] Guid? productId,
        [FromQuery] StockMovementType? type,
        CancellationToken cancellationToken)
    {
        var data = await _stock.GetMovementsPagedAsync(parameters, productId, type, cancellationToken);
        return Ok(ApiResponse<PagedResult<StockMovementDto>>.Ok(data));
    }

    /// <summary>Urun lokasyon gecmisi (stok hareketleri, urun filtresi sabit).</summary>
    [HttpGet("products/{productId:guid}/location-history")]
    public async Task<ActionResult<ApiResponse<PagedResult<StockMovementDto>>>> ProductLocationHistory(
        Guid productId,
        [FromQuery] PaginationParameters parameters,
        CancellationToken cancellationToken)
    {
        var data = await _stock.GetMovementsPagedAsync(parameters, productId, null, cancellationToken);
        return Ok(ApiResponse<PagedResult<StockMovementDto>>.Ok(data));
    }

    [HttpPost("entry")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager},{RoleNames.WarehouseStaff}")]
    public async Task<ActionResult<ApiResponse>> Entry([FromBody] StockEntryRequestDto dto, CancellationToken cancellationToken)
    {
        await _stock.StockEntryAsync(dto, cancellationToken);
        return Ok(ApiResponse.Ok("Stok girişi tamamlandı."));
    }

    [HttpPost("exit")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager},{RoleNames.WarehouseStaff}")]
    public async Task<ActionResult<ApiResponse>> Exit([FromBody] StockExitRequestDto dto, CancellationToken cancellationToken)
    {
        await _stock.StockExitAsync(dto, cancellationToken);
        return Ok(ApiResponse.Ok("Stok çıkışı tamamlandı."));
    }

    [HttpPost("transfer")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager},{RoleNames.WarehouseStaff}")]
    public async Task<ActionResult<ApiResponse>> Transfer([FromBody] StockTransferRequestDto dto, CancellationToken cancellationToken)
    {
        await _stock.StockTransferAsync(dto, cancellationToken);
        return Ok(ApiResponse.Ok("Transfer tamamlandı."));
    }

    [HttpPost("count")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager},{RoleNames.WarehouseStaff}")]
    public async Task<ActionResult<ApiResponse>> Count([FromBody] StockCountRequestDto dto, CancellationToken cancellationToken)
    {
        await _stock.StockCountAsync(dto, cancellationToken);
        return Ok(ApiResponse.Ok("Sayım farkı kaydedildi."));
    }
}
