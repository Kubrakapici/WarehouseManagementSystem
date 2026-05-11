namespace WarehouseManagement.Application.Contracts;



/// <summary>

/// Depo bazli yetkilendirme. Admin ve Manager her zaman tum depolara erisir.

/// Diger rollerde <see cref="UserWarehouse"/> kaydi varsa sadece bu depolar.

/// </summary>

public interface IWarehouseAccessService

{

    /// <summary>null: kisitlama yok (tum depolar).</summary>

    Task<HashSet<Guid>?> GetRestrictedWarehouseIdsAsync(CancellationToken cancellationToken = default);



    Task EnsureCanAccessWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default);

}


