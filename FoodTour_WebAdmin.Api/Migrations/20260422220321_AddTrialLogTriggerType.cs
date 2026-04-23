using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodTour_WebAdmin.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTrialLogTriggerType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "UserDevices");

            migrationBuilder.AddColumn<int>(
                name: "TriggerType",
                table: "TrialLogs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TrialLogs_DeviceId_TriggerType",
                table: "TrialLogs",
                columns: new[] { "DeviceId", "TriggerType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrialLogs_DeviceId_TriggerType",
                table: "TrialLogs");

            migrationBuilder.DropColumn(
                name: "TriggerType",
                table: "TrialLogs");

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "UserDevices",
                type: "text",
                nullable: true);
        }
    }
}
