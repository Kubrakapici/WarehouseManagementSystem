using WarehouseManagement.Application.DTOs.Products;
using WarehouseManagement.Application.DTOs.Stock;

namespace WarehouseManagement.Application.DTOs.Dashboard;

public class DashboardSummaryDto
{
    public int TotalProducts { get; set; }
    public int TotalWarehouses { get; set; }
    public int TodayStockEntries { get; set; }
    public int TodayStockExits { get; set; }
    public IReadOnlyList<ProductDto> CriticalStockProducts { get; set; } = Array.Empty<ProductDto>();
    public IReadOnlyList<StockMovementDto> RecentMovements { get; set; } = Array.Empty<StockMovementDto>();

    public IReadOnlyList<WarehouseOccupancyDto> WarehouseOccupancy { get; set; } = Array.Empty<WarehouseOccupancyDto>();
    public IReadOnlyList<TopMovedProductDto> TopMovedProducts { get; set; } = Array.Empty<TopMovedProductDto>();
    public IReadOnlyList<DashboardSeriesPointDto> LastSevenDaysMovement { get; set; } = Array.Empty<DashboardSeriesPointDto>();
    public int OnlineUsers { get; set; }
}

public class WarehouseOccupancyDto
{
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public int TotalLocations { get; set; }
    public int UsedLocations { get; set; }
    public double OccupancyRatio { get; set; }
}

public class TopMovedProductDto
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public int MovementUnits { get; set; }
}

public class DashboardSeriesPointDto
{
    public string Label { get; set; } = string.Empty;
    public int Entries { get; set; }
    public int Exits { get; set; }
}
