using ClientLauncher.Common.Constants;
using ClientLauncher.Implement.EntityModels;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace QueueSystem.Implement.ApplicationDbContext.SeedData
{
    public static class RolePermissionSeed
    {
        public static void Seed(EntityTypeBuilder<RolePermission> builder)
        {
            var seedAt = new DateTime(2025, 12, 01, 0, 0, 0, DateTimeKind.Utc);

            var rolePermissions = new List<RolePermission>();

            // Administrator - all permissions
            var adminPermissions = Enumerable.Range(1, 16).Select(i => new RolePermission
            {
                Id = i,
                RoleId = 1,
                PermissionId = i,
                CreatedAt = seedAt,
                UpdatedAt = seedAt,
                CreatedBy = CommonConstants.SystemUser,
                UpdatedBy = CommonConstants.SystemUser,
                IsActive = true,
                IsDelete = false
            });
            rolePermissions.AddRange(adminPermissions);
            builder.HasData(rolePermissions);
        }
    }
}