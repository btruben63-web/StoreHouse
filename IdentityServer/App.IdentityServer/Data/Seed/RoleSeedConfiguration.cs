using App.IdentityServer.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.IdentityServer.Data.Seed;

internal sealed class RoleSeedConfiguration : IEntityTypeConfiguration<IdentityRole>
{
    public void Configure(EntityTypeBuilder<IdentityRole> builder) => builder.HasData(
        new IdentityRole
        {
            Id = IdentitySeedData.AdminRoleId,
            Name = AppRoles.Admin,
            NormalizedName = AppRoles.Admin.ToUpperInvariant(),
            ConcurrencyStamp = IdentitySeedData.AdminRoleConcurrencyStamp
        },
        new IdentityRole
        {
            Id = IdentitySeedData.ManagerRoleId,
            Name = AppRoles.Manager,
            NormalizedName = AppRoles.Manager.ToUpperInvariant(),
            ConcurrencyStamp = IdentitySeedData.ManagerRoleConcurrencyStamp
        });
}
