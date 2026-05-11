using WarehouseManagement.Domain.Common;



namespace WarehouseManagement.Domain.Entities;



/// <summary>Warehouse scope for users; when empty, access is not restricted by warehouse (legacy).</summary>

public class UserWarehouse : BaseEntity

{

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;



    public Guid WarehouseId { get; set; }

    public Warehouse Warehouse { get; set; } = null!;

}


