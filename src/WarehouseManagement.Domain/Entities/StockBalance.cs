using WarehouseManagement.Domain.Common;

namespace WarehouseManagement.Domain.Entities;

public class StockBalance : BaseAuditableEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public Guid LocationId { get; set; }
    public Location Location { get; set; } = null!;

    public int Quantity { get; set; }
}
