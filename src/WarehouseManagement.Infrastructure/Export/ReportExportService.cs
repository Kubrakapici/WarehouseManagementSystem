using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WarehouseManagement.Application.Contracts.Services;
using WarehouseManagement.Infrastructure.Persistence;

namespace WarehouseManagement.Infrastructure.Export;

public class ReportExportService : IReportService
{
    private readonly AppDbContext _db;

    public ReportExportService(AppDbContext db)
    {
        _db = db;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> ExportCriticalStockExcelAsync(CancellationToken cancellationToken = default)
    {
        var products = await _db.Products.AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.StockBalances)
            .Where(p => p.IsActive)
            .ToListAsync(cancellationToken);

        var critical = products
            .Where(p => p.StockBalances.Sum(b => b.Quantity) <= p.MinimumStockLevel)
            .OrderBy(p => p.Name)
            .ToList();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("CriticalStock");
        ws.Cell(1, 1).Value = "SKU";
        ws.Cell(1, 2).Value = "Name";
        ws.Cell(1, 3).Value = "Category";
        ws.Cell(1, 4).Value = "TotalQty";
        ws.Cell(1, 5).Value = "MinStock";

        var row = 2;
        foreach (var p in critical)
        {
            ws.Cell(row, 1).Value = p.Sku;
            ws.Cell(row, 2).Value = p.Name;
            ws.Cell(row, 3).Value = p.Category.Name;
            ws.Cell(row, 4).Value = p.StockBalances.Sum(b => b.Quantity);
            ws.Cell(row, 5).Value = p.MinimumStockLevel;
            row++;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> ExportDailyStockMovementExcelAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        var day = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        var next = day.AddDays(1);

        var movements = await _db.StockMovements.AsNoTracking()
            .Include(m => m.Product)
            .Include(m => m.User)
            .Where(m => m.CreatedDate >= day && m.CreatedDate < next)
            .OrderBy(m => m.CreatedDate)
            .ToListAsync(cancellationToken);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Movements");
        ws.Cell(1, 1).Value = "Time";
        ws.Cell(1, 2).Value = "Type";
        ws.Cell(1, 3).Value = "SKU";
        ws.Cell(1, 4).Value = "Product";
        ws.Cell(1, 5).Value = "Qty";
        ws.Cell(1, 6).Value = "User";

        var row = 2;
        foreach (var m in movements)
        {
            ws.Cell(row, 1).Value = m.CreatedDate.ToUniversalTime();
            ws.Cell(row, 2).Value = m.MovementType.ToString();
            ws.Cell(row, 3).Value = m.Product.Sku;
            ws.Cell(row, 4).Value = m.Product.Name;
            ws.Cell(row, 5).Value = m.Quantity;
            ws.Cell(row, 6).Value = $"{m.User.FirstName} {m.User.LastName}";
            row++;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> ExportProductMovementPdfAsync(Guid productId, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default)
    {
        var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId, cancellationToken)
                      ?? throw new InvalidOperationException("Ürün bulunamadı.");

        var query = _db.StockMovements.AsNoTracking()
            .Include(m => m.User)
            .Where(m => m.ProductId == productId);

        if (fromUtc.HasValue)
            query = query.Where(m => m.CreatedDate >= fromUtc.Value);
        if (toUtc.HasValue)
            query = query.Where(m => m.CreatedDate <= toUtc.Value);

        var movements = await query.OrderByDescending(m => m.CreatedDate).Take(500).ToListAsync(cancellationToken);

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Header().Text($"Ürün hareketleri — {product.Name} ({product.Sku})").SemiBold().FontSize(16);
                page.Content().Column(col =>
                {
                    col.Spacing(8);
                    foreach (var m in movements)
                    {
                        col.Item().Text($"{m.CreatedDate:u} | {m.MovementType} | {m.Quantity} | {m.User.FirstName} {m.User.LastName}");
                    }

                    if (movements.Count == 0)
                        col.Item().Text("Kayıt yok.");
                });
            });
        }).GeneratePdf();

        return pdf;
    }
}
