using ClientLancher.Implement.EntityModels;
using Microsoft.EntityFrameworkCore;

namespace ClientLancher.Implement.ApplicationDbContext
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(ClientLancherDbContext context)
        {
            // Seed Applications
            if (!await context.Applications.AnyAsync())
            {
                var applications = new List<Application>
                {
                    new Application
                    {
                        AppCode = "APP001",
                        Name = "Levy Ticket Monitor",
                        Description = "Application for monitoring levy tickets",
                        IconUrl = "/icons/levy.png",
                        Category = "Cage",
                        IsActive = true
                    },
                    new Application
                    {
                        AppCode = "APP002",
                        Name = "CSEntry",
                        Description = "Application for checking Patron In/Out ",
                        IconUrl = "/icons/levy.png",
                        Category = "HTR",
                        IsActive = true
                    },
                    new Application
                    {
                        AppCode = "APP003",
                        Name = "Inventory Tracker",
                        Description = "Application for managing inventory and stock levels",
                        IconUrl = "/icons/levy.png",
                        Category = "Finance",
                        IsActive = true
                    }
                };

                await context.Applications.AddRangeAsync(applications);
                await context.SaveChangesAsync();
            }
        }
    }
}