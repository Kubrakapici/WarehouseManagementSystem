namespace WarehouseManagement.Application.DTOs.Stock;

public class StockBalanceDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public Guid LocationId { get; set; }
    public string LocationCode { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public class StockMovementDto
{
    public Guid Id { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? Description { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public Guid? FromLocationId { get; set; }
    public string? FromLocationCode { get; set; }
    public Guid? ToLocationId { get; set; }
    public string? ToLocationCode { get; set; }
    public Guid UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}

public class StockEntryRequestDto
{
    public Guid ProductId { get; set; }
    public Guid LocationId { get; set; }
    public int Quantity { get; set; }
    public string? Description { get; set; }
}

public class StockExitRequestDto
{
    public Guid ProductId { get; set; }
    public Guid LocationId { get; set; }
    public int Quantity { get; set; }
    public string? Description { get; set; }
}

public class StockTransferRequestDto
{
    public Guid ProductId { get; set; }
    public Guid FromLocationId { get; set; }
    public Guid ToLocationId { get; set; }
    public int Quantity { get; set; }
    public string? Description { get; set; }
}

public class StockCountRequestDto
{
    public Guid ProductId { get; set; }
    public Guid LocationId { get; set; }
    public int CountedQuantity { get; set; }
    public string? Description { get; set; }
}
