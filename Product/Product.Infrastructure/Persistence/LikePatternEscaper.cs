namespace Product.Infrastructure.Persistence;

public static class LikePatternEscaper
{
    public const string EscapeCharacter = @"\";

    public static string Escape(string value) =>
        value
            .Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal);
}
