using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FoodTour_WebAdmin.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDishEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DishTranslations");

            migrationBuilder.DropTable(
                name: "Dishes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Dishes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ShopId = table.Column<string>(type: "text", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: false),
                    Price = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dishes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dishes_Shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "Shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DishTranslations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DishId = table.Column<string>(type: "text", nullable: false),
                    LanguageCode = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DishTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DishTranslations_Dishes_DishId",
                        column: x => x.DishId,
                        principalTable: "Dishes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dishes_ShopId",
                table: "Dishes",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_DishTranslations_DishId",
                table: "DishTranslations",
                column: "DishId");

            migrationBuilder.CreateIndex(
                name: "IX_DishTranslations_LanguageCode",
                table: "DishTranslations",
                column: "LanguageCode");
        }
    }
}
