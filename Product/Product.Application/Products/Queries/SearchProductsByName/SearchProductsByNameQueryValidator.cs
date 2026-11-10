using FluentValidation;
using Product.Application.Products.Validation;
using Product.Domain.Constants;

namespace Product.Application.Products.Queries.SearchProductsByName;

public sealed class SearchProductsByNameQueryValidator : AbstractValidator<SearchProductsByNameQuery>
{
    public SearchProductsByNameQueryValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(name => !string.IsNullOrWhiteSpace(name) && name.Trim().Length <= ProductConstraints.NameMaxLength)
            .WithMessage($"'Name' must have between 1 and {ProductConstraints.NameMaxLength} characters.");
        RuleFor(x => x.LastItemId).ValidCursor();
        RuleFor(x => x.PageSize).ValidPageSize();
    }
}
