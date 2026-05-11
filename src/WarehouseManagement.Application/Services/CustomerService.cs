using AutoMapper;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common;
using WarehouseManagement.Application.Contracts.Repositories;
using WarehouseManagement.Application.Contracts.Services;
using WarehouseManagement.Application.DTOs.Customers;
using WarehouseManagement.Domain.Entities;

namespace WarehouseManagement.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly IRepository<Customer> _customers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CustomerService(IRepository<Customer> customers, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedResult<CustomerDto>> GetPagedAsync(PaginationParameters parameters, CancellationToken cancellationToken = default)
    {
        var query = _customers.Query().AsQueryable();

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var s = parameters.Search.Trim().ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(s)
                || (c.CompanyName != null && c.CompanyName.ToLower().Contains(s))
                || (c.Email != null && c.Email.ToLower().Contains(s))
                || (c.City != null && c.City.ToLower().Contains(s)));
        }

        query = parameters.SortDescending ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name);

        var page = await query.ToPagedResultAsync(parameters.PageNumber, parameters.PageSize, cancellationToken);
        var items = page.Items.Select(c => _mapper.Map<CustomerDto>(c)).ToList();

        return new PagedResult<CustomerDto>
        {
            Items = items,
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount
        };
    }

    public async Task<CustomerDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _customers.Query().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        return entity == null ? null : _mapper.Map<CustomerDto>(entity);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("M\u00fc\u015fteri ad\u0131 zorunludur.");

        var entity = new Customer
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            CompanyName = NormalizeOptional(dto.CompanyName),
            TaxNumber = NormalizeOptional(dto.TaxNumber),
            Phone = NormalizeOptional(dto.Phone),
            Email = NormalizeOptional(dto.Email),
            Address = NormalizeOptional(dto.Address),
            City = NormalizeOptional(dto.City),
            Notes = NormalizeOptional(dto.Notes),
            IsActive = dto.IsActive
        };

        await _customers.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<CustomerDto?> UpdateAsync(Guid id, UpdateCustomerDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("M\u00fc\u015fteri ad\u0131 zorunludur.");

        var entity = await _customers.Query().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (entity == null) return null;

        entity.Name = dto.Name.Trim();
        entity.CompanyName = NormalizeOptional(dto.CompanyName);
        entity.TaxNumber = NormalizeOptional(dto.TaxNumber);
        entity.Phone = NormalizeOptional(dto.Phone);
        entity.Email = NormalizeOptional(dto.Email);
        entity.Address = NormalizeOptional(dto.Address);
        entity.City = NormalizeOptional(dto.City);
        entity.Notes = NormalizeOptional(dto.Notes);
        entity.IsActive = dto.IsActive;

        _customers.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _customers.Query().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (entity == null) return false;

        _customers.Remove(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Trim();
    }
}
