namespace WarehouseManagement.Application.Contracts;



public interface IPresenceService

{

    void AddConnection(Guid userId, string connectionId);



    void RemoveConnection(string connectionId);



    int GetOnlineUserCount();

}


