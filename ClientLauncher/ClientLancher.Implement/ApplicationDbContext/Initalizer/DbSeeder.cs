using ClientLancher.Implement.EntityModels;
using Microsoft.EntityFrameworkCore;

namespace ClientLancher.Implement.ApplicationDbContext
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(ClientLancherDbContext context)
        {
            // Seed Categories
            if (!await context.ApplicationCategories.AnyAsync())
            {
                var categories = new List<ApplicationCategory>
                {
                    new ApplicationCategory
                    {
                        Name = "Cage",
                        DisplayName = "Cage Applications",
                        Description = "Applications for casino cage operations",
                        IconUrl = "/icons/cage.png",
                        DisplayOrder = 1,
                        IsActive = true
                    },
                    new ApplicationCategory
                    {
                        Name = "HTR",
                        DisplayName = "HTR Applications",
                        Description = "Hotel and Restaurant management applications",
                        IconUrl = "/icons/htr.png",
                        DisplayOrder = 2,
                        IsActive = true
                    },
                    new ApplicationCategory
                    {
                        Name = "Finance",
                        DisplayName = "Finance Applications",
                        Description = "Financial management and reporting applications",
                        IconUrl = "/icons/finance.png",
                        DisplayOrder = 3,
                        IsActive = true
                    }
                };

                await context.ApplicationCategories.AddRangeAsync(categories);
                await context.SaveChangesAsync();
            }

            // Seed Applications
            if (!await context.Applications.AnyAsync())
            {
                var cageCategory = await context.ApplicationCategories.FirstOrDefaultAsync(c => c.Name == "Cage");
                var htrCategory = await context.ApplicationCategories.FirstOrDefaultAsync(c => c.Name == "HTR");
                var financeCategory = await context.ApplicationCategories.FirstOrDefaultAsync(c => c.Name == "Finance");

                var applications = new List<Application>
                {
                    new Application
                    {
                        AppCode = "LevyTicketMonitor",
                        Name = "Levy Ticket Monitor",
                        Description = "Application for monitoring levy tickets",
                        IconUrl = "/icons/levy.png",
                        CategoryId = cageCategory?.Id,
                        IsActive = true
                    },
                    new Application
                    {
                        AppCode = "HTCasinoEntry",
                        Name = "HT Casino Entry",
                         Description = "Application for checking Patron In/Out",
                        IconUrl = "/icons/csentry.png",
                        CategoryId = htrCategory?.Id,
                        IsActive = true
                    },
                    new Application
                    {
                        AppCode = "QueueTicketDisplay",
                        Name = "Queue Ticket Display",
                        Description = "Application for display queue ticket",
                        IconUrl = "/icons/levy.png",
                        CategoryId = htrCategory?.Id,
                        IsActive = true
                    }
                };
                await context.Applications.AddRangeAsync(applications);

                // Seed Applications Manifest
                if (!await context.ApplicationManifests.AnyAsync())
                {
                    var LevyTicketMonitorApp = applications.First(a => a.AppCode == "LevyTicketMonitor");
                    var HTCasinoEntryApp = applications.First(a => a.AppCode == "HTCasinoEntry");
                    var QueueTicketDisplayApp = applications.First(a => a.AppCode == "QueueTicketDisplay");
                    var manifests = new List<ApplicationManifest>
                    {
                        new ApplicationManifest
                        {
                            Application = LevyTicketMonitorApp,
                            Version = "1.1.0",
                            BinaryVersion = "1.1.0",
                            BinaryPackage = "LevyTicketMonitor_v1.1.0.zip",
                            ConfigMergeStrategy = "Overwrite",
                            UpdateType = "Mandatory",
                            ForceUpdate = true,
                            IsStable = true,
                            PublishedAt = DateTime.UtcNow,
                            ApplicationId = LevyTicketMonitorApp.Id,
                            ConfigPackage = "LevyTicketMonitor_v1.1.0.zip",
                            ConfigVersion = "1.1.0",
                            ReleaseNotes = "Initial release of Levy Ticket Monitor.",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            CreatedBy = "System",
                            UpdatedBy = "System",
                            IsActive = true,
                            IsDelete = false
                        },
                        new ApplicationManifest
                        {
                            Application = HTCasinoEntryApp,
                            Version = "1.1.0",
                            BinaryVersion = "1.1.0",
                            BinaryPackage = "HTCasinoEntry_v1.1.0.zip",
                            ConfigMergeStrategy = "Overwrite",
                            UpdateType = "Mandatory",
                            ForceUpdate = true,
                            IsStable = true,
                            PublishedAt = DateTime.UtcNow,
                            ApplicationId = HTCasinoEntryApp.Id,
                            ConfigPackage = "HTCasinoEntry_v1.1.0.zip",
                            ConfigVersion = "1.1.0",
                            ReleaseNotes = "Initial release of HT Casino Entry.",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            CreatedBy = "System",
                            UpdatedBy = "System",
                            IsActive = true,
                            IsDelete = false

                        },
                        new ApplicationManifest
                        {
                            Application = QueueTicketDisplayApp,
                            Version = "1.1.0",
                            BinaryVersion = "1.1.0",
                            BinaryPackage = "QueueTicketDisplay_v1.1.0.zip",
                            ConfigMergeStrategy = "Overwrite",
                            UpdateType = "Mandatory",
                            ForceUpdate = true,
                            IsStable = true,
                            PublishedAt = DateTime.UtcNow,
                            ApplicationId = QueueTicketDisplayApp.Id,
                            ConfigPackage = "QueueTicketDisplay_v1.1.0.zip",
                            ConfigVersion = "1.1.0",
                            ReleaseNotes = "Initial release of Queue Ticket Display.",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            CreatedBy = "System",
                            UpdatedBy = "System",
                            IsActive = true,
                            IsDelete = false
                        }
                    };
                    await context.ApplicationManifests.AddRangeAsync(manifests);
                }

                await context.SaveChangesAsync();
            }
        }
    }
}