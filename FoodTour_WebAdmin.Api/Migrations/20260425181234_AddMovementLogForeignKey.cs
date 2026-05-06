using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodTour_WebAdmin.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMovementLogForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_UserDevices_DeviceId",
                table: "UserDevices",
                column: "DeviceId");

            migrationBuilder.AddForeignKey(
                name: "FK_MovementLogs_UserDevices_DeviceId",
                table: "MovementLogs",
                column: "DeviceId",
                principalTable: "UserDevices",
                principalColumn: "DeviceId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovementLogs_UserDevices_DeviceId",
                table: "MovementLogs");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_UserDevices_DeviceId",
                table: "UserDevices");
        }
    }
}
