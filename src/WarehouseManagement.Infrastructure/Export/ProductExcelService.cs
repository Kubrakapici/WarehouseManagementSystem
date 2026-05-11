using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Contracts.Services;
using WarehouseManagement.Domain.Entities;
using WarehouseManagement.Infrastructure.Persistence;

namespace WarehouseManagement.Infrastructure.Export;

public class ProductExcelService : IProductExcelService
{
    private readonly AppDbContext _db;

    public ProductExcelService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<byte[]> ExportProductsAsync(CancellationToken cancellationToken = default)
    {
        var products = await _db.Products.AsNoTracking()
            .Include(p => p.Category)
            .OrderBy(p => p.Sku)
            .ToListAsync(cancellationToken);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Products");
        ws.Cell(1, 1).Value = "SKU";
        ws.Cell(1, 2).Value = "Name";
        ws.Cell(1, 3).Value = "Category";
        ws.Cell(1, 4).Value = "Barcode";
        ws.Cell(1, 5).Value = "UnitPrice";
        ws.Cell(1, 6).Value = "MinStock";
        ws.Cell(1, 7).Value = "Active";

        var row = 2;
        foreach (var p in products)
        {
            ws.Cell(row, 1).Value = p.Sku;
            ws.Cell(row, 2).Value = p.Name;
            ws.Cell(row, 3).Value = p.Category.Name;
            ws.Cell(row, 4).Value = p.Barcode ?? "";
            ws.Cell(row, 5).Value = p.UnitPrice;
            ws.Cell(row, 6).Value = p.MinimumStockLevel;
            ws.Cell(row, 7).Value = p.IsActive ? "Yes" : "No";
            row++;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<(int Imported, IReadOnlyList<string> Errors)> ImportProductsAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheet(1);
        var rows = ws.RangeUsed()?.RowsUsed().Skip(1) ?? Enumerable.Empty<IXLRangeRow>();
        var imported = 0;

        foreach (var row in rows)
        {
            try
            {
                var sku = row.Cell(1).GetString().Trim();
                var name = row.Cell(2).GetString().Trim();
                var categoryName = row.Cell(3).GetString().Trim();
                var barcode = row.Cell(4).GetString().Trim();
                var unitPrice = row.Cell(5).GetDouble();
                var minStock = (int)row.Cell(6).GetDouble();
                var activeStr = row.Cell(7).GetString().Trim();

                if (string.IsNullOrWhiteSpace(sku) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(categoryName))
                {
                    errors.Add($"Satır {row.RowNumber()}: SKU, Name ve Category zorunlu.");
                    continue;
                }

                var category = await _db.Categories.FirstOrDefaultAsync(c => c.Name == categoryName, cancellationToken);
                if (category == null)
                {
                    category = new Category { Id = Guid.NewGuid(), Name = categoryName };
                    _db.Categories.Add(category);
                    await _db.SaveChangesAsync(cancellationToken);
                }

                var existing = await _db.Products.FirstOrDefaultAsync(p => p.Sku == sku, cancellationToken);
                if (existing != null)
                {
                    errors.Add($"Satır {row.RowNumber()}: SKU '{sku}' zaten var.");
                    continue;
                }

                var product = new Product
                {
                    Id = Guid.NewGuid(),
                    Sku = sku,
                    Name = name,
                    CategoryId = category.Id,
                    Barcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode,
                    UnitPrice = (decimal)unitPrice,
                    MinimumStockLevel = minStock,
                    IsActive = string.Equals(activeStr, "Yes", StringComparison.OrdinalIgnoreCase) || string.Equals(activeStr, "True", StringComparison.OrdinalIgnoreCase),
                    QrCodeData = $"WMS:{sku}"
                };

                _db.Products.Add(product);
                await _db.SaveChangesAsync(cancellationToken);
                imported++;
            }
            catch (Exception ex)
            {
                errors.Add($"Satır {row.RowNumber()}: {ex.Message}");
            }
        }

        return (imported, errors);
    }
}
