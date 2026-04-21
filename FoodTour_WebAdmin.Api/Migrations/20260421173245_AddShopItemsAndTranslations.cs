using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodTour_WebAdmin.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddShopItemsAndTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "UserDevices",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ShopId",
                table: "TrialLogs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "DeviceId",
                table: "TrialLogs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            // migrationBuilder.AddColumn<string>(
            //     name: "BrowserFingerprint",
            //     table: "TrialLogs",
            //     type: "text",
            //     nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DeviceId",
                table: "DownloadLogs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateTable(
                name: "ShopItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShopId = table.Column<string>(type: "text", nullable: false),
                    IsPremiumOnly = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopItems_Shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "Shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShopItemTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShopItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    LanguageCode = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    AudioUrl = table.Column<string>(type: "text", nullable: true),
                    IsAudioGenerated = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopItemTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopItemTranslations_ShopItems_ShopItemId",
                        column: x => x.ShopItemId,
                        principalTable: "ShopItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShopItems_ShopId",
                table: "ShopItems",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopItemTranslations_ShopItemId_LanguageCode",
                table: "ShopItemTranslations",
                columns: new[] { "ShopItemId", "LanguageCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShopItemTranslations");

            migrationBuilder.DropTable(
                name: "ShopItems");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "UserDevices");

            // migrationBuilder.DropColumn(
            //     name: "BrowserFingerprint",
            //     table: "TrialLogs");

            migrationBuilder.AlterColumn<string>(
                name: "ShopId",
                table: "TrialLogs",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DeviceId",
                table: "TrialLogs",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DeviceId",
                table: "DownloadLogs",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
