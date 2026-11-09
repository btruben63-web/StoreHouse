using FluentValidation;
using Product.Application.Products.Validation;

namespace Product.Application.Products.Queries.GetProductById;

public sealed class GetProductByIdQueryValidator : AbstractValidator<GetProductByIdQuery>
{
    public GetProductByIdQueryValidator() => RuleFor(x => x.Id).ValidProductId();
}
