using AutoMapper;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common;
using WarehouseManagement.Application.Contracts;
using WarehouseManagement.Application.Contracts.Repositories;
using WarehouseManagement.Application.Contracts.Services;
using WarehouseManagement.Application.DTOs.Locations;
using WarehouseManagement.Domain.Entities;

namespace WarehouseManagement.Application.Services;

public class LocationService : ILocationService
{
    private readonly IRepository<Location> _locations;
    private readonly IRepository<Warehouse> _warehouses;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IWarehouseAccessService _warehouseAccess;

    public LocationService(
        IRepository<Location> locations,
        IRepository<Warehouse> warehouses,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IWarehouseAccessService warehouseAccess)
    {
        _locations = locations;
        _warehouses = warehouses;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _warehouseAccess = warehouseAccess;
    }

    public async Task<PagedResult<LocationDto>> GetPagedAsync(PaginationParameters parameters, Guid? warehouseId, CancellationToken cancellationToken = default)
    {
        if (warehouseId.HasValue)
            await _warehouseAccess.EnsureCanAccessWarehouseAsync(warehouseId.Value, cancellationToken);

        var restricted = await _warehouseAccess.GetRestrictedWarehouseIdsAsync(cancellationToken);

        var query = _locations.Query().Include(l => l.Warehouse).AsQueryable();

        if (warehouseId.HasValue)
            query = query.Where(l => l.WarehouseId == warehouseId.Value);

        if (restricted != null)
            query = query.Where(l => restricted.Contains(l.WarehouseId));

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var s = parameters.Search.Trim().ToLower();
            query = query.Where(l => l.Code.ToLower().Contains(s));
        }

        query = parameters.SortDescending ? query.OrderByDescending(l => l.Code) : query.OrderBy(l => l.Code);

        var page = await query.ToPagedResultAsync(parameters.PageNumber, parameters.PageSize, cancellationToken);
        var items = page.Items.Select(l => _mapper.Map<LocationDto>(l)).ToList();

        return new PagedResult<LocationDto>
        {
            Items = items,
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount
        };
    }

    public async Task<LocationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var l = await _locations.Query().Include(x => x.Warehouse).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (l == null) return null;
        await _warehouseAccess.EnsureCanAccessWarehouseAsync(l.WarehouseId, cancellationToken);
        return _mapper.Map<LocationDto>(l);
    }

    public async Task<LocationDto> CreateAsync(CreateLocationDto dto, CancellationToken cancellationToken = default)
    {
        await _warehouseAccess.EnsureCanAccessWarehouseAsync(dto.WarehouseId, cancellationToken);

        if (!await _warehouses.ExistsAsync(w => w.Id == dto.WarehouseId, cancellationToken))
            throw new InvalidOperationException("Depo bulunamadı.");

        var corridor = dto.Corridor.Trim();
        var shelf = dto.Shelf.Trim();
        var floor = dto.Floor.Trim();
        var code = $"{corridor}-{shelf}-{floor}";

        if (await _locations.ExistsAsync(l => l.WarehouseId == dto.WarehouseId && l.Code == code, cancellationToken))
            throw new InvalidOperationException("Bu depoda aynı lokasyon kodu zaten var.");

        var entity = new Location
        {
            Id = Guid.NewGuid(),
            Corridor = corridor,
            Shelf = shelf,
            Floor = floor,
            Code = code,
            WarehouseId = dto.WarehouseId,
            MaxCapacity = dto.MaxCapacity,
            PickSortOrder = dto.PickSortOrder
        };
        await _locations.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<LocationDto?> UpdateAsync(Guid id, UpdateLocationDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _locations.Query().FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (entity == null) return null;

        await _warehouseAccess.EnsureCanAccessWarehouseAsync(entity.WarehouseId, cancellationToken);

        entity.Corridor = dto.Corridor.Trim();
        entity.Shelf = dto.Shelf.Trim();
        entity.Floor = dto.Floor.Trim();
        entity.Code = $"{entity.Corridor}-{entity.Shelf}-{entity.Floor}";
        entity.MaxCapacity = dto.MaxCapacity;
        entity.PickSortOrder = dto.PickSortOrder;

        if (await _locations.ExistsAsync(l => l.WarehouseId == entity.WarehouseId && l.Code == entity.Code && l.Id != id, cancellationToken))
            throw new InvalidOperationException("Bu depoda aynı lokasyon kodu zaten var.");

        _locations.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _locations.Query().Include(l => l.StockBalances).FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (entity == null) return false;

        await _warehouseAccess.EnsureCanAccessWarehouseAsync(entity.WarehouseId, cancellationToken);

        if (entity.StockBalances.Any(b => b.Quantity != 0)) return false;

        _locations.Remove(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
