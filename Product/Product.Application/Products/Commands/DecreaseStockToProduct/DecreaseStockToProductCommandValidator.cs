using FluentValidation;
using Product.Application.Products.Validation;

namespace Product.Application.Products.Commands.DecreaseStockToProduct;

public sealed class DecreaseStockToProductCommandValidator : AbstractValidator<DecreaseStockToProductCommand>
{
    public DecreaseStockToProductCommandValidator()
    {
        RuleFor(x => x.Id).ValidProductId();
        RuleFor(x => x.Quantity).ValidStockQuantity();
    }
}
