using AutoMapper;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common;
using WarehouseManagement.Application.Contracts;
using WarehouseManagement.Application.Contracts.Repositories;
using WarehouseManagement.Application.Contracts.Services;
using WarehouseManagement.Application.DTOs.Warehouses;
using WarehouseManagement.Domain.Entities;

namespace WarehouseManagement.Application.Services;

public class WarehouseService : IWarehouseService
{
    private readonly IRepository<Warehouse> _warehouses;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IWarehouseAccessService _warehouseAccess;

    public WarehouseService(
        IRepository<Warehouse> warehouses,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IWarehouseAccessService warehouseAccess)
    {
        _warehouses = warehouses;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _warehouseAccess = warehouseAccess;
    }

    public async Task<PagedResult<WarehouseDto>> GetPagedAsync(PaginationParameters parameters, CancellationToken cancellationToken = default)
    {
        var restricted = await _warehouseAccess.GetRestrictedWarehouseIdsAsync(cancellationToken);

        var query = _warehouses.Query().Include(w => w.Locations).AsQueryable();

        if (restricted != null)
            query = query.Where(w => restricted.Contains(w.Id));

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var s = parameters.Search.Trim().ToLower();
            query = query.Where(w => w.Name.ToLower().Contains(s) || (w.City != null && w.City.ToLower().Contains(s)));
        }

        query = parameters.SortDescending ? query.OrderByDescending(w => w.Name) : query.OrderBy(w => w.Name);

        var page = await query.ToPagedResultAsync(parameters.PageNumber, parameters.PageSize, cancellationToken);
        var items = page.Items.Select(w =>
        {
            var dto = _mapper.Map<WarehouseDto>(w);
            dto.LocationCount = w.Locations.Count;
            return dto;
        }).ToList();

        return new PagedResult<WarehouseDto>
        {
            Items = items,
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount
        };
    }

    public async Task<WarehouseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _warehouseAccess.EnsureCanAccessWarehouseAsync(id, cancellationToken);

        var w = await _warehouses.Query().Include(x => x.Locations).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (w == null) return null;
        var dto = _mapper.Map<WarehouseDto>(w);
        dto.LocationCount = w.Locations.Count;
        return dto;
    }

    public async Task<WarehouseDto> CreateAsync(CreateWarehouseDto dto, CancellationToken cancellationToken = default)
    {
        var entity = new Warehouse
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Address = dto.Address,
            City = dto.City,
            IsActive = dto.IsActive
        };
        await _warehouses.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<WarehouseDto?> UpdateAsync(Guid id, UpdateWarehouseDto dto, CancellationToken cancellationToken = default)
    {
        await _warehouseAccess.EnsureCanAccessWarehouseAsync(id, cancellationToken);

        var entity = await _warehouses.Query().FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (entity == null) return null;

        entity.Name = dto.Name.Trim();
        entity.Address = dto.Address;
        entity.City = dto.City;
        entity.IsActive = dto.IsActive;
        _warehouses.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _warehouseAccess.EnsureCanAccessWarehouseAsync(id, cancellationToken);

        var entity = await _warehouses.Query().Include(w => w.Locations).FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (entity == null) return false;
        if (entity.Locations.Count > 0) return false;

        _warehouses.Remove(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
