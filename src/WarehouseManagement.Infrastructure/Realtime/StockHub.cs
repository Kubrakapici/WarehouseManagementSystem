using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.SignalR;

using WarehouseManagement.Application.Contracts;



namespace WarehouseManagement.Infrastructure.Realtime;



[Authorize]

public class StockHub : Hub

{

    private readonly IPresenceService _presence;



    public StockHub(IPresenceService presence)

    {

        _presence = presence;

    }



    public override Task OnConnectedAsync()

    {

        var uid = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (Guid.TryParse(uid, out var userId))

            _presence.AddConnection(userId, Context.ConnectionId);



        return base.OnConnectedAsync();

    }



    public override Task OnDisconnectedAsync(Exception? exception)

    {

        _presence.RemoveConnection(Context.ConnectionId);

        return base.OnDisconnectedAsync(exception);

    }



    public Task SubscribeProduct(Guid productId) =>

        Groups.AddToGroupAsync(Context.ConnectionId, $"product-{productId}");



    public Task UnsubscribeProduct(Guid productId) =>

        Groups.RemoveFromGroupAsync(Context.ConnectionId, $"product-{productId}");



    public Task SubscribeDashboard() =>

        Groups.AddToGroupAsync(Context.ConnectionId, "dashboard");



    public Task UnsubscribeDashboard() =>

        Groups.RemoveFromGroupAsync(Context.ConnectionId, "dashboard");



    public Task SubscribeOrders() =>

        Groups.AddToGroupAsync(Context.ConnectionId, "orders");



    public Task UnsubscribeOrders() =>

        Groups.RemoveFromGroupAsync(Context.ConnectionId, "orders");

}


