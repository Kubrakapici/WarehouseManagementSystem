using WarehouseManagement.Domain.Common;



namespace WarehouseManagement.Domain.Entities;



public class PurchaseRequisitionLine : BaseAuditableEntity

{

    public Guid PurchaseRequisitionId { get; set; }

    public PurchaseRequisition PurchaseRequisition { get; set; } = null!;



    public Guid ProductId { get; set; }

    public Product Product { get; set; } = null!;



    public int Quantity { get; set; }

    public string? Notes { get; set; }

}


