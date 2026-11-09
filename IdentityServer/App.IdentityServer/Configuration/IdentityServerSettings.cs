namespace App.IdentityServer.Configuration;

public sealed class IdentityServerSettings
{
    public const string SectionName = "IdentityServer";

    public string IssuerUri { get; init; } = string.Empty;
    public string TokenEndpoint { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public string ApiScope { get; init; } = string.Empty;
    public int AccessTokenLifetimeSeconds { get; init; } = 3600;
    public string? KeyPath { get; init; }

    public void Validate()
    {
        (string Name, string Value)[] requiredValues =
        [
            (nameof(IssuerUri), IssuerUri),
            (nameof(TokenEndpoint), TokenEndpoint),
            (nameof(ClientId), ClientId),
            (nameof(ClientSecret), ClientSecret),
            (nameof(ApiScope), ApiScope)
        ];

        foreach (var (name, value) in requiredValues)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"Configuration value '{SectionName}:{name}' is required.");
            }
        }
    }
}
