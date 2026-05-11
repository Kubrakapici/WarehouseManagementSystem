using WarehouseManagement.Domain.Enums;

namespace WarehouseManagement.Application.Contracts;

public interface IRealtimeNotifier
{
    Task NotifyStockUpdatedAsync(Guid productId, CancellationToken cancellationToken = default);
    Task NotifyUserAsync(Guid userId, string title, string message, CancellationToken cancellationToken = default);
    Task NotifyDashboardChangedAsync(CancellationToken cancellationToken = default);
    Task NotifyOrderStatusChangedAsync(Guid orderId, OrderStatus status, CancellationToken cancellationToken = default);
}
