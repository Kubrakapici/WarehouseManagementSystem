using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Application.Common;
using WarehouseManagement.Application.Contracts;
using WarehouseManagement.Application.Contracts.Services;
using WarehouseManagement.Application.DTOs.Products;
using WarehouseManagement.Application.DTOs.Procurement;

namespace WarehouseManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager},{RoleNames.WarehouseStaff},{RoleNames.Operations},{RoleNames.Viewer}")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _products;
    private readonly IProductExcelService _excel;
    private readonly IBarcodeQrService _qr;

    public ProductsController(IProductService products, IProductExcelService excel, IBarcodeQrService qr)
    {
        _products = products;
        _excel = excel;
        _qr = qr;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductDto>>>> GetPaged(
        [FromQuery] PaginationParameters parameters,
        [FromQuery] Guid? categoryId,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var data = await _products.GetPagedAsync(parameters, categoryId, isActive, cancellationToken);
        return Ok(ApiResponse<PagedResult<ProductDto>>.Ok(data));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await _products.GetByIdAsync(id, cancellationToken);
        return item == null ? NotFound(ApiResponse<ProductDto>.Fail("Ürün bulunamadı.")) : Ok(ApiResponse<ProductDto>.Ok(item));
    }

    [HttpGet("barcode/{code}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetByBarcode(string code, CancellationToken cancellationToken)
    {
        var item = await _products.GetByBarcodeAsync(code, cancellationToken);
        return item == null ? NotFound(ApiResponse<ProductDto>.Fail("Ürün bulunamadı.")) : Ok(ApiResponse<ProductDto>.Ok(item));
    }

    /// <summary>SKU içeren QR görselleri (PNG).</summary>
    [HttpGet("{id:guid}/qr")]
    public async Task<IActionResult> GetQr(Guid id, CancellationToken cancellationToken)
    {
        var item = await _products.GetByIdAsync(id, cancellationToken);
        if (item == null) return NotFound();
        var payload = item.QrCodeData ?? $"WMS:{item.Sku}";
        var png = _qr.GenerateQrPng(payload);
        return File(png, "image/png", $"{item.Sku}-qr.png");
    }

    [HttpPost]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager},{RoleNames.WarehouseStaff}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> Create([FromBody] CreateProductDto dto, CancellationToken cancellationToken)
    {
        var created = await _products.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ApiResponse<ProductDto>.Ok(created));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager},{RoleNames.WarehouseStaff}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> Update(Guid id, [FromBody] UpdateProductDto dto, CancellationToken cancellationToken)
    {
        var updated = await _products.UpdateAsync(id, dto, cancellationToken);
        return updated == null ? NotFound(ApiResponse<ProductDto>.Fail("Ürün bulunamadı.")) : Ok(ApiResponse<ProductDto>.Ok(updated));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var ok = await _products.DeleteAsync(id, cancellationToken);
        return ok ? Ok(ApiResponse.Ok("Silindi.")) : BadRequest(ApiResponse.Fail("Silinemedi (sipariş veya stok kısıtı)."));
    }

    [HttpGet("export/excel")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager}")]
    public async Task<IActionResult> ExportExcel(CancellationToken cancellationToken)
    {
        var bytes = await _excel.ExportProductsAsync(cancellationToken);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "products.xlsx");
    }

    [HttpPost("import/excel")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager}")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<object>>> ImportExcel(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
            return BadRequest(ApiResponse.Fail("Dosya boş."));

        await using var stream = file.OpenReadStream();
        var result = await _excel.ImportProductsAsync(stream, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { result.Imported, result.Errors }));
    }

    /// <summary>Toplu QR etiketleri (ZIP, her urun icin PNG).</summary>
    [HttpPost("labels/qr-zip")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager},{RoleNames.WarehouseStaff}")]
    public async Task<IActionResult> BulkQrZip([FromBody] BulkProductQrLabelsDto dto, CancellationToken cancellationToken)
    {
        if (dto.ProductIds.Count == 0)
            return BadRequest(ApiResponse.Fail("Urun listesi bos."));

        var items = new List<(string FileName, string Payload)>();
        foreach (var id in dto.ProductIds.Distinct())
        {
            var p = await _products.GetByIdAsync(id, cancellationToken);
            if (p == null) continue;
            var payload = p.QrCodeData ?? $"WMS:{p.Sku}";
            items.Add((p.Sku, payload));
        }

        if (items.Count == 0)
            return NotFound();

        var zip = _qr.GenerateQrLabelsZip(items);
        return File(zip, "application/zip", "product-qr-labels.zip");
    }
}
