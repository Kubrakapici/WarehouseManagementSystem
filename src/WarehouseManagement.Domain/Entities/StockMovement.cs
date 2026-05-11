using WarehouseManagement.Domain.Common;
using WarehouseManagement.Domain.Enums;

namespace WarehouseManagement.Domain.Entities;

public class StockMovement : BaseAuditableEntity
{
    public StockMovementType MovementType { get; set; }
    public int Quantity { get; set; }
    public string? Description { get; set; }

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public Guid? FromLocationId { get; set; }
    public Location? FromLocation { get; set; }

    public Guid? ToLocationId { get; set; }
    public Location? ToLocation { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}
