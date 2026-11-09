using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.IdentityServer.Data.Seed;

internal sealed class UserSeedConfiguration : IEntityTypeConfiguration<IdentityUser>
{
    public void Configure(EntityTypeBuilder<IdentityUser> builder) => builder.HasData(
        new IdentityUser
        {
            Id = IdentitySeedData.AdminUserId,
            UserName = "Admin",
            NormalizedUserName = "ADMIN",
            Email = "admin@storehouse.local",
            NormalizedEmail = "ADMIN@STOREHOUSE.LOCAL",
            EmailConfirmed = true,
            PhoneNumber = "+351910000001",
            PhoneNumberConfirmed = true,
            PasswordHash = IdentitySeedData.AdminPasswordHash,
            SecurityStamp = IdentitySeedData.AdminSecurityStamp,
            ConcurrencyStamp = IdentitySeedData.AdminConcurrencyStamp,
            LockoutEnabled = true,
            TwoFactorEnabled = false,
            AccessFailedCount = 0
        },
        new IdentityUser
        {
            Id = IdentitySeedData.ManagerUserId,
            UserName = "Manager",
            NormalizedUserName = "MANAGER",
            Email = "manager@storehouse.local",
            NormalizedEmail = "MANAGER@STOREHOUSE.LOCAL",
            EmailConfirmed = true,
            PhoneNumber = "+351910000002",
            PhoneNumberConfirmed = true,
            PasswordHash = IdentitySeedData.ManagerPasswordHash,
            SecurityStamp = IdentitySeedData.ManagerSecurityStamp,
            ConcurrencyStamp = IdentitySeedData.ManagerConcurrencyStamp,
            LockoutEnabled = true,
            TwoFactorEnabled = false,
            AccessFailedCount = 0
        });
}
