namespace WarehouseManagement.Application.DTOs.Locations;

public class LocationDto
{
    public Guid Id { get; set; }
    public string Corridor { get; set; } = string.Empty;
    public string Shelf { get; set; } = string.Empty;
    public string Floor { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int? MaxCapacity { get; set; }
    public int? PickSortOrder { get; set; }
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
}

public class CreateLocationDto
{
    public string Corridor { get; set; } = string.Empty;
    public string Shelf { get; set; } = string.Empty;
    public string Floor { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public int? MaxCapacity { get; set; }
    public int? PickSortOrder { get; set; }
}

public class UpdateLocationDto
{
    public string Corridor { get; set; } = string.Empty;
    public string Shelf { get; set; } = string.Empty;
    public string Floor { get; set; } = string.Empty;
    public int? MaxCapacity { get; set; }
    public int? PickSortOrder { get; set; }
}
