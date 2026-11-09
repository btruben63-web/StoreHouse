using FluentValidation;
using Product.Application.Products.Validation;

namespace Product.Application.Products.Commands.AddStockToProduct;

public sealed class AddStockToProductCommandValidator : AbstractValidator<AddStockToProductCommand>
{
    public AddStockToProductCommandValidator()
    {
        RuleFor(x => x.Id).ValidProductId();
        RuleFor(x => x.Quantity).ValidStockQuantity();
    }
}
