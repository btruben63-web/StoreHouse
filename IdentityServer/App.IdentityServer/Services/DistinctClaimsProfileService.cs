using Duende.IdentityServer.AspNetIdentity;
using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Identity;

namespace App.IdentityServer.Services;

public sealed class DistinctClaimsProfileService(
    UserManager<IdentityUser> userManager,
    IUserClaimsPrincipalFactory<IdentityUser> claimsFactory,
    ILogger<ProfileService<IdentityUser>> logger)
    : ProfileService<IdentityUser>(userManager, claimsFactory, logger)
{
    public override async Task GetProfileDataAsync(ProfileDataRequestContext context, CancellationToken ct)
    {
        await base.GetProfileDataAsync(context, ct);
        context.IssuedClaims = context.IssuedClaims
            .DistinctBy(claim => (claim.Type, claim.Value))
            .ToList();
    }
}
