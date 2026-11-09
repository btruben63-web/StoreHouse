using FluentValidation;
using Product.Domain.Constants;

namespace Product.Application.Products.Validation;

public static class ProductRules
{
    public static IRuleBuilderOptions<T, int> ValidProductId<T>(this IRuleBuilder<T, int> rule) =>
        rule.InclusiveBetween(ProductConstraints.MinId, ProductConstraints.MaxId);

    public static IRuleBuilderOptions<T, string> ValidProductName<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MaximumLength(ProductConstraints.NameMaxLength);

    public static IRuleBuilderOptions<T, string?> ValidDescription<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(ProductConstraints.DescriptionMaxLength);

    public static IRuleBuilderOptions<T, decimal> ValidPrice<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.GreaterThanOrEqualTo(ProductConstraints.MinPrice)
            .LessThanOrEqualTo(ProductConstraints.MaxPrice)
            .PrecisionScale(ProductConstraints.PricePrecision, ProductConstraints.PriceScale, ignoreTrailingZeros: true);

    public static IRuleBuilderOptions<T, int> ValidInitialStock<T>(this IRuleBuilder<T, int> rule) =>
        rule.InclusiveBetween(0, ProductConstraints.MaxQuantityPerOperation);

    public static IRuleBuilderOptions<T, int> ValidStockQuantity<T>(this IRuleBuilder<T, int> rule) =>
        rule.InclusiveBetween(1, ProductConstraints.MaxQuantityPerOperation);

    public static IRuleBuilderOptions<T, int> ValidPageSize<T>(this IRuleBuilder<T, int> rule) =>
        rule.InclusiveBetween(1, ProductConstraints.MaxPageSize);

    public static IRuleBuilderOptions<T, int?> ValidCursor<T>(this IRuleBuilder<T, int?> rule) =>
        rule.InclusiveBetween(ProductConstraints.MinId, ProductConstraints.MaxId);
}
