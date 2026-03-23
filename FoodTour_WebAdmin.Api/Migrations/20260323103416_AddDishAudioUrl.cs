using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodTour_WebAdmin.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDishAudioUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AudioUrl",
                table: "DishTranslations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAudioGenerated",
                table: "DishTranslations",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AudioUrl",
                table: "DishTranslations");

            migrationBuilder.DropColumn(
                name: "IsAudioGenerated",
                table: "DishTranslations");
        }
    }
}
