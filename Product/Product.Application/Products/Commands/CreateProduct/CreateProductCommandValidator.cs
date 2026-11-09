using FluentValidation;
using Product.Application.Products.Validation;

namespace Product.Application.Products.Commands.CreateProduct;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Product).NotNull();

        When(x => x.Product is not null, () =>
        {
            RuleFor(x => x.Product.Name).ValidProductName();
            RuleFor(x => x.Product.Description).ValidDescription();
            RuleFor(x => x.Product.Price).ValidPrice();
            RuleFor(x => x.Product.InitialStock).ValidInitialStock();
        });
    }
}
