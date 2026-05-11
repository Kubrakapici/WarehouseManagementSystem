using AutoMapper;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common;
using WarehouseManagement.Application.Contracts.Repositories;
using WarehouseManagement.Application.Contracts.Services;
using WarehouseManagement.Application.DTOs.Suppliers;
using WarehouseManagement.Domain.Entities;

namespace WarehouseManagement.Application.Services;

public class SupplierService : ISupplierService
{
    private readonly IRepository<Supplier> _suppliers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public SupplierService(IRepository<Supplier> suppliers, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _suppliers = suppliers;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedResult<SupplierDto>> GetPagedAsync(PaginationParameters parameters, CancellationToken cancellationToken = default)
    {
        var query = _suppliers.Query().Include(s => s.Orders).AsQueryable();

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var s = parameters.Search.Trim().ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(s) || (x.Email != null && x.Email.ToLower().Contains(s)));
        }

        query = parameters.SortDescending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name);

        var page = await query.ToPagedResultAsync(parameters.PageNumber, parameters.PageSize, cancellationToken);
        var items = page.Items.Select(s =>
        {
            var dto = _mapper.Map<SupplierDto>(s);
            dto.OrderCount = s.Orders.Count;
            return dto;
        }).ToList();

        return new PagedResult<SupplierDto>
        {
            Items = items,
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount
        };
    }

    public async Task<SupplierDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var s = await _suppliers.Query().Include(x => x.Orders).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (s == null) return null;
        var dto = _mapper.Map<SupplierDto>(s);
        dto.OrderCount = s.Orders.Count;
        return dto;
    }

    public async Task<SupplierDto> CreateAsync(CreateSupplierDto dto, CancellationToken cancellationToken = default)
    {
        var entity = new Supplier
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            TaxNumber = dto.TaxNumber,
            Phone = dto.Phone,
            Email = dto.Email,
            Address = dto.Address,
            IsActive = dto.IsActive
        };
        await _suppliers.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<SupplierDto?> UpdateAsync(Guid id, UpdateSupplierDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _suppliers.Query().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (entity == null) return null;

        entity.Name = dto.Name.Trim();
        entity.TaxNumber = dto.TaxNumber;
        entity.Phone = dto.Phone;
        entity.Email = dto.Email;
        entity.Address = dto.Address;
        entity.IsActive = dto.IsActive;
        _suppliers.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _suppliers.Query().Include(s => s.Orders).FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (entity == null) return false;
        if (entity.Orders.Count > 0) return false;

        _suppliers.Remove(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
