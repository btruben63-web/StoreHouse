namespace Product.Application.Common.Results;

public sealed record Error(string Code, string Message, ErrorType Type)
{
    public IReadOnlyDictionary<string, string[]> ValidationErrors { get; init; } = new Dictionary<string, string[]>();

    public static Error Validation(IReadOnlyDictionary<string, string[]> validationErrors) =>
        new("Validation.Failed", "One or more validation errors occurred.", ErrorType.Validation)
        {
            ValidationErrors = validationErrors
        };
}
