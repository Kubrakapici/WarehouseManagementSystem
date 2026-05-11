using WarehouseManagement.Domain.Common;

using WarehouseManagement.Domain.Enums;



namespace WarehouseManagement.Domain.Entities;



public class PurchaseRequisition : BaseAuditableEntity

{

    public string RequestNumber { get; set; } = string.Empty;

    public string? Title { get; set; }

    public PurchaseRequisitionStatus Status { get; set; } = PurchaseRequisitionStatus.Draft;



    public Guid WarehouseId { get; set; }

    public Warehouse Warehouse { get; set; } = null!;



    public Guid RequestedByUserId { get; set; }

    public User RequestedByUser { get; set; } = null!;



    public Guid? ApprovedByUserId { get; set; }

    public User? ApprovedByUser { get; set; }



    public DateTime? ApprovedDate { get; set; }

    public string? Notes { get; set; }



    public ICollection<PurchaseRequisitionLine> Lines { get; set; } = new List<PurchaseRequisitionLine>();

    public ICollection<SupplierQuote> SupplierQuotes { get; set; } = new List<SupplierQuote>();

}


