using WarehouseManagement.Domain.Common;

namespace WarehouseManagement.Domain.Entities;

public class Location : BaseAuditableEntity
{
    public string Corridor { get; set; } = string.Empty;
    public string Shelf { get; set; } = string.Empty;
    public string Floor { get; set; } = string.Empty;

    /// <summary>Örn: A-01-B-03</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Raf kapasitesi (birim: stok adedi). Doluluk hesaplamalari icin.</summary>
    public int? MaxCapacity { get; set; }

    /// <summary>Toplama yolu / oncelik (dusuk deger once).</summary>
    public int? PickSortOrder { get; set; }

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    public ICollection<StockBalance> StockBalances { get; set; } = new List<StockBalance>();
    public ICollection<StockMovement> MovementsFrom { get; set; } = new List<StockMovement>();
    public ICollection<StockMovement> MovementsTo { get; set; } = new List<StockMovement>();
}
