using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClientLancher.Implement.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IconUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDelete = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Applications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IconUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDelete = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Applications_ApplicationCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ApplicationCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationManifests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BinaryVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BinaryPackage = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    BinaryFilesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConfigVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ConfigPackage = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ConfigMergeStrategy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "preserveLocal"),
                    ConfigFilesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdateType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "both"),
                    ForceUpdate = table.Column<bool>(type: "bit", nullable: false),
                    NotifyUser = table.Column<bool>(type: "bit", nullable: false),
                    AllowSkip = table.Column<bool>(type: "bit", nullable: false),
                    UpdateDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReleaseNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsStable = table.Column<bool>(type: "bit", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDelete = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationManifests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationManifests_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InstallationLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MachineName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MachineId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StackTrace = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OldVersion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NewVersion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InstallationPath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationInSeconds = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDelete = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstallationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstallationLogs_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PackageVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PackageFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PackageType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    FileHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    StoragePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ReleaseNotes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsStable = table.Column<bool>(type: "bit", nullable: false),
                    MinimumClientVersion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UploadedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DownloadCount = table.Column<int>(type: "int", nullable: false),
                    LastDownloadedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReplacesVersionId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDelete = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackageVersions_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PackageVersions_PackageVersions_ReplacesVersionId",
                        column: x => x.ReplacesVersionId,
                        principalTable: "PackageVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeploymentHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackageVersionId = table.Column<int>(type: "int", nullable: false),
                    Environment = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DeploymentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TargetMachines = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TargetUsers = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsGlobalDeployment = table.Column<bool>(type: "bit", nullable: false),
                    TotalTargets = table.Column<int>(type: "int", nullable: false),
                    SuccessCount = table.Column<int>(type: "int", nullable: false),
                    FailedCount = table.Column<int>(type: "int", nullable: false),
                    PendingCount = table.Column<int>(type: "int", nullable: false),
                    DeployedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DeployedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDelete = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeploymentHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeploymentHistories_PackageVersions_PackageVersionId",
                        column: x => x.PackageVersionId,
                        principalTable: "PackageVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DownloadStatistics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackageVersionId = table.Column<int>(type: "int", nullable: false),
                    MachineName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MachineId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DownloadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BytesDownloaded = table.Column<long>(type: "bigint", nullable: false),
                    DurationSeconds = table.Column<int>(type: "int", nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClientLauncherVersion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OsVersion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDelete = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DownloadStatistics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DownloadStatistics_PackageVersions_PackageVersionId",
                        column: x => x.PackageVersionId,
                        principalTable: "PackageVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationCategories_Name",
                table: "ApplicationCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationManifests_ApplicationId_IsActive_PublishedAt",
                table: "ApplicationManifests",
                columns: new[] { "ApplicationId", "IsActive", "PublishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationManifests_ApplicationId_Version",
                table: "ApplicationManifests",
                columns: new[] { "ApplicationId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Applications_AppCode",
                table: "Applications",
                column: "AppCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Applications_CategoryId",
                table: "Applications",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentHistories_DeployedAt",
                table: "DeploymentHistories",
                column: "DeployedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentHistories_PackageVersionId",
                table: "DeploymentHistories",
                column: "PackageVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentHistories_Status",
                table: "DeploymentHistories",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DownloadStatistics_DownloadedAt",
                table: "DownloadStatistics",
                column: "DownloadedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DownloadStatistics_MachineName_UserName",
                table: "DownloadStatistics",
                columns: new[] { "MachineName", "UserName" });

            migrationBuilder.CreateIndex(
                name: "IX_DownloadStatistics_PackageVersionId",
                table: "DownloadStatistics",
                column: "PackageVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_InstallationLogs_ApplicationId_StartedAt",
                table: "InstallationLogs",
                columns: new[] { "ApplicationId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InstallationLogs_MachineName",
                table: "InstallationLogs",
                column: "MachineName");

            migrationBuilder.CreateIndex(
                name: "IX_InstallationLogs_UserName",
                table: "InstallationLogs",
                column: "UserName");

            migrationBuilder.CreateIndex(
                name: "IX_PackageVersions_ApplicationId_Version",
                table: "PackageVersions",
                columns: new[] { "ApplicationId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PackageVersions_IsActive",
                table: "PackageVersions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PackageVersions_ReplacesVersionId",
                table: "PackageVersions",
                column: "ReplacesVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageVersions_UploadedAt",
                table: "PackageVersions",
                column: "UploadedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationManifests");

            migrationBuilder.DropTable(
                name: "DeploymentHistories");

            migrationBuilder.DropTable(
                name: "DownloadStatistics");

            migrationBuilder.DropTable(
                name: "InstallationLogs");

            migrationBuilder.DropTable(
                name: "PackageVersions");

            migrationBuilder.DropTable(
                name: "Applications");

            migrationBuilder.DropTable(
                name: "ApplicationCategories");
        }
    }
}
