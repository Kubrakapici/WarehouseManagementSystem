using WarehouseManagement.Domain.Common;

using WarehouseManagement.Domain.Enums;



namespace WarehouseManagement.Domain.Entities;



public class SupplierQuote : BaseAuditableEntity

{

    public Guid PurchaseRequisitionId { get; set; }

    public PurchaseRequisition PurchaseRequisition { get; set; } = null!;



    public Guid SupplierId { get; set; }

    public Supplier Supplier { get; set; } = null!;



    public decimal TotalAmount { get; set; }

    public string Currency { get; set; } = "TRY";

    public SupplierQuoteStatus Status { get; set; } = SupplierQuoteStatus.Pending;

    public string? Notes { get; set; }

}


