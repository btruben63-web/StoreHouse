using FluentValidation;
using Product.Application.Products.Validation;

namespace Product.Application.Products.Queries.GetProducts;

public sealed class GetProductsQueryValidator : AbstractValidator<GetProductsQuery>
{
    public GetProductsQueryValidator()
    {
        RuleFor(x => x.LastItemId).ValidCursor();
        RuleFor(x => x.PageSize).ValidPageSize();
    }
}
