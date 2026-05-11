using System.Collections.Concurrent;

using WarehouseManagement.Application.Contracts;



namespace WarehouseManagement.Infrastructure.Realtime;



public class ConnectionPresenceService : IPresenceService

{

    private readonly ConcurrentDictionary<string, Guid> _connectionToUser = new();



    public void AddConnection(Guid userId, string connectionId) =>

        _connectionToUser[connectionId] = userId;



    public void RemoveConnection(string connectionId) =>

        _connectionToUser.TryRemove(connectionId, out _);



    public int GetOnlineUserCount() =>

        _connectionToUser.Values.Distinct().Count();

}


