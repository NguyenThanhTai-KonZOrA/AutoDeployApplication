using ClientLauncher.Common.Constants;
using ClientLauncher.Implement.EntityModels;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClientLauncher.Implement.ApplicationDbContext.SeedData
{
    public static class PermissionSeed
    {
        public static void Seed(EntityTypeBuilder<Permission> builder)
        {
            var seedAt = new DateTime(2025, 12, 01, 0, 0, 0, DateTimeKind.Utc);

            builder.HasData(
                // Employee Management
                new Permission { Id = 1, PermissionName = "View Employee", PermissionCode = "EMPLOYEE_VIEW", Category = "Employee", Description = "View employee information", CreatedAt = seedAt, UpdatedAt = seedAt, CreatedBy = CommonConstants.SystemUser, UpdatedBy = CommonConstants.SystemUser, IsActive = true, IsDelete = false },
                new Permission { Id = 2, PermissionName = "Create Employee", PermissionCode = "EMPLOYEE_CREATE", Category = "Employee", Description = "Create new employees", CreatedAt = seedAt, UpdatedAt = seedAt, CreatedBy = CommonConstants.SystemUser, UpdatedBy = CommonConstants.SystemUser, IsActive = true, IsDelete = false },
                new Permission { Id = 3, PermissionName = "Update Employee", PermissionCode = "EMPLOYEE_UPDATE", Category = "Employee", Description = "Update employee information", CreatedAt = seedAt, UpdatedAt = seedAt, CreatedBy = CommonConstants.SystemUser, UpdatedBy = CommonConstants.SystemUser, IsActive = true, IsDelete = false },
                new Permission { Id = 4, PermissionName = "Delete Employee", PermissionCode = "EMPLOYEE_DELETE", Category = "Employee", Description = "Delete employees", CreatedAt = seedAt, UpdatedAt = seedAt, CreatedBy = CommonConstants.SystemUser, UpdatedBy = CommonConstants.SystemUser, IsActive = true, IsDelete = false }
            );
        }
    }
}