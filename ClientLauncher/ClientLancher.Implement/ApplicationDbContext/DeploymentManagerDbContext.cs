using ClientLauncher.Implement.ApplicationDbContext.Initalizer;
using ClientLauncher.Implement.ApplicationDbContext.SeedData;
using ClientLauncher.Implement.EntityModels;
using Microsoft.EntityFrameworkCore;
using QueueSystem.Implement.ApplicationDbContext.SeedData;

namespace ClientLauncher.Implement.ApplicationDbContext
{
    public class DeploymentManagerDbContext : DbContext
    {
        public DeploymentManagerDbContext(DbContextOptions<DeploymentManagerDbContext> options) : base(options)
        {
        }

        public DbSet<Application> Applications { get; set; }
        public DbSet<InstallationLog> InstallationLogs { get; set; }
        public DbSet<PackageVersion> PackageVersions { get; set; }
        public DbSet<DeploymentHistory> DeploymentHistories { get; set; }
        public DbSet<ApplicationCategory> ApplicationCategories { get; set; }
        public DbSet<DownloadStatistic> DownloadStatistics { get; set; }
        public DbSet<ApplicationManifest> ApplicationManifests { get; set; }
        public DbSet<Icons> Icons { get; set; }

        // Audit Logs table
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Employee> Employees { get; set; }

        // Role and Permission tables
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<EmployeeRole> EmployeeRoles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<ApplicationSettings> ApplicationSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Application Configuration
            modelBuilder.Entity<Application>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.AppCode).IsUnique();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.AppCode).IsRequired().HasMaxLength(50);

                // Category relationship
                entity.HasOne(e => e.Category)
                    .WithMany(c => c.Applications)
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // InstallationLog Configuration
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

            // ApplicationCategory Configuration
            modelBuilder.Entity<ApplicationCategory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(200);
            });

            // PackageVersion Configuration
            modelBuilder.Entity<PackageVersion>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Composite unique index: ApplicationId + Version
                entity.HasIndex(e => new { e.ApplicationId, e.Version }).IsUnique();
                entity.HasIndex(e => e.UploadedAt);
                entity.HasIndex(e => e.IsActive);

                entity.Property(e => e.Version).IsRequired().HasMaxLength(50);
                entity.Property(e => e.PackageFileName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FileHash).IsRequired().HasMaxLength(64);
                entity.Property(e => e.StoragePath).IsRequired().HasMaxLength(500);

                // Application relationship
                entity.HasOne(e => e.Application)
                    .WithMany(a => a.PackageVersions)
                    .HasForeignKey(e => e.ApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Self-referencing relationship for version replacement
                entity.HasOne(e => e.ReplacesVersion)
                    .WithMany()
                    .HasForeignKey(e => e.ReplacesVersionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // DeploymentHistory Configuration
            modelBuilder.Entity<DeploymentHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.PackageVersionId);
                entity.HasIndex(e => e.DeployedAt);
                entity.HasIndex(e => e.Status);

                entity.Property(e => e.Environment).IsRequired().HasMaxLength(50);
                entity.Property(e => e.DeploymentType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.DeployedBy).IsRequired().HasMaxLength(200);

                entity.HasOne(e => e.PackageVersion)
                    .WithMany(p => p.DeploymentHistories)
                    .HasForeignKey(e => e.PackageVersionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // DownloadStatistic Configuration
            modelBuilder.Entity<DownloadStatistic>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.PackageVersionId);
                entity.HasIndex(e => e.DownloadedAt);
                entity.HasIndex(e => new { e.MachineName, e.UserName });

                entity.Property(e => e.MachineName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.MachineId).IsRequired().HasMaxLength(200);
                entity.Property(e => e.UserName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.IpAddress).IsRequired().HasMaxLength(50);

                entity.HasOne(e => e.PackageVersion)
                    .WithMany(p => p.DownloadStatistics)
                    .HasForeignKey(e => e.PackageVersionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ApplicationManifest Configuration
            modelBuilder.Entity<ApplicationManifest>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Indexes
                entity.HasIndex(e => new { e.ApplicationId, e.Version }).IsUnique();
                entity.HasIndex(e => new { e.ApplicationId, e.IsActive, e.PublishedAt });

                // Properties
                entity.Property(e => e.Version).IsRequired().HasMaxLength(50);
                entity.Property(e => e.BinaryVersion).IsRequired().HasMaxLength(50);
                entity.Property(e => e.BinaryPackage).IsRequired().HasMaxLength(255);
                entity.Property(e => e.ConfigVersion).HasMaxLength(50);
                entity.Property(e => e.ConfigPackage).HasMaxLength(255);
                entity.Property(e => e.ConfigMergeStrategy).HasMaxLength(50).HasDefaultValue("preserveLocal");
                entity.Property(e => e.UpdateType).IsRequired().HasMaxLength(50).HasDefaultValue("both");
                entity.Property(e => e.CreatedBy).HasMaxLength(100);
                entity.Property(e => e.UpdatedBy).HasMaxLength(100);

                // Relationships
                entity.HasOne(e => e.Application)
                    .WithMany()
                    .HasForeignKey(e => e.ApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Icons Configuration
            modelBuilder.Entity<Icons>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.Type, e.ReferenceId });
                entity.HasIndex(e => e.Type);

                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
                entity.Property(e => e.FileUrl).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.FileExtension).IsRequired().HasMaxLength(10);
                entity.Property(e => e.Type).IsRequired();
            });

            // AuditLog configurations
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(a => a.ID);

                // Index for searching by user
                entity.HasIndex(a => a.UserName)
                    .HasDatabaseName("IX_AuditLogs_UserName");

                // Index for searching by action
                entity.HasIndex(a => a.Action)
                    .HasDatabaseName("IX_AuditLogs_Action");

                // Index for searching by entity type and ID
                entity.HasIndex(a => new { a.EntityType, a.EntityId })
                    .HasDatabaseName("IX_AuditLogs_Entity");

                // Index for searching by date range
                entity.HasIndex(a => a.CreatedAt)
                    .HasDatabaseName("IX_AuditLogs_CreatedAt");

                // Index for failed actions
                entity.HasIndex(a => new { a.IsSuccess, a.CreatedAt })
                    .HasDatabaseName("IX_AuditLogs_Success")
                    .HasFilter("[IsSuccess] = 0");

                // Soft delete filter
                entity.HasQueryFilter(x => !x.IsDelete);

                // Employee Configuration
                modelBuilder.Entity<Employee>().HasQueryFilter(x => !x.IsDelete);
                modelBuilder.Entity<Employee>()
                    .HasIndex(e => e.EmployeeCode)
                    .IsUnique();

                // Role configuration
                modelBuilder.Entity<Role>().HasQueryFilter(x => !x.IsDelete);
                modelBuilder.Entity<Role>()
                    .HasIndex(r => r.RoleName)
                    .IsUnique();

                // Permission configuration
                modelBuilder.Entity<Permission>().HasQueryFilter(x => !x.IsDelete);
                modelBuilder.Entity<Permission>()
                    .HasIndex(p => p.PermissionCode)
                    .IsUnique();

                // EmployeeRole configuration (Many-to-Many)
                modelBuilder.Entity<EmployeeRole>()
                    .HasKey(er => er.Id);

                modelBuilder.Entity<EmployeeRole>()
                    .HasIndex(er => new { er.EmployeeId, er.RoleId })
                    .IsUnique();

                modelBuilder.Entity<EmployeeRole>()
                    .HasOne(er => er.Employee)
                    .WithMany(e => e.EmployeeRoles)
                    .HasForeignKey(er => er.EmployeeId)
                    .OnDelete(DeleteBehavior.Cascade);

                modelBuilder.Entity<EmployeeRole>()
                    .HasOne(er => er.Role)
                    .WithMany(r => r.EmployeeRoles)
                    .HasForeignKey(er => er.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);

                modelBuilder.Entity<EmployeeRole>()
                    .HasQueryFilter(x => !x.IsDelete);

                // RolePermission configuration (Many-to-Many)
                modelBuilder.Entity<RolePermission>()
                    .HasKey(rp => rp.Id);

                modelBuilder.Entity<RolePermission>()
                    .HasIndex(rp => new { rp.RoleId, rp.PermissionId })
                    .IsUnique();

                modelBuilder.Entity<RolePermission>()
                    .HasOne(rp => rp.Role)
                    .WithMany(r => r.RolePermissions)
                    .HasForeignKey(rp => rp.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);

                modelBuilder.Entity<RolePermission>()
                    .HasOne(rp => rp.Permission)
                    .WithMany(p => p.RolePermissions)
                    .HasForeignKey(rp => rp.PermissionId)
                    .OnDelete(DeleteBehavior.Cascade);

                modelBuilder.Entity<RolePermission>()
                    .HasQueryFilter(x => !x.IsDelete);

                modelBuilder.Entity<ApplicationSettings>().HasQueryFilter(x => !x.IsDelete);

                modelBuilder.Entity<ApplicationSettings>()
                    .HasIndex(e => e.Key)
                    .IsUnique();


                RoleSeed.Seed(modelBuilder.Entity<Role>());
                //PermissionSeed.Seed(modelBuilder.Entity<Permission>());
                //RolePermissionSeed.Seed(modelBuilder.Entity<RolePermission>());
                EmployeeSeed.Seed(modelBuilder.Entity<Employee>());
                EmployeeRoleSeed.Seed(modelBuilder.Entity<EmployeeRole>());
                ApplicationSettingsSeed.Seed(modelBuilder.Entity<ApplicationSettings>());
            });
        }
    }
}