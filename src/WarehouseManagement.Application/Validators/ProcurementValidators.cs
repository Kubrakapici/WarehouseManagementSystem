using FluentValidation;
using WarehouseManagement.Application.DTOs.Procurement;

namespace WarehouseManagement.Application.Validators;

public class CreatePurchaseRequisitionValidator : AbstractValidator<CreatePurchaseRequisitionDto>
{
    public CreatePurchaseRequisitionValidator()
    {
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThan(0);
        });
    }
}

public class AddSupplierQuoteValidator : AbstractValidator<AddSupplierQuoteDto>
{
    public AddSupplierQuoteValidator()
    {
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.TotalAmount).GreaterThan(0);
    }
}
