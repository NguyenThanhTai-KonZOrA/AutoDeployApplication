using ClientLauncher.Common.Constants;
using ClientLauncher.Implement.EntityModels;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClientLauncher.Implement.ApplicationDbContext.Initalizer
{
    public static class ApplicationSettingsSeed
    {
        public static void Seed(EntityTypeBuilder<ApplicationSettings> builder)
        {
            var seedAt = new DateTime(2025, 12, 01, 0, 0, 0, DateTimeKind.Utc);

            builder.HasData(
                new ApplicationSettings { Id = 1, Key = "EnableCheckAdministrator", Value = "False", Description = "On / Off Check Administrator Role", Category = "System", DataType = "Boolean", IsActive = true, IsDelete = false, CreatedBy = CommonConstants.SystemUser, UpdatedBy = CommonConstants.SystemUser, CreatedAt = seedAt, UpdatedAt = seedAt }
            );
        }
    }
}