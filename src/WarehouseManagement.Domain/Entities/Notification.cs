using WarehouseManagement.Domain.Common;
using WarehouseManagement.Domain.Enums;

namespace WarehouseManagement.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedDate { get; set; }
}
