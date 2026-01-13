using ClientLauncher.Common.Constants;
using ClientLauncher.Implement.EntityModels;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClientLauncher.Implement.ApplicationDbContext.SeedData
{
    public static class RoleSeed
    {
        public static void Seed(EntityTypeBuilder<Role> builder)
        {
            var seedAt = new DateTime(2025, 12, 01, 0, 0, 0, DateTimeKind.Utc);

            builder.HasData(
                new Role { Id = 1, RoleName = "Administrator", Description = "Full system access", CreatedAt = seedAt, UpdatedAt = seedAt, CreatedBy = CommonConstants.SystemUser, UpdatedBy = CommonConstants.SystemUser, IsActive = true, IsDelete = false },
                new Role { Id = 2, RoleName = "Manager", Description = "Management level access", CreatedAt = seedAt, UpdatedAt = seedAt, CreatedBy = CommonConstants.SystemUser, UpdatedBy = CommonConstants.SystemUser, IsActive = true, IsDelete = false },
                new Role { Id = 4, RoleName = "Viewer", Description = "Read-only access", CreatedAt = seedAt, UpdatedAt = seedAt, CreatedBy = CommonConstants.SystemUser, UpdatedBy = CommonConstants.SystemUser, IsActive = true, IsDelete = false }
            );
        }
    }
}