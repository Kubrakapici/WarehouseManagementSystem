using AutoMapper;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Contracts;
using WarehouseManagement.Application.Contracts.Repositories;
using WarehouseManagement.Application.Contracts.Services;
using WarehouseManagement.Application.DTOs.Dashboard;
using WarehouseManagement.Application.DTOs.Products;
using WarehouseManagement.Application.DTOs.Stock;
using WarehouseManagement.Domain.Entities;
using WarehouseManagement.Domain.Enums;

namespace WarehouseManagement.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IRepository<Product> _products;
    private readonly IRepository<Warehouse> _warehouses;
    private readonly IRepository<StockMovement> _movements;
    private readonly IRepository<StockBalance> _balances;
    private readonly IMapper _mapper;
    private readonly IPresenceService _presence;

    public DashboardService(
        IRepository<Product> products,
        IRepository<Warehouse> warehouses,
        IRepository<StockMovement> movements,
        IRepository<StockBalance> balances,
        IMapper mapper,
        IPresenceService presence)
    {
        _products = products;
        _warehouses = warehouses;
        _movements = movements;
        _balances = balances;
        _mapper = mapper;
        _presence = presence;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var from7 = today.AddDays(-6);

        var totalProducts = await _products.Query().CountAsync(p => p.IsActive, cancellationToken);
        var totalWarehouses = await _warehouses.Query().CountAsync(w => w.IsActive, cancellationToken);

        var todayMovements = await _movements.Query()
            .Where(m => m.CreatedDate >= today && m.CreatedDate < tomorrow)
            .ToListAsync(cancellationToken);

        var entries = todayMovements.Where(m => m.MovementType == StockMovementType.Entry).Sum(m => m.Quantity);
        var exits = todayMovements.Where(m => m.MovementType == StockMovementType.Exit).Sum(m => m.Quantity);

        var activeWithBalances = await _products.Query()
            .Include(p => p.Category)
            .Include(p => p.StockBalances)
            .Where(p => p.IsActive)
            .ToListAsync(cancellationToken);

        var criticalProducts = activeWithBalances
            .Where(p => p.StockBalances.Sum(b => b.Quantity) <= p.MinimumStockLevel)
            .OrderBy(p => p.Name)
            .Take(20)
            .ToList();

        var criticalDtos = criticalProducts.Select(p =>
        {
            var dto = _mapper.Map<ProductDto>(p);
            dto.TotalQuantity = p.StockBalances.Sum(b => b.Quantity);
            return dto;
        }).ToList();

        var recent = await _movements.Query()
            .Include(m => m.Product)
            .Include(m => m.FromLocation)
            .Include(m => m.ToLocation)
            .Include(m => m.User)
            .OrderByDescending(m => m.CreatedDate)
            .Take(10)
            .ToListAsync(cancellationToken);

        var recentDtos = recent.Select(m => _mapper.Map<StockMovementDto>(m)).ToList();

        var weekMovements = await _movements.Query()
            .Where(m => m.CreatedDate >= from7)
            .ToListAsync(cancellationToken);

        var lastSevenDays = new List<DashboardSeriesPointDto>();
        for (var i = 0; i < 7; i++)
        {
            var d = from7.AddDays(i);
            var next = d.AddDays(1);
            var dayList = weekMovements.Where(m => m.CreatedDate >= d && m.CreatedDate < next).ToList();
            lastSevenDays.Add(new DashboardSeriesPointDto
            {
                Label = d.ToString("yyyy-MM-dd"),
                Entries = dayList.Where(m => m.MovementType == StockMovementType.Entry).Sum(m => m.Quantity),
                Exits = dayList.Where(m => m.MovementType == StockMovementType.Exit).Sum(m => m.Quantity)
            });
        }

        var since30 = DateTime.UtcNow.AddDays(-30);
        var movements30 = await _movements.Query()
            .Include(m => m.Product)
            .Where(m => m.CreatedDate >= since30)
            .ToListAsync(cancellationToken);

        var topMoved = movements30
            .GroupBy(m => m.ProductId)
            .Select(g =>
            {
                var p = g.First().Product;
                return new TopMovedProductDto
                {
                    ProductId = g.Key,
                    Name = p.Name,
                    Sku = p.Sku,
                    MovementUnits = g.Sum(x => x.Quantity)
                };
            })
            .OrderByDescending(x => x.MovementUnits)
            .Take(10)
            .ToList();

        var warehouseList = await _warehouses.Query()
            .Include(w => w.Locations)
            .Where(w => w.IsActive)
            .ToListAsync(cancellationToken);

        var occupancy = new List<WarehouseOccupancyDto>();
        foreach (var w in warehouseList)
        {
            var locIds = w.Locations.Select(l => l.Id).ToHashSet();
            var used = await _balances.Query()
                .Where(b => locIds.Contains(b.LocationId) && b.Quantity > 0)
                .Select(b => b.LocationId)
                .Distinct()
                .CountAsync(cancellationToken);

            var totalLoc = w.Locations.Count;
            var ratio = totalLoc == 0 ? 0 : (double)used / totalLoc;
            occupancy.Add(new WarehouseOccupancyDto
            {
                WarehouseId = w.Id,
                WarehouseName = w.Name,
                TotalLocations = totalLoc,
                UsedLocations = used,
                OccupancyRatio = Math.Round(ratio, 4)
            });
        }

        return new DashboardSummaryDto
        {
            TotalProducts = totalProducts,
            TotalWarehouses = totalWarehouses,
            TodayStockEntries = entries,
            TodayStockExits = exits,
            CriticalStockProducts = criticalDtos,
            RecentMovements = recentDtos,
            WarehouseOccupancy = occupancy,
            TopMovedProducts = topMoved,
            LastSevenDaysMovement = lastSevenDays,
            OnlineUsers = _presence.GetOnlineUserCount()
        };
    }
}
