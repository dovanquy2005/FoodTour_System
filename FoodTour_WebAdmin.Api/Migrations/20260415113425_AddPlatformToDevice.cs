using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodTour_WebAdmin.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformToDevice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsBlocked",
                table: "UserDevices");

            migrationBuilder.AddColumn<string>(
                name: "Platform",
                table: "UserDevices",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Platform",
                table: "UserDevices");

            migrationBuilder.AddColumn<bool>(
                name: "IsBlocked",
                table: "UserDevices",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
