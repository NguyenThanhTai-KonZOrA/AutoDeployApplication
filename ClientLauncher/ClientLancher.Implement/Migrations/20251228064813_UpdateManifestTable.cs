using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClientLancher.Implement.Migrations
{
    /// <inheritdoc />
    public partial class UpdateManifestTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowSkip",
                table: "ApplicationManifests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "BinaryFilesJson",
                table: "ApplicationManifests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConfigFilesJson",
                table: "ApplicationManifests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyUser",
                table: "ApplicationManifests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UpdateDescription",
                table: "ApplicationManifests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowSkip",
                table: "ApplicationManifests");

            migrationBuilder.DropColumn(
                name: "BinaryFilesJson",
                table: "ApplicationManifests");

            migrationBuilder.DropColumn(
                name: "ConfigFilesJson",
                table: "ApplicationManifests");

            migrationBuilder.DropColumn(
                name: "NotifyUser",
                table: "ApplicationManifests");

            migrationBuilder.DropColumn(
                name: "UpdateDescription",
                table: "ApplicationManifests");
        }
    }
}
