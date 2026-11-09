using Microsoft.Extensions.Logging;

namespace Product.Application.Common.Logging;

internal static partial class LogMessages
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "Handling {RequestName}")]
    public static partial void HandlingRequest(this ILogger logger, string requestName);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Handled {RequestName} in {ElapsedMilliseconds} ms")]
    public static partial void HandledRequest(this ILogger logger, string requestName, double elapsedMilliseconds);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning, Message = "{RequestName} failed with {ErrorCode}: {ErrorMessage}")]
    public static partial void RequestFailed(this ILogger logger, string requestName, string errorCode, string errorMessage);

    [LoggerMessage(EventId = 2000, Level = LogLevel.Information, Message = "Product {ProductId} created with initial stock {Quantity}")]
    public static partial void ProductCreated(this ILogger logger, int productId, int quantity);

    [LoggerMessage(EventId = 2001, Level = LogLevel.Information, Message = "Product {ProductId} updated")]
    public static partial void ProductUpdated(this ILogger logger, int productId);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Information, Message = "Product {ProductId} soft deleted by {UserName}")]
    public static partial void ProductDeleted(this ILogger logger, int productId, string? userName);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Warning, Message = "Concurrency conflict while changing product {ProductId}")]
    public static partial void ProductConcurrencyConflict(this ILogger logger, int productId);

    [LoggerMessage(EventId = 2004, Level = LogLevel.Information, Message = "Stock of product {ProductId} increased by {Quantity} to {QuantityAfter}")]
    public static partial void StockIncreased(this ILogger logger, int productId, int quantity, int quantityAfter);

    [LoggerMessage(EventId = 2005, Level = LogLevel.Information, Message = "Stock of product {ProductId} decreased by {Quantity} to {QuantityAfter}")]
    public static partial void StockDecreased(this ILogger logger, int productId, int quantity, int quantityAfter);

    [LoggerMessage(EventId = 2006, Level = LogLevel.Warning, Message = "Insufficient stock for product {ProductId}: requested {Requested}, available {Available}")]
    public static partial void InsufficientStock(this ILogger logger, int productId, int requested, int available);

    [LoggerMessage(EventId = 2007, Level = LogLevel.Warning, Message = "Stock limit exceeded for product {ProductId} when adding {Quantity}")]
    public static partial void StockLimitExceeded(this ILogger logger, int productId, int quantity);
}
