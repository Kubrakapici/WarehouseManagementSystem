using WarehouseManagement.Domain.Common;

namespace WarehouseManagement.Domain.Entities;

public class User : BaseAuditableEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }

    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<UserWarehouse> UserWarehouses { get; set; } = new List<UserWarehouse>();
    public ICollection<PurchaseRequisition> PurchaseRequisitionsRequested { get; set; } = new List<PurchaseRequisition>();
    public ICollection<PurchaseRequisition> PurchaseRequisitionsApproved { get; set; } = new List<PurchaseRequisition>();
}
