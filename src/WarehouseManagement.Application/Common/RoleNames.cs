namespace WarehouseManagement.Application.Common;

public static class RoleNames
{
    public const string Admin = "Admin";
    public const string WarehouseStaff = "WarehouseStaff";
    public const string Operations = "Operations";
    public const string Manager = "Manager";
    public const string Viewer = "Viewer";

    public static IReadOnlyList<string> All => new[] { Admin, WarehouseStaff, Operations, Manager, Viewer };
}
