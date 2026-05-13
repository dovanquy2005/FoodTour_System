using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodTour_WebAdmin.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemovePaymentAndUserDeviceRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserDevices_Users_UserId",
                table: "UserDevices");

            migrationBuilder.DropIndex(
                name: "IX_UserDevices_UserId",
                table: "UserDevices");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "UserDevices");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "UserDevices",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserDevices_UserId",
                table: "UserDevices",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserDevices_Users_UserId",
                table: "UserDevices",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
