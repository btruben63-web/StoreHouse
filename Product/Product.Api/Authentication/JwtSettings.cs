namespace Product.Api.Authentication;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Authority { get; init; } = string.Empty;
    public string? MetadataAddress { get; init; }
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public bool RequireHttpsMetadata { get; init; } = true;
}
