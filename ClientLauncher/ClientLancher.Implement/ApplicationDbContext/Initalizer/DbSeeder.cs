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
                        Name = "IT Ho Tram",
                        DisplayName = "IT Ho Tram Applications",
                        Description = "Applications for IT Ho Tram operations",
                        IconUrl = "/icons/it.png",
                        DisplayOrder = 1,
                        IsActive = true
                    },
                    new ApplicationCategory
                    {
                        Name = "Cage",
                        DisplayName = "Cage Applications",
                        Description = "Applications for casino cage operations",
                        IconUrl = "/icons/cage.png",
                        DisplayOrder = 2,
                        IsActive = true
                    },
                    new ApplicationCategory
                    {
                        Name = "HTR",
                        DisplayName = "HTR Applications",
                        Description = "Hotel and Restaurant management applications",
                        IconUrl = "/icons/htr.png",
                        DisplayOrder = 3,
                        IsActive = true
                    },
                    new ApplicationCategory
                    {
                        Name = "Finance",
                        DisplayName = "Finance Applications",
                        Description = "Financial management and reporting applications",
                        IconUrl = "/icons/finance.png",
                        DisplayOrder = 4,
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
                var itHotramApp = await context.ApplicationCategories.FirstOrDefaultAsync(c => c.Name == "IT_HoTram");
                var applications = new List<Application>
                {
                    new Application
                    {
                        AppCode = "ClientApplication",
                        Name = "Client Application",
                        Description = "Application for auto install",
                        IconUrl = "/icons/ClientApplication.png",
                        CategoryId = itHotramApp?.Id,
                        IsActive = true
                    },
                    //new Application
                    //{
                    //    AppCode = "LevyTicketMonitor",
                    //    Name = "Levy Ticket Monitor",
                    //    Description = "Application for monitoring levy tickets",
                    //    IconUrl = "/icons/LevyTicketMonitor.png",
                    //    CategoryId = cageCategory?.Id,
                    //    IsActive = true
                    //},
                    //new Application
                    //{
                    //    AppCode = "HTCasinoEntry",
                    //    Name = "HT Casino Entry",
                    //     Description = "Application for checking Patron In/Out",
                    //    IconUrl = "/icons/csentry.png",
                    //    CategoryId = htrCategory?.Id,
                    //    IsActive = true
                    //},
                    //new Application
                    //{
                    //    AppCode = "QueueTicketDisplay",
                    //    Name = "Queue Ticket Display",
                    //    Description = "Application for display queue ticket",
                    //    IconUrl = "/icons/QueueTicketDisplay.png",
                    //    CategoryId = htrCategory?.Id,
                    //    IsActive = true
                    //}
                };
                await context.Applications.AddRangeAsync(applications);

                // Seed Applications Manifest
                if (!await context.ApplicationManifests.AnyAsync())
                {
                    var clientApplication = applications.First(a => a.AppCode == "ClientApplication");
                    //var LevyTicketMonitorApp = applications.First(a => a.AppCode == "LevyTicketMonitor");
                    //var HTCasinoEntryApp = applications.First(a => a.AppCode == "HTCasinoEntry");
                    //var QueueTicketDisplayApp = applications.First(a => a.AppCode == "QueueTicketDisplay");
                    var manifests = new List<ApplicationManifest>
                    {
                        new ApplicationManifest
                        {
                            Application = clientApplication,
                            Version = "1.1.0",
                            BinaryVersion = "1.1.0",
                            BinaryPackage = "ClientApplication_v1.1.0.zip",
                            ConfigMergeStrategy = "ReplaceAll",
                            UpdateType = "Binary",
                            ForceUpdate = true,
                            IsStable = true,
                            PublishedAt = DateTime.UtcNow,
                            ApplicationId = clientApplication.Id,
                            ConfigPackage = "ClientApplication_v1.1.1.zip",
                            ConfigVersion = "1.1.1",
                            ReleaseNotes = "Initial release of Client application.",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            CreatedBy = "System",
                            UpdatedBy = "System",
                            IsActive = true,
                            IsDelete = false
                        },
                        //new ApplicationManifest
                        //{
                        //    Application = LevyTicketMonitorApp,
                        //    Version = "1.1.0",
                        //    BinaryVersion = "1.1.0",
                        //    BinaryPackage = "LevyTicketMonitor_v1.1.0.zip",
                        //    ConfigMergeStrategy = "ReplaceAll",
                        //    UpdateType = "Binary",
                        //    ForceUpdate = true,
                        //    IsStable = true,
                        //    PublishedAt = DateTime.UtcNow,
                        //    ApplicationId = LevyTicketMonitorApp.Id,
                        //    ConfigPackage = "LevyTicketMonitor_Config_v1.1.1.zip",
                        //    ConfigVersion = "1.1.1",
                        //    ReleaseNotes = "Initial release of Levy Ticket Monitor.",
                        //    CreatedAt = DateTime.UtcNow,
                        //    UpdatedAt = DateTime.UtcNow,
                        //    CreatedBy = "System",
                        //    UpdatedBy = "System",
                        //    IsActive = true,
                        //    IsDelete = false
                        //},
                        //new ApplicationManifest
                        //{
                        //    Application = HTCasinoEntryApp,
                        //    Version = "1.1.0",
                        //    BinaryVersion = "1.1.0",
                        //    BinaryPackage = "HTCasinoEntry_v1.1.0.zip",
                        //    ConfigMergeStrategy = "ReplaceAll",
                        //    UpdateType = "Binary",
                        //    ForceUpdate = true,
                        //    IsStable = true,
                        //    PublishedAt = DateTime.UtcNow,
                        //    ApplicationId = HTCasinoEntryApp.Id,
                        //    ConfigPackage = "HTCasinoEntry_Config_v1.1.1.zip",
                        //    ConfigVersion = "1.1.1",
                        //    ReleaseNotes = "Initial release of HT Casino Entry.",
                        //    CreatedAt = DateTime.UtcNow,
                        //    UpdatedAt = DateTime.UtcNow,
                        //    CreatedBy = "System",
                        //    UpdatedBy = "System",
                        //    IsActive = true,
                        //    IsDelete = false

                        //},
                        //new ApplicationManifest
                        //{
                        //    Application = QueueTicketDisplayApp,
                        //    Version = "1.1.0",
                        //    BinaryVersion = "1.1.0",
                        //    BinaryPackage = "QueueTicketDisplay_v1.1.0.zip",
                        //    ConfigMergeStrategy = "ReplaceAll",
                        //    UpdateType = "Binary",
                        //    ForceUpdate = true,
                        //    IsStable = true,
                        //    PublishedAt = DateTime.UtcNow,
                        //    ApplicationId = QueueTicketDisplayApp.Id,
                        //    ConfigPackage = "QueueTicketDisplay_Config_v1.1.1.zip",
                        //    ConfigVersion = "1.1.1",
                        //    ReleaseNotes = "Initial release of Queue Ticket Display.",
                        //    CreatedAt = DateTime.UtcNow,
                        //    UpdatedAt = DateTime.UtcNow,
                        //    CreatedBy = "System",
                        //    UpdatedBy = "System",
                        //    IsActive = true,
                        //    IsDelete = false
                        //}
                    };
                    await context.ApplicationManifests.AddRangeAsync(manifests);

                    // Seed Applications Manifest
                    if (!await context.PackageVersions.AnyAsync())
                    {
                        var packageVersions = new List<PackageVersion>
                        {
                            new PackageVersion
                            {
                                ApplicationId = clientApplication.Id,
                                Version = "1.1.0",
                                PackageFileName = "ClientApplication_v1.1.0.zip",
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow,
                                CreatedBy = "System",
                                UpdatedBy = "System",
                                IsActive = true,
                                IsDelete = false,
                                Application = clientApplication,
                                DownloadCount = 0,
                                FileHash = "abc123def456ghi789jkl012mno345pq",
                                PackageType = "Binary",
                                ReleaseNotes = "Initial release of Levy Ticket Monitor.",
                                StoragePath = "ClientApplication/v1.1.0/ClientApplication_v1.1.0.zip",
                                DeploymentHistories = new List<DeploymentHistory>(),
                                DownloadStatistics = new List<DownloadStatistic>(),
                                IsStable = true,
                                LastDownloadedAt = null,
                                MinimumClientVersion = "1.0.0",
                                UploadedAt = DateTime.UtcNow,
                                UploadedBy = "System",
                                FileSizeBytes = 15000000
                            },
                            //new PackageVersion
                            //{
                            //    ApplicationId = LevyTicketMonitorApp.Id,
                            //    Version = "1.1.0",
                            //    PackageFileName = "LevyTicketMonitor_v1.1.0.zip",
                            //    CreatedAt = DateTime.UtcNow,
                            //    UpdatedAt = DateTime.UtcNow,
                            //    CreatedBy = "System",
                            //    UpdatedBy = "System",
                            //    IsActive = true,
                            //    IsDelete = false,
                            //    Application = LevyTicketMonitorApp,
                            //    DownloadCount = 0,
                            //    FileHash = "abc123def456ghi789jkl012mno345pq",
                            //    PackageType = "Binary",
                            //    ReleaseNotes = "Initial release of Levy Ticket Monitor.",
                            //    StoragePath = "LevyTicketMonitor/v1.1.0/LevyTicketMonitor_v1.1.0.zip",
                            //    DeploymentHistories = new List<DeploymentHistory>(),
                            //    DownloadStatistics = new List<DownloadStatistic>(),
                            //    IsStable = true,
                            //    LastDownloadedAt = null,
                            //    MinimumClientVersion = "1.0.0",
                            //    UploadedAt = DateTime.UtcNow,
                            //    UploadedBy = "System",
                            //    FileSizeBytes = 15000000
                            //},
                            //new PackageVersion
                            //{
                            //    ApplicationId = LevyTicketMonitorApp.Id,
                            //    Version = "1.1.1",
                            //    PackageFileName = "LevyTicketMonitor_Config_v1.1.1.zip",
                            //    CreatedAt = DateTime.UtcNow,
                            //    UpdatedAt = DateTime.UtcNow,
                            //    CreatedBy = "System",
                            //    UpdatedBy = "System",
                            //    IsActive = true,
                            //    IsDelete = false,
                            //    Application = LevyTicketMonitorApp,
                            //    DownloadCount = 0,
                            //    FileHash = "abc123def456ghi789jkl012mno345pq",
                            //    PackageType = "Config",
                            //    ReleaseNotes = "Initial release of Levy Ticket Monitor.",
                            //    StoragePath = "LevyTicketMonitor/v1.1.1/LevyTicketMonitor_Config_v1.1.1.zip",
                            //    DeploymentHistories = new List<DeploymentHistory>(),
                            //    DownloadStatistics = new List<DownloadStatistic>(),
                            //    IsStable = true,
                            //    LastDownloadedAt = null,
                            //    MinimumClientVersion = "1.0.0",
                            //    UploadedAt = DateTime.UtcNow,
                            //    UploadedBy = "System",
                            //    FileSizeBytes = 15000000
                            //},
                            //new PackageVersion
                            //{
                            //    ApplicationId = HTCasinoEntryApp.Id,
                            //    Version = "1.1.0",
                            //    PackageFileName = "HTCasinoEntry_v1.1.0.zip",
                            //    CreatedAt = DateTime.UtcNow,
                            //    UpdatedAt = DateTime.UtcNow,
                            //    CreatedBy = "System",
                            //    UpdatedBy = "System",
                            //    IsActive = true,
                            //    IsDelete = false,
                            //    Application = HTCasinoEntryApp,
                            //    DownloadCount = 0,
                            //    FileHash = "def456ghi789jkl012mno345pqabc123",
                            //    PackageType = "Binary",
                            //    ReleaseNotes = "Initial release of HT Casino Entry.",
                            //    StoragePath = "HTCasinoEntry/v1.1.0/HTCasinoEntry_v1.1.0.zip",
                            //    DeploymentHistories = new List<DeploymentHistory>(),
                            //    DownloadStatistics = new List<DownloadStatistic>(),
                            //    IsStable = true,
                            //    LastDownloadedAt = null,
                            //    MinimumClientVersion = "1.0.0",
                            //    UploadedAt = DateTime.UtcNow,
                            //    UploadedBy = "System",
                            //    FileSizeBytes = 20000000
                            //},
                            //new PackageVersion
                            //{
                            //    ApplicationId = HTCasinoEntryApp.Id,
                            //    Version = "1.1.1",
                            //    PackageFileName = "HTCasinoEntry_Config_v1.1.1.zip",
                            //    CreatedAt = DateTime.UtcNow,
                            //    UpdatedAt = DateTime.UtcNow,
                            //    CreatedBy = "System",
                            //    UpdatedBy = "System",
                            //    IsActive = true,
                            //    IsDelete = false,
                            //    Application = HTCasinoEntryApp,
                            //    DownloadCount = 0,
                            //    FileHash = "def456ghi789jkl012mno345pqabc123",
                            //    PackageType = "Config",
                            //    ReleaseNotes = "Initial release of HT Casino Entry.",
                            //    StoragePath = "HTCasinoEntry/v1.1.1/HTCasinoEntry_Config_v1.1.1.zip",
                            //    DeploymentHistories = new List<DeploymentHistory>(),
                            //    DownloadStatistics = new List<DownloadStatistic>(),
                            //    IsStable = true,
                            //    LastDownloadedAt = null,
                            //    MinimumClientVersion = "1.0.0",
                            //    UploadedAt = DateTime.UtcNow,
                            //    UploadedBy = "System",
                            //    FileSizeBytes = 20000000
                            //},
                            //new PackageVersion
                            //{
                            //    ApplicationId = QueueTicketDisplayApp.Id,
                            //    Version = "1.1.0",
                            //    PackageFileName = "QueueTicketDisplay_v1.1.0.zip",
                            //    CreatedAt = DateTime.UtcNow,
                            //    UpdatedAt = DateTime.UtcNow,
                            //    CreatedBy = "System",
                            //    UpdatedBy = "System",
                            //    IsActive = true,
                            //    IsDelete = false,
                            //    Application = QueueTicketDisplayApp,
                            //    DownloadCount = 0,
                            //    FileHash = "abc123def456ghi789jkl012mno345pq",
                            //    PackageType = "Binary",
                            //    ReleaseNotes = "Initial release of Levy Ticket Monitor.",
                            //    StoragePath = "QueueTicketDisplay/v1.1.0/QueueTicketDisplay_v1.1.0.zip",
                            //    DeploymentHistories = new List<DeploymentHistory>(),
                            //    DownloadStatistics = new List<DownloadStatistic>(),
                            //    LastDownloadedAt = null,
                            //    MinimumClientVersion = "1.0.0",
                            //    UploadedAt = DateTime.UtcNow,
                            //    UploadedBy = "System",
                            //    FileSizeBytes = 15000000
                            //},
                            //new PackageVersion
                            //{
                            //    ApplicationId = QueueTicketDisplayApp.Id,
                            //    Version = "1.1.1",
                            //    PackageFileName = "QueueTicketDisplay_Config_v1.1.1.zip",
                            //    CreatedAt = DateTime.UtcNow,
                            //    UpdatedAt = DateTime.UtcNow,
                            //    CreatedBy = "System",
                            //    UpdatedBy = "System",
                            //    IsActive = true,
                            //    IsDelete = false,
                            //    Application = QueueTicketDisplayApp,
                            //    DownloadCount = 0,
                            //    FileHash = "abc123def456ghi789jkl012mno345pq",
                            //    PackageType = "Config",
                            //    ReleaseNotes = "Initial release of Levy Ticket Monitor.",
                            //    StoragePath = "QueueTicketDisplay/v1.1.1/QueueTicketDisplay_Config_v1.1.1.zip",
                            //    DeploymentHistories = new List<DeploymentHistory>(),
                            //    DownloadStatistics = new List<DownloadStatistic>(),
                            //    LastDownloadedAt = null,
                            //    MinimumClientVersion = "1.0.0",
                            //    UploadedAt = DateTime.UtcNow,
                            //    UploadedBy = "System",
                            //    FileSizeBytes = 15000000
                            //}

                        };
                        await context.PackageVersions.AddRangeAsync(packageVersions);
                    }
                }
                await context.SaveChangesAsync();
            }
        }
    }
}