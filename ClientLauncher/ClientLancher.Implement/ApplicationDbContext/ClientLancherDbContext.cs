using ClientLancher.Implement.EntityModels;
using Microsoft.EntityFrameworkCore;

namespace ClientLancher.Implement.ApplicationDbContext
{
    public class ClientLancherDbContext : DbContext
    {
        public ClientLancherDbContext(DbContextOptions<ClientLancherDbContext> options)
            : base(options)
        {
        }

        public DbSet<Application> Applications { get; set; }
        public DbSet<InstallationLog> InstallationLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Application>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.AppCode).IsUnique();
                entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
                entity.Property(e => e.AppCode).HasMaxLength(50).IsRequired();
            });

            modelBuilder.Entity<InstallationLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.ApplicationId, e.StartedAt });
                entity.HasIndex(e => e.UserName);
                entity.HasIndex(e => e.MachineName);

                entity.HasOne(e => e.Application)
                    .WithMany(a => a.InstallationLogs)
                    .HasForeignKey(e => e.ApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}