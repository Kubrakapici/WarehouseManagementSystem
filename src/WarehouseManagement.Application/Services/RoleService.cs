using AutoMapper;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Contracts.Repositories;
using WarehouseManagement.Application.Contracts.Services;
using WarehouseManagement.Application.DTOs.Roles;
using WarehouseManagement.Domain.Entities;

namespace WarehouseManagement.Application.Services;

public class RoleService : IRoleService
{
    private readonly IRepository<Role> _roles;
    private readonly IMapper _mapper;

    public RoleService(IRepository<Role> roles, IMapper mapper)
    {
        _roles = roles;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<RoleDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _roles.Query().OrderBy(r => r.Name).ToListAsync(cancellationToken);
        return _mapper.Map<IReadOnlyList<RoleDto>>(roles);
    }
}
