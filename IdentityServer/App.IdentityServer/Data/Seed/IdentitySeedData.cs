namespace App.IdentityServer.Data.Seed;

internal static class IdentitySeedData
{
    public const string AdminRoleId = "6b0e1c2a-3f4d-4a5b-9c6d-7e8f9a0b1c2d";
    public const string ManagerRoleId = "7c1f2d3b-4a5e-4b6c-8d7e-8f9a0b1c2d3e";
    public const string AdminRoleConcurrencyStamp = "0f6a8f43-2c55-4d1e-9a3b-6e2d4c8b1a70";
    public const string ManagerRoleConcurrencyStamp = "8d2b5c71-9e4f-4a3d-b6c2-1f7e9a0d3b54";

    public const string AdminUserId = "1a2b3c4d-5e6f-4a7b-8c9d-0e1f2a3b4c5d";
    public const string ManagerUserId = "2b3c4d5e-6f7a-4b8c-9d0e-1f2a3b4c5d6e";
    public const string AdminSecurityStamp = "W5J7QK3XN4LZ8RT6Y2PV9CHUDMBFGSAE";
    public const string ManagerSecurityStamp = "H3M8TZ6QW2KD9XRV5NLC7YBPJ4FSGAUE";
    public const string AdminConcurrencyStamp = "3e9c1b7a-5d2f-4e8b-a6c4-9f1d3b7e5a20";
    public const string ManagerConcurrencyStamp = "a7d4e2b9-1c6f-4b3a-8e5d-2f9c7a1b4e63";

    // Pre-computed with PasswordHasher<IdentityUser>; regenerate if the seeded password changes.
    public const string AdminPasswordHash =
        "AQAAAAIAAYagAAAAEACetcoOnESG4t7RY1t7m00Dbs1jxpMIgT/84iO3FxIFdu8Abje3KDC4/TdYC+spow==";
    public const string ManagerPasswordHash =
        "AQAAAAIAAYagAAAAEP5tIlHRsgC+r8NLx0YX5CFjy9+WMfunvEaLpYWZBlNr91b/2SlvomIvmx5pP+H0PQ==";
}
