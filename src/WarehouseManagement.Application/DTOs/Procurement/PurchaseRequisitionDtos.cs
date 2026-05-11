using WarehouseManagement.Domain.Enums;

namespace WarehouseManagement.Application.DTOs.Procurement;

public class PurchaseRequisitionLineDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? Notes { get; set; }
}

public class SupplierQuoteDto
{
    public Guid Id { get; set; }
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "TRY";
    public SupplierQuoteStatus Status { get; set; }
}

public class PurchaseRequisitionDto
{
    public Guid Id { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string? Title { get; set; }
    public PurchaseRequisitionStatus Status { get; set; }
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public Guid RequestedByUserId { get; set; }
    public string RequestedByName { get; set; } = string.Empty;
    public Guid? ApprovedByUserId { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? Notes { get; set; }
    public IReadOnlyList<PurchaseRequisitionLineDto> Lines { get; set; } = Array.Empty<PurchaseRequisitionLineDto>();
    public IReadOnlyList<SupplierQuoteDto> Quotes { get; set; } = Array.Empty<SupplierQuoteDto>();
}

public class CreatePurchaseRequisitionLineDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public string? Notes { get; set; }
}

public class CreatePurchaseRequisitionDto
{
    public Guid WarehouseId { get; set; }
    public string? Title { get; set; }
    public string? Notes { get; set; }
    public IReadOnlyList<CreatePurchaseRequisitionLineDto> Lines { get; set; } = Array.Empty<CreatePurchaseRequisitionLineDto>();
}

public class AddSupplierQuoteDto
{
    public Guid SupplierId { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Currency { get; set; }
    public string? Notes { get; set; }
}

public class RejectPurchaseRequisitionDto
{
    public string? Reason { get; set; }
}

public class PurchaseSuggestionLineDto
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public int CurrentQuantity { get; set; }
    public int MinimumStockLevel { get; set; }
    public int SuggestedOrderQuantity { get; set; }
}

public class BulkProductQrLabelsDto
{
    public IReadOnlyList<Guid> ProductIds { get; set; } = Array.Empty<Guid>();
}
