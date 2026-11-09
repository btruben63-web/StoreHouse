using Duende.IdentityModel;
using Duende.IdentityServer.Models;

namespace App.IdentityServer.Configuration;

public static class IdentityServerConfig
{
    public static IEnumerable<ApiScope> GetApiScopes(IdentityServerSettings settings) =>
        [new ApiScope(settings.ApiScope, "StoreHouse Product API")];

    public static IEnumerable<ApiResource> GetApiResources(IdentityServerSettings settings) =>
    [
        new ApiResource(settings.ApiScope, "StoreHouse Product API")
        {
            Scopes = { settings.ApiScope },
            UserClaims = { JwtClaimTypes.Role, JwtClaimTypes.Name }
        }
    ];

    public static IEnumerable<Client> GetClients(IdentityServerSettings settings) =>
    [
        new Client
        {
            ClientId = settings.ClientId,
            ClientName = "StoreHouse Swagger",
            AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
            ClientSecrets = { new Secret(settings.ClientSecret.Sha256()) },
            AllowedScopes = { settings.ApiScope },
            AccessTokenLifetime = settings.AccessTokenLifetimeSeconds,
            AllowOfflineAccess = false
        }
    ];
}
