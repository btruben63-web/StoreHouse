using FluentValidation;
using Product.Application.Products.Validation;

namespace Product.Application.Products.Commands.DeleteProduct;

public sealed class DeleteProductCommandValidator : AbstractValidator<DeleteProductCommand>
{
    public DeleteProductCommandValidator() => RuleFor(x => x.Id).ValidProductId();
}
