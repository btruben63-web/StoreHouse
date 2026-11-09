namespace Product.Application.Products.Abstractions;

public sealed record ProductFilter(string? NameContains = null, int? MinStock = null, int? MaxStock = null)
{
    public static ProductFilter None { get; } = new();
}
