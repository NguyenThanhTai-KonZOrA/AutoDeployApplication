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
                        Name = "Sales Management System",
                        Description = "Application for managing sales and customers",
                        IconUrl = "/icons/sales.png",
                        Category = "Business",
                        IsActive = true
                    },
                    new Application
                    {
                        AppCode = "APP002",
                        Name = "HR Management System",
                        Description = "Human Resources management application",
                        IconUrl = "/icons/hr.png",
                        Category = "Business",
                        IsActive = true
                    },
                    new Application
                    {
                        AppCode = "APP003",
                        Name = "Inventory Manager",
                        Description = "Warehouse and inventory management",
                        IconUrl = "/icons/inventory.png",
                        Category = "Logistics",
                        IsActive = true
                    }
                };

                await context.Applications.AddRangeAsync(applications);
                await context.SaveChangesAsync();
            }
        }
    }
}