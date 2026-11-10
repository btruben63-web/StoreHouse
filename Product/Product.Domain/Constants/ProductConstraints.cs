namespace Product.Domain.Constants;

public static class ProductConstraints
{
    public const int MinId = 100_000;
    public const int MaxId = 999_999;
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 500;
    public const int UserNameMaxLength = 100;
    public const int PricePrecision = 10;
    public const int PriceScale = 2;
    public const decimal MinPrice = 0.01m;
    public const decimal MaxPrice = 99_999_999.99m;
    public const int MaxQuantityPerOperation = 1_000_000;
    public const int DefaultPageSize = 5;
    public const int MaxPageSize = 50;
}
