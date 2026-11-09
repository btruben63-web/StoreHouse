using FluentValidation;
using Product.Application.Products.Validation;

namespace Product.Application.Products.Queries.SearchProductsByStockRange;

public sealed class SearchProductsByStockRangeQueryValidator : AbstractValidator<SearchProductsByStockRangeQuery>
{
    public SearchProductsByStockRangeQueryValidator()
    {
        RuleFor(x => x.Min).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Max).GreaterThanOrEqualTo(x => x.Min);
        RuleFor(x => x.LastItemId).ValidCursor();
        RuleFor(x => x.PageSize).ValidPageSize();
    }
}
