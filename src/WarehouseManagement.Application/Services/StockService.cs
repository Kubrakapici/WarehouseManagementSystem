using AutoMapper;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common;
using WarehouseManagement.Application.Contracts;
using WarehouseManagement.Application.Contracts.Repositories;
using WarehouseManagement.Application.Contracts.Services;
using WarehouseManagement.Application.DTOs.Stock;
using WarehouseManagement.Domain.Entities;
using WarehouseManagement.Domain.Enums;

namespace WarehouseManagement.Application.Services;

public class StockService : IStockService
{
    private readonly IRepository<StockBalance> _balances;
    private readonly IRepository<StockMovement> _movements;
    private readonly IRepository<Product> _products;
    private readonly IRepository<Location> _locations;
    private readonly IRepository<Notification> _notifications;
    private readonly IRepository<User> _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    private readonly IRealtimeNotifier _realtime;
    private readonly IWarehouseAccessService _warehouseAccess;

    public StockService(
        IRepository<StockBalance> balances,
        IRepository<StockMovement> movements,
        IRepository<Product> products,
        IRepository<Location> locations,
        IRepository<Notification> notifications,
        IRepository<User> users,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUser,
        IRealtimeNotifier realtime,
        IWarehouseAccessService warehouseAccess)
    {
        _balances = balances;
        _movements = movements;
        _products = products;
        _locations = locations;
        _notifications = notifications;
        _users = users;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUser = currentUser;
        _realtime = realtime;
        _warehouseAccess = warehouseAccess;
    }

    public async Task<PagedResult<StockBalanceDto>> GetBalancesPagedAsync(PaginationParameters parameters, Guid? warehouseId, CancellationToken cancellationToken = default)
    {
        if (warehouseId.HasValue)
            await _warehouseAccess.EnsureCanAccessWarehouseAsync(warehouseId.Value, cancellationToken);

        var restricted = await _warehouseAccess.GetRestrictedWarehouseIdsAsync(cancellationToken);

        var query = _balances.Query()
            .Include(b => b.Product)
            .Include(b => b.Location).ThenInclude(l => l.Warehouse)
            .Where(b => b.Quantity != 0);

        if (warehouseId.HasValue)
            query = query.Where(b => b.Location.WarehouseId == warehouseId.Value);

        if (restricted != null)
            query = query.Where(b => restricted.Contains(b.Location.WarehouseId));

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var s = parameters.Search.Trim().ToLower();
            query = query.Where(b =>
                b.Product.Name.ToLower().Contains(s) ||
                b.Product.Sku.ToLower().Contains(s) ||
                b.Location.Code.ToLower().Contains(s));
        }

        query = parameters.SortDescending ? query.OrderByDescending(b => b.UpdatedDate ?? b.CreatedDate) : query.OrderBy(b => b.Location.Code);

        var page = await query.ToPagedResultAsync(parameters.PageNumber, parameters.PageSize, cancellationToken);
        var items = page.Items.Select(b => _mapper.Map<StockBalanceDto>(b)).ToList();

        return new PagedResult<StockBalanceDto>
        {
            Items = items,
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount
        };
    }

    public async Task<PagedResult<StockMovementDto>> GetMovementsPagedAsync(PaginationParameters parameters, Guid? productId, StockMovementType? type, CancellationToken cancellationToken = default)
    {
        var restricted = await _warehouseAccess.GetRestrictedWarehouseIdsAsync(cancellationToken);

        IQueryable<StockMovement> query = _movements.Query()
            .Include(m => m.Product)
            .Include(m => m.FromLocation)
            .Include(m => m.ToLocation)
            .Include(m => m.User);

        if (productId.HasValue)
            query = query.Where(m => m.ProductId == productId.Value);
        if (type.HasValue)
            query = query.Where(m => m.MovementType == type.Value);

        if (restricted != null)
        {
            query = query.Where(m =>
                (m.FromLocation != null && restricted.Contains(m.FromLocation.WarehouseId)) ||
                (m.ToLocation != null && restricted.Contains(m.ToLocation.WarehouseId)));
        }

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var s = parameters.Search.Trim().ToLower();
            query = query.Where(m =>
                m.Product.Name.ToLower().Contains(s) ||
                (m.Description != null && m.Description.ToLower().Contains(s)));
        }

        query = parameters.SortDescending ? query.OrderByDescending(m => m.CreatedDate) : query.OrderBy(m => m.CreatedDate);

        var page = await query.ToPagedResultAsync(parameters.PageNumber, parameters.PageSize, cancellationToken);
        var items = page.Items.Select(m => _mapper.Map<StockMovementDto>(m)).ToList();

        return new PagedResult<StockMovementDto>
        {
            Items = items,
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount
        };
    }

    public async Task StockEntryAsync(StockEntryRequestDto dto, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();
        await ValidateProductLocationAsync(dto.ProductId, dto.LocationId, cancellationToken);

        var (balance, isNew) = await GetOrCreateBalanceAsync(dto.ProductId, dto.LocationId, cancellationToken);
        balance.Quantity += dto.Quantity;

        var movement = NewMovement(StockMovementType.Entry, dto.Quantity, dto.Description, dto.ProductId, null, dto.LocationId, userId);
        await _movements.AddAsync(movement, cancellationToken);
        if (!isNew) _balances.Update(balance);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await NotifyProductAsync(dto.ProductId, cancellationToken);
    }

    public async Task StockExitAsync(StockExitRequestDto dto, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();
        await ValidateProductLocationAsync(dto.ProductId, dto.LocationId, cancellationToken);

        var balance = await _balances.Query().FirstOrDefaultAsync(b => b.ProductId == dto.ProductId && b.LocationId == dto.LocationId, cancellationToken)
                      ?? throw new InvalidOperationException("Bu lokasyonda stok kaydı yok.");

        if (balance.Quantity < dto.Quantity)
            throw new InvalidOperationException("Yetersiz stok.");

        balance.Quantity -= dto.Quantity;

        var movement = NewMovement(StockMovementType.Exit, dto.Quantity, dto.Description, dto.ProductId, dto.LocationId, null, userId);
        await _movements.AddAsync(movement, cancellationToken);
        _balances.Update(balance);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await NotifyProductAsync(dto.ProductId, cancellationToken);
    }

    public async Task StockTransferAsync(StockTransferRequestDto dto, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();
        await ValidateProductLocationAsync(dto.ProductId, dto.FromLocationId, cancellationToken);
        await ValidateProductLocationAsync(dto.ProductId, dto.ToLocationId, cancellationToken);

        var from = await _balances.Query().FirstOrDefaultAsync(b => b.ProductId == dto.ProductId && b.LocationId == dto.FromLocationId, cancellationToken)
                   ?? throw new InvalidOperationException("Kaynak lokasyonda stok kaydı yok.");
        if (from.Quantity < dto.Quantity)
            throw new InvalidOperationException("Kaynak lokasyonda yetersiz stok.");

        var (to, toIsNew) = await GetOrCreateBalanceAsync(dto.ProductId, dto.ToLocationId, cancellationToken);

        from.Quantity -= dto.Quantity;
        to.Quantity += dto.Quantity;

        var movement = NewMovement(StockMovementType.Transfer, dto.Quantity, dto.Description, dto.ProductId, dto.FromLocationId, dto.ToLocationId, userId);
        await _movements.AddAsync(movement, cancellationToken);
        _balances.Update(from);
        if (!toIsNew) _balances.Update(to);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await NotifyProductAsync(dto.ProductId, cancellationToken);
    }

    public async Task StockCountAsync(StockCountRequestDto dto, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();
        await ValidateProductLocationAsync(dto.ProductId, dto.LocationId, cancellationToken);

        var (balance, isNew) = await GetOrCreateBalanceAsync(dto.ProductId, dto.LocationId, cancellationToken);
        var diff = dto.CountedQuantity - balance.Quantity;
        if (diff == 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        balance.Quantity = dto.CountedQuantity;

        var movement = NewMovement(StockMovementType.CountDifference, diff, dto.Description, dto.ProductId, dto.LocationId, dto.LocationId, userId);
        await _movements.AddAsync(movement, cancellationToken);
        if (!isNew) _balances.Update(balance);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await NotifyProductAsync(dto.ProductId, cancellationToken);
    }

    private async Task NotifyProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        await _realtime.NotifyStockUpdatedAsync(productId, cancellationToken);
        await _realtime.NotifyDashboardChangedAsync(cancellationToken);
        await TryCriticalStockNotificationsAsync(productId, cancellationToken);
    }

    private async Task TryCriticalStockNotificationsAsync(Guid productId, CancellationToken cancellationToken)
    {
        var product = await _products.Query().Include(p => p.StockBalances).FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
        if (product == null) return;

        var total = product.StockBalances.Sum(b => b.Quantity);
        if (total > product.MinimumStockLevel) return;

        var targets = await _users.Query()
            .Include(u => u.Role)
            .Where(u => u.IsActive && (u.Role.Name == RoleNames.Admin || u.Role.Name == RoleNames.Manager))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        foreach (var uid in targets)
        {
            await _notifications.AddAsync(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = uid,
                Title = "Kritik stok",
                Message = $"{product.Name} ({product.Sku}) toplam stok {total}, minimum {product.MinimumStockLevel}.",
                Type = NotificationType.CriticalStock,
                IsRead = false,
                CreatedDate = DateTime.UtcNow
            }, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateProductLocationAsync(Guid productId, Guid locationId, CancellationToken cancellationToken)
    {
        if (!await _products.ExistsAsync(p => p.Id == productId && p.IsActive, cancellationToken))
            throw new InvalidOperationException("Ürün bulunamadı veya pasif.");

        var loc = await _locations.Query().FirstOrDefaultAsync(l => l.Id == locationId, cancellationToken)
                  ?? throw new InvalidOperationException("Lokasyon bulunamadı.");

        await _warehouseAccess.EnsureCanAccessWarehouseAsync(loc.WarehouseId, cancellationToken);
    }

    private async Task<(StockBalance Balance, bool IsNew)> GetOrCreateBalanceAsync(Guid productId, Guid locationId, CancellationToken cancellationToken)
    {
        var existing = await _balances.Query().FirstOrDefaultAsync(b => b.ProductId == productId && b.LocationId == locationId, cancellationToken);
        if (existing != null) return (existing, false);

        var created = new StockBalance
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            LocationId = locationId,
            Quantity = 0
        };
        await _balances.AddAsync(created, cancellationToken);
        return (created, true);
    }

    private static StockMovement NewMovement(
        StockMovementType type,
        int qty,
        string? description,
        Guid productId,
        Guid? fromId,
        Guid? toId,
        Guid userId) =>
        new()
        {
            Id = Guid.NewGuid(),
            MovementType = type,
            Quantity = qty,
            Description = description,
            ProductId = productId,
            FromLocationId = fromId,
            ToLocationId = toId,
            UserId = userId
        };
}
