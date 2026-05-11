using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Application.Common;
using WarehouseManagement.Application.Contracts.Services;

namespace WarehouseManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager},{RoleNames.Viewer}")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reports;

    public ReportsController(IReportService reports)
    {
        _reports = reports;
    }

    [HttpGet("critical-stock/excel")]
    public async Task<IActionResult> CriticalStockExcel(CancellationToken cancellationToken)
    {
        var bytes = await _reports.ExportCriticalStockExcelAsync(cancellationToken);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "critical-stock.xlsx");
    }

    [HttpGet("movements/daily/excel")]
    public async Task<IActionResult> DailyMovementExcel([FromQuery] DateTime date, CancellationToken cancellationToken)
    {
        var bytes = await _reports.ExportDailyStockMovementExcelAsync(date, cancellationToken);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"movements-{date:yyyyMMdd}.xlsx");
    }

    [HttpGet("products/{productId:guid}/movements/pdf")]
    public async Task<IActionResult> ProductMovementPdf(Guid productId, [FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc, CancellationToken cancellationToken)
    {
        var bytes = await _reports.ExportProductMovementPdfAsync(productId, fromUtc, toUtc, cancellationToken);
        return File(bytes, "application/pdf", $"product-movements-{productId:N}.pdf");
    }
}
