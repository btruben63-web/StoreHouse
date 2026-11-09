using Product.Application.Common.Results;

namespace Product.Application.Products;

public static class ProductErrors
{
    public static Error NotFound(int id) =>
        new("Product.NotFound", $"Product with id {id} was not found.", ErrorType.NotFound);

    public static Error InsufficientStock(int id, int available, int requested) =>
        new("Product.InsufficientStock",
            $"Product {id} has {available} units in stock and {requested} were requested.",
            ErrorType.BusinessRule);

    public static Error StockLimitExceeded(int id) =>
        new("Product.StockLimitExceeded",
            $"Adding this quantity would exceed the maximum stock allowed for product {id}.",
            ErrorType.BusinessRule);

    public static Error ConcurrencyConflict(int id) =>
        new("Product.ConcurrencyConflict",
            $"Product {id} was modified by another request. Reload it and try again.",
            ErrorType.Conflict);
}
