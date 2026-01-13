using ClientLauncher.Common.Constants;
using ClientLauncher.Implement.EntityModels;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClientLauncher.Implement.ApplicationDbContext.SeedData
{
    public static class EmployeeSeed
    {
        public static void Seed(EntityTypeBuilder<Employee> builder)
        {
            var seedAt = new DateTime(2025, 12, 01, 0, 0, 0, DateTimeKind.Utc);

            builder.HasData(
                new Employee
                {
                    Id = 1,
                    FullName = "System Administrator",
                    Email = "admin@thegrandhotram.com",
                    PhoneNumber = CommonConstants.SystemUser,
                    Department = "IT",
                    Position = "Administrator",
                    CreatedAt = seedAt,
                    UpdatedAt = seedAt,
                    CreatedBy = CommonConstants.SystemUser,
                    UpdatedBy = CommonConstants.SystemUser,
                    IsActive = true,
                    IsDelete = false,
                    WindowAccount = CommonConstants.AdminUserName,
                    EmployeeCode = CommonConstants.AdminUserName
                }
            );
        }
    }
}