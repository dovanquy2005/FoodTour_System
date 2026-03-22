using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodTour_WebAdmin.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAudioUrlToShopTranslation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AudioUrl",
                table: "ShopTranslations",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AudioUrl",
                table: "ShopTranslations");
        }
    }
}
