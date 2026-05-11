using Microsoft.AspNetCore.SignalR;
using WarehouseManagement.Application.Contracts;
using WarehouseManagement.Domain.Enums;

namespace WarehouseManagement.Infrastructure.Realtime;

public class SignalRRealtimeNotifier : IRealtimeNotifier
{
    private readonly IHubContext<StockHub> _hub;

    public SignalRRealtimeNotifier(IHubContext<StockHub> hub)
    {
        _hub = hub;
    }

    public Task NotifyStockUpdatedAsync(Guid productId, CancellationToken cancellationToken = default) =>
        _hub.Clients.Group($"product-{productId}").SendAsync("StockUpdated", productId, cancellationToken);

    public Task NotifyUserAsync(Guid userId, string title, string message, CancellationToken cancellationToken = default) =>
        _hub.Clients.User(userId.ToString()).SendAsync("Notification", new { title, message }, cancellationToken);

    public Task NotifyDashboardChangedAsync(CancellationToken cancellationToken = default) =>
        _hub.Clients.Group("dashboard").SendAsync("DashboardChanged", cancellationToken);

    public Task NotifyOrderStatusChangedAsync(Guid orderId, OrderStatus status, CancellationToken cancellationToken = default) =>
        _hub.Clients.Group("orders").SendAsync("OrderStatusChanged", new { orderId, status = status.ToString() }, cancellationToken);
}
