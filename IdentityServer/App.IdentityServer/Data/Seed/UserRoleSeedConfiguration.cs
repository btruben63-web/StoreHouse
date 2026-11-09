using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.IdentityServer.Data.Seed;

internal sealed class UserRoleSeedConfiguration : IEntityTypeConfiguration<IdentityUserRole<string>>
{
    public void Configure(EntityTypeBuilder<IdentityUserRole<string>> builder) => builder.HasData(
        new IdentityUserRole<string> { UserId = IdentitySeedData.AdminUserId, RoleId = IdentitySeedData.AdminRoleId },
        new IdentityUserRole<string> { UserId = IdentitySeedData.ManagerUserId, RoleId = IdentitySeedData.ManagerRoleId });
}
