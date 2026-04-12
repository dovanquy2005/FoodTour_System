using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodTour_WebAdmin.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnerForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Shops_OwnerId",
                table: "Shops",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Shops_Users_OwnerId",
                table: "Shops",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ShopSubmissions_Shops_ShopId",
                table: "ShopSubmissions",
                column: "ShopId",
                principalTable: "Shops",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ShopSubmissions_Users_OwnerId",
                table: "ShopSubmissions",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Shops_Users_OwnerId",
                table: "Shops");

            migrationBuilder.DropForeignKey(
                name: "FK_ShopSubmissions_Shops_ShopId",
                table: "ShopSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_ShopSubmissions_Users_OwnerId",
                table: "ShopSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_Shops_OwnerId",
                table: "Shops");
        }
    }
}
