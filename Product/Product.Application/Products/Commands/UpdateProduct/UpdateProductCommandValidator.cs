using FluentValidation;
using Product.Application.Products.Validation;

namespace Product.Application.Products.Commands.UpdateProduct;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id).ValidProductId();
        RuleFor(x => x.Product).NotNull();

        When(x => x.Product is not null, () =>
        {
            RuleFor(x => x.Product.Name).ValidProductName();
            RuleFor(x => x.Product.Description).ValidDescription();
            RuleFor(x => x.Product.Price).ValidPrice();
        });
    }
}
