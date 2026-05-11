using Microsoft.EntityFrameworkCore;

using WarehouseManagement.Application.Common;

using WarehouseManagement.Application.Contracts;

using WarehouseManagement.Application.Contracts.Repositories;

using WarehouseManagement.Domain.Entities;



namespace WarehouseManagement.Application.Services;



public class WarehouseAccessService : IWarehouseAccessService

{

    private readonly IRepository<UserWarehouse> _userWarehouses;

    private readonly ICurrentUserService _currentUser;



    public WarehouseAccessService(IRepository<UserWarehouse> userWarehouses, ICurrentUserService currentUser)

    {

        _userWarehouses = userWarehouses;

        _currentUser = currentUser;

    }



    public async Task<HashSet<Guid>?> GetRestrictedWarehouseIdsAsync(CancellationToken cancellationToken = default)

    {

        if (string.Equals(_currentUser.Role, RoleNames.Admin, StringComparison.OrdinalIgnoreCase) ||

            string.Equals(_currentUser.Role, RoleNames.Manager, StringComparison.OrdinalIgnoreCase))

        {

            return null;

        }



        var userId = _currentUser.UserId;

        if (userId == null)

            return new HashSet<Guid>();



        var ids = await _userWarehouses.Query()

            .Where(x => x.UserId == userId.Value)

            .Select(x => x.WarehouseId)

            .ToListAsync(cancellationToken);



        if (ids.Count == 0)

            return null;



        return ids.ToHashSet();

    }



    public async Task EnsureCanAccessWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default)

    {

        var restricted = await GetRestrictedWarehouseIdsAsync(cancellationToken);

        if (restricted == null)

            return;



        if (!restricted.Contains(warehouseId))

            throw new UnauthorizedAccessException("Bu depo icin yetkiniz yok.");

    }

}


