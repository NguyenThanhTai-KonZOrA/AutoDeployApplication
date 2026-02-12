using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClientLauncher.Implement.Migrations
{
    /// <inheritdoc />
    public partial class AddClientMachineAndDeploymentTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClientMachines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MachineId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MachineName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ComputerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DomainName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IPAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MACAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OSVersion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OSArchitecture = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CPUInfo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalMemoryMB = table.Column<long>(type: "bigint", nullable: true),
                    AvailableDiskSpaceGB = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastHeartbeat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RegisteredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InstalledApplications = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClientVersion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDelete = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientMachines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeploymentTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeploymentHistoryId = table.Column<int>(type: "int", nullable: false),
                    TargetMachineId = table.Column<int>(type: "int", nullable: false),
                    PackageVersionId = table.Column<int>(type: "int", nullable: false),
                    AppCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AppName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ScheduledFor = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProgressPercentage = table.Column<int>(type: "int", nullable: false),
                    CurrentStep = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorStackTrace = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    MaxRetries = table.Column<int>(type: "int", nullable: false),
                    NextRetryAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeploymentNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DownloadSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    InstallDuration = table.Column<TimeSpan>(type: "time", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDelete = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeploymentTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeploymentTasks_ClientMachines_TargetMachineId",
                        column: x => x.TargetMachineId,
                        principalTable: "ClientMachines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeploymentTasks_DeploymentHistories_DeploymentHistoryId",
                        column: x => x.DeploymentHistoryId,
                        principalTable: "DeploymentHistories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeploymentTasks_PackageVersions_PackageVersionId",
                        column: x => x.PackageVersionId,
                        principalTable: "PackageVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClientMachines_LastHeartbeat",
                table: "ClientMachines",
                column: "LastHeartbeat");

            migrationBuilder.CreateIndex(
                name: "IX_ClientMachines_MachineId",
                table: "ClientMachines",
                column: "MachineId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientMachines_MachineName",
                table: "ClientMachines",
                column: "MachineName");

            migrationBuilder.CreateIndex(
                name: "IX_ClientMachines_Status",
                table: "ClientMachines",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ClientMachines_UserName",
                table: "ClientMachines",
                column: "UserName");

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentTasks_DeploymentHistoryId",
                table: "DeploymentTasks",
                column: "DeploymentHistoryId");

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentTasks_PackageVersionId",
                table: "DeploymentTasks",
                column: "PackageVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentTasks_ScheduledFor",
                table: "DeploymentTasks",
                column: "ScheduledFor");

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentTasks_Status",
                table: "DeploymentTasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentTasks_TargetMachineId",
                table: "DeploymentTasks",
                column: "TargetMachineId");

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentTasks_TargetMachineId_Status",
                table: "DeploymentTasks",
                columns: new[] { "TargetMachineId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeploymentTasks");

            migrationBuilder.DropTable(
                name: "ClientMachines");
        }
    }
}
