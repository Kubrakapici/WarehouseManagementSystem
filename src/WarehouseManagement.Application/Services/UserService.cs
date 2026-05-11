using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common;
using WarehouseManagement.Application.Contracts;
using WarehouseManagement.Application.Contracts.Repositories;
using WarehouseManagement.Application.Contracts.Services;
using WarehouseManagement.Application.DTOs.Users;
using WarehouseManagement.Domain.Entities;

namespace WarehouseManagement.Application.Services;

public class UserService : IUserService
{
    private readonly IRepository<User> _users;
    private readonly IRepository<Role> _roles;
    private readonly IRepository<UserWarehouse> _userWarehouses;
    private readonly IRepository<Warehouse> _warehouses;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;

    public UserService(
        IRepository<User> users,
        IRepository<Role> roles,
        IRepository<UserWarehouse> userWarehouses,
        IRepository<Warehouse> warehouses,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUser)
    {
        _users = users;
        _roles = roles;
        _userWarehouses = userWarehouses;
        _warehouses = warehouses;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<UserDto>> GetPagedAsync(PaginationParameters parameters, CancellationToken cancellationToken = default)
    {
        var query = _users.Query().Include(u => u.Role).Include(u => u.UserWarehouses).AsQueryable();
        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var s = parameters.Search.Trim().ToLower();
            query = query.Where(u =>
                u.Email.ToLower().Contains(s) ||
                u.FirstName.ToLower().Contains(s) ||
                u.LastName.ToLower().Contains(s));
        }

        query = parameters.SortBy?.ToLower() switch
        {
            "email" => parameters.SortDescending ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
            "firstname" => parameters.SortDescending ? query.OrderByDescending(u => u.FirstName) : query.OrderBy(u => u.FirstName),
            _ => parameters.SortDescending ? query.OrderByDescending(u => u.CreatedDate) : query.OrderBy(u => u.CreatedDate)
        };

        var paged = await query.ProjectTo<UserDto>(_mapper.ConfigurationProvider)
            .ToPagedResultAsync(parameters.PageNumber, parameters.PageSize, cancellationToken);
        return paged;
    }

    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _users.Query().Include(u => u.Role).Include(u => u.UserWarehouses).FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        return user == null ? null : _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto> CreateAsync(CreateUserDto dto, CancellationToken cancellationToken = default)
    {
        if (!await _roles.ExistsAsync(r => r.Id == dto.RoleId, cancellationToken))
            throw new InvalidOperationException("Geçersiz rol.");

        await ValidateWarehouseAssignmentsAsync(dto.WarehouseIds, cancellationToken);

        var entity = new User
        {
            Id = Guid.NewGuid(),
            Email = dto.Email.Trim().ToLowerInvariant(),
            PasswordHash = _passwordHasher.HashPassword(dto.Password),
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            IsActive = dto.IsActive,
            RoleId = dto.RoleId
        };
        await _users.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await SyncUserWarehousesAsync(entity.Id, dto.WarehouseIds, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var created = await _users.Query().Include(u => u.Role).Include(u => u.UserWarehouses).FirstAsync(u => u.Id == entity.Id, cancellationToken);
        return _mapper.Map<UserDto>(created);
    }

    public async Task<UserDto?> UpdateAsync(Guid id, UpdateUserDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _users.Query().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user == null) return null;

        if (!await _roles.ExistsAsync(r => r.Id == dto.RoleId, cancellationToken))
            throw new InvalidOperationException("Geçersiz rol.");

        await ValidateWarehouseAssignmentsAsync(dto.WarehouseIds, cancellationToken);

        user.FirstName = dto.FirstName.Trim();
        user.LastName = dto.LastName.Trim();
        user.RoleId = dto.RoleId;
        user.IsActive = dto.IsActive;
        if (!string.IsNullOrWhiteSpace(dto.NewPassword))
            user.PasswordHash = _passwordHasher.HashPassword(dto.NewPassword);

        _users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await SyncUserWarehousesAsync(id, dto.WarehouseIds, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await _users.Query().Include(u => u.Role).Include(u => u.UserWarehouses).FirstAsync(u => u.Id == id, cancellationToken);
        return _mapper.Map<UserDto>(updated);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId == id)
            return false;

        var user = await _users.Query().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user == null) return false;

        _users.Remove(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task ValidateWarehouseAssignmentsAsync(IReadOnlyList<Guid>? warehouseIds, CancellationToken cancellationToken)
    {
        if (warehouseIds == null || warehouseIds.Count == 0)
            return;

        foreach (var wid in warehouseIds.Distinct())
        {
            if (!await _warehouses.ExistsAsync(w => w.Id == wid, cancellationToken))
                throw new InvalidOperationException($"Depo bulunamadı: {wid}");
        }
    }

    private async Task SyncUserWarehousesAsync(Guid userId, IReadOnlyList<Guid>? warehouseIds, CancellationToken cancellationToken)
    {
        var existing = await _userWarehouses.Query().Where(x => x.UserId == userId).ToListAsync(cancellationToken);
        foreach (var e in existing)
            _userWarehouses.Remove(e);

        if (warehouseIds == null || warehouseIds.Count == 0)
            return;

        foreach (var wid in warehouseIds.Distinct())
        {
            await _userWarehouses.AddAsync(new UserWarehouse
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                WarehouseId = wid
            }, cancellationToken);
        }
    }
}
