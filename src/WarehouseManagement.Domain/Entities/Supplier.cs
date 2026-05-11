using WarehouseManagement.Domain.Common;

namespace WarehouseManagement.Domain.Entities;

public class Supplier : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? TaxNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<SupplierQuote> SupplierQuotes { get; set; } = new List<SupplierQuote>();
}
