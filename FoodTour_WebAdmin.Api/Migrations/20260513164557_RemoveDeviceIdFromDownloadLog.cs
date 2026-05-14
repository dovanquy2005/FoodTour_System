using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodTour_WebAdmin.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDeviceIdFromDownloadLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // migrationBuilder.DropIndex(
            //     name: "IX_DownloadLogs_DeviceId",
            //     table: "DownloadLogs");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "DownloadLogs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeviceId",
                table: "DownloadLogs",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DownloadLogs_DeviceId",
                table: "DownloadLogs",
                column: "DeviceId");
        }
    }
}
