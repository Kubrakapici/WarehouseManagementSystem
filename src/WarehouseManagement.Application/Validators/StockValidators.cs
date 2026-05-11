using FluentValidation;
using WarehouseManagement.Application.DTOs.Stock;

namespace WarehouseManagement.Application.Validators;

public class StockEntryRequestValidator : AbstractValidator<StockEntryRequestDto>
{
    public StockEntryRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.LocationId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

public class StockExitRequestValidator : AbstractValidator<StockExitRequestDto>
{
    public StockExitRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.LocationId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

public class StockTransferRequestValidator : AbstractValidator<StockTransferRequestDto>
{
    public StockTransferRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.FromLocationId).NotEmpty();
        RuleFor(x => x.ToLocationId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x).Must(x => x.FromLocationId != x.ToLocationId).WithMessage("Kaynak ve hedef lokasyon farklı olmalıdır.");
    }
}

public class StockCountRequestValidator : AbstractValidator<StockCountRequestDto>
{
    public StockCountRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.LocationId).NotEmpty();
        RuleFor(x => x.CountedQuantity).GreaterThanOrEqualTo(0);
    }
}
