namespace Product.Application.Common.Exceptions;

public sealed class ConcurrencyConflictException(string message, Exception innerException)
    : Exception(message, innerException);
