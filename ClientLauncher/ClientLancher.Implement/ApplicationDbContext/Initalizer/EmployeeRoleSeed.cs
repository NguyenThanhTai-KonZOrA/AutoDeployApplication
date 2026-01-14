using ClientLauncher.Common.Constants;
using ClientLauncher.Implement.EntityModels;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClientLauncher.Implement.ApplicationDbContext.SeedData
{
    public static class EmployeeRoleSeed
    {
        public static void Seed(EntityTypeBuilder<EmployeeRole> builder)
        {
            var seedAt = new DateTime(2025, 12, 01, 0, 0, 0, DateTimeKind.Utc);

            builder.HasData(
                new EmployeeRole
                {
                    Id = 1,
                    RoleId = 1,
                    EmployeeId = 1,
                    CreatedAt = seedAt,
                    UpdatedAt = seedAt,
                    CreatedBy = CommonConstants.SystemUser,
                    UpdatedBy = CommonConstants.SystemUser,
                    IsActive = true,
                    IsDelete = false
                }
            );
        }
    }
}