using App.IdentityServer.Authorization;
using Duende.IdentityModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.IdentityServer.Data.Seed;

internal sealed class RoleClaimSeedConfiguration : IEntityTypeConfiguration<IdentityRoleClaim<string>>
{
    public void Configure(EntityTypeBuilder<IdentityRoleClaim<string>> builder) => builder.HasData(
        new IdentityRoleClaim<string>
        {
            Id = 1,
            RoleId = IdentitySeedData.AdminRoleId,
            ClaimType = JwtClaimTypes.Role,
            ClaimValue = AppRoles.Admin
        },
        new IdentityRoleClaim<string>
        {
            Id = 2,
            RoleId = IdentitySeedData.ManagerRoleId,
            ClaimType = JwtClaimTypes.Role,
            ClaimValue = AppRoles.Manager
        });
}
