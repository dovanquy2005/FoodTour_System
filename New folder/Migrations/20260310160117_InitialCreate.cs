using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FoodTour_WebAdmin.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Shops",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ImageUrl = table.Column<string>(type: "TEXT", nullable: false),
                    Latitude = table.Column<double>(type: "REAL", nullable: false),
                    Longitude = table.Column<double>(type: "REAL", nullable: false),
                    Rating = table.Column<double>(type: "REAL", nullable: false),
                    IsVisited = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shops", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Dishes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ShopId = table.Column<string>(type: "TEXT", nullable: false),
                    Price = table.Column<double>(type: "REAL", nullable: false),
                    ImageUrl = table.Column<string>(type: "TEXT", nullable: false)
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
                name: "ShopTranslations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ShopId = table.Column<string>(type: "TEXT", nullable: false),
                    LanguageCode = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Address = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopTranslations_Shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "Shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DishTranslations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DishId = table.Column<string>(type: "TEXT", nullable: false),
                    LanguageCode = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
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

            migrationBuilder.InsertData(
                table: "Shops",
                columns: new[] { "Id", "CreatedAt", "ImageUrl", "IsVisited", "Latitude", "Longitude", "Rating", "UpdatedAt" },
                values: new object[,]
                {
                    { "s-001", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "shop_01.jpg", false, 10.75895, 106.70945, 4.7999999999999998, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "s-002", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "shop_02.jpg", false, 10.76042, 106.70589, 4.5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "s-003", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "shop_03.jpg", false, 10.75933, 106.70814, 4.2000000000000002, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "s-004", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "shop_04.jpg", false, 10.758699999999999, 106.709, 4.2999999999999998, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "s-005", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "shop_05.jpg", false, 10.7591, 106.7093, 4.5999999999999996, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "s-006", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "shop_06.jpg", false, 10.758850000000001, 106.70869999999999, 4.0999999999999996, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "s-007", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "shop_07.jpg", false, 10.759600000000001, 106.7075, 4.4000000000000004, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "s-008", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "shop_08.jpg", false, 10.7598, 106.70699999999999, 4.0, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Dishes",
                columns: new[] { "Id", "ImageUrl", "Price", "ShopId" },
                values: new object[,]
                {
                    { "d-001", "dish_01.jpg", 120000.0, "s-001" },
                    { "d-002", "dish_02.jpg", 50000.0, "s-001" },
                    { "d-003", "dish_03.jpg", 120000.0, "s-001" },
                    { "d-004", "dish_04.jpg", 160000.0, "s-001" },
                    { "d-005", "dish_05.jpg", 120000.0, "s-001" },
                    { "d-006", "dish_01.jpg", 110000.0, "s-002" },
                    { "d-007", "dish_02.jpg", 110000.0, "s-002" },
                    { "d-008", "dish_03.jpg", 110000.0, "s-002" },
                    { "d-009", "dish_04.jpg", 80000.0, "s-002" },
                    { "d-010", "dish_05.jpg", 110000.0, "s-002" },
                    { "d-011", "dish_01.jpg", 100000.0, "s-003" },
                    { "d-012", "dish_02.jpg", 60000.0, "s-003" },
                    { "d-013", "dish_03.jpg", 60000.0, "s-003" },
                    { "d-014", "dish_04.jpg", 110000.0, "s-003" },
                    { "d-015", "dish_05.jpg", 50000.0, "s-003" },
                    { "d-016", "dish_01.jpg", 250000.0, "s-004" },
                    { "d-017", "dish_02.jpg", 180000.0, "s-004" },
                    { "d-018", "dish_03.jpg", 150000.0, "s-004" },
                    { "d-019", "dish_04.jpg", 60000.0, "s-004" },
                    { "d-020", "dish_05.jpg", 80000.0, "s-004" },
                    { "d-021", "dish_01.jpg", 450000.0, "s-005" },
                    { "d-022", "dish_02.jpg", 320000.0, "s-005" },
                    { "d-023", "dish_03.jpg", 200000.0, "s-005" },
                    { "d-024", "dish_04.jpg", 130000.0, "s-005" },
                    { "d-025", "dish_05.jpg", 380000.0, "s-005" },
                    { "d-026", "dish_01.jpg", 70000.0, "s-006" },
                    { "d-027", "dish_02.jpg", 60000.0, "s-006" },
                    { "d-028", "dish_03.jpg", 90000.0, "s-006" },
                    { "d-029", "dish_04.jpg", 80000.0, "s-006" },
                    { "d-030", "dish_05.jpg", 70000.0, "s-006" },
                    { "d-031", "dish_01.jpg", 65000.0, "s-007" },
                    { "d-032", "dish_02.jpg", 120000.0, "s-007" },
                    { "d-033", "dish_03.jpg", 95000.0, "s-007" },
                    { "d-034", "dish_04.jpg", 75000.0, "s-007" },
                    { "d-035", "dish_05.jpg", 55000.0, "s-007" },
                    { "d-036", "dish_01.jpg", 30000.0, "s-008" },
                    { "d-037", "dish_02.jpg", 25000.0, "s-008" },
                    { "d-038", "dish_03.jpg", 35000.0, "s-008" },
                    { "d-039", "dish_04.jpg", 40000.0, "s-008" },
                    { "d-040", "dish_05.jpg", 45000.0, "s-008" }
                });

            migrationBuilder.InsertData(
                table: "ShopTranslations",
                columns: new[] { "Id", "Address", "Description", "LanguageCode", "Name", "ShopId" },
                values: new object[,]
                {
                    { 1, "534 Vĩnh Khánh, Phường 8, Quận 4", "Quán ốc nổi tiếng nhất nhì Sài Gòn, nằm trong Michelin Guide 2024. Không gian bình dân nhưng ốc luôn tươi sống, nước chấm đậm đà.", "vi", "Ốc Oanh 1", "s-001" },
                    { 2, "232/123 Vĩnh Khánh, Phường 6, Quận 4", "Chi nhánh của thương hiệu ốc Đào nổi tiếng, nước sốt đậm đà, không gian rộng rãi, phù hợp nhóm bạn.", "vi", "Ốc Đào II", "s-002" },
                    { 3, "395 Vĩnh Khánh, Phường 8, Quận 4", "Quán ốc bình dân với không khí nhộn nhịp đặc trưng của phố ẩm thực. Giá cả hợp lý, menu phong phú.", "vi", "Ốc Vũ", "s-003" },
                    { 4, "478 Vĩnh Khánh, Phường 8, Quận 4", "Chuyên lẩu dê và các món dê nướng. Thịt dê tươi, không hôi, nước lẩu thơm ngon đậm vị thuốc bắc.", "vi", "Lẩu Dê Vĩnh Khánh", "s-004" },
                    { 5, "502 Vĩnh Khánh, Phường 8, Quận 4", "Quán hải sản bình dân với bể hải sản tươi sống ngay trước quán. Tôm hùm, cua ghẹ luôn có sẵn.", "vi", "Hải Sản Bé Xu", "s-005" },
                    { 6, "420 Vĩnh Khánh, Phường 8, Quận 4", "Ốc len xào dừa là đặc sản. Quán nhỏ nhưng đông khách, phục vụ nhanh, giá sinh viên.", "vi", "Quán Ốc Thúy", "s-006" },
                    { 7, "350 Vĩnh Khánh, Phường 6, Quận 4", "Chuyên bò né, bít tết và các món ăn sáng kiểu Sài Gòn. Trứng ốp la, pate, bánh mì nóng giòn.", "vi", "Bò Né 3 Ngon", "s-007" },
                    { 8, "300 Vĩnh Khánh, Phường 6, Quận 4", "Xe bánh tráng trộn nổi tiếng đầu đường. Bánh tráng giòn, nước sốt chua cay đặc biệt, topping đầy đủ.", "vi", "Bánh Tráng Trộn Cô Ba", "s-008" }
                });

            migrationBuilder.InsertData(
                table: "DishTranslations",
                columns: new[] { "Id", "DishId", "LanguageCode", "Name" },
                values: new object[,]
                {
                    { 1, "d-001", "vi", "Càng ghẹ rang muối" },
                    { 2, "d-002", "vi", "Hào nướng phô mai" },
                    { 3, "d-003", "vi", "Nghêu hấp Thái" },
                    { 4, "d-004", "vi", "Bạch tuộc nướng muối ớt" },
                    { 5, "d-005", "vi", "Chem chép xào tỏi" },
                    { 6, "d-006", "vi", "Ốc hương rang muối ớt" },
                    { 7, "d-007", "vi", "Ốc hương xào bơ" },
                    { 8, "d-008", "vi", "Sò huyết xào tỏi" },
                    { 9, "d-009", "vi", "Nghêu hấp sả" },
                    { 10, "d-010", "vi", "Ốc mỡ xào me" },
                    { 11, "d-011", "vi", "Lưỡi vịt Sapo" },
                    { 12, "d-012", "vi", "Ốc tỏi nướng mắm" },
                    { 13, "d-013", "vi", "Răng mực rang muối" },
                    { 14, "d-014", "vi", "Bò lúc lắc" },
                    { 15, "d-015", "vi", "Khoai tây chiên" },
                    { 16, "d-016", "vi", "Lẩu dê nấm" },
                    { 17, "d-017", "vi", "Dê nướng tảng" },
                    { 18, "d-018", "vi", "Dê xào lăn" },
                    { 19, "d-019", "vi", "Cháo dê đậu xanh" },
                    { 20, "d-020", "vi", "Tiết canh dê" },
                    { 21, "d-021", "vi", "Tôm hùm nướng phô mai" },
                    { 22, "d-022", "vi", "Cua rang me" },
                    { 23, "d-023", "vi", "Ghẹ hấp bia" },
                    { 24, "d-024", "vi", "Mực chiên giòn" },
                    { 25, "d-025", "vi", "Cá mú hấp Hồng Kông" },
                    { 26, "d-026", "vi", "Ốc len xào dừa" },
                    { 27, "d-027", "vi", "Ốc bươu xào sả ớt" },
                    { 28, "d-028", "vi", "Sò điệp nướng mỡ hành" },
                    { 29, "d-029", "vi", "Ốc giác luộc" },
                    { 30, "d-030", "vi", "Nghêu nướng mỡ hành" },
                    { 31, "d-031", "vi", "Bò né truyền thống" },
                    { 32, "d-032", "vi", "Bò bít tết Úc" },
                    { 33, "d-033", "vi", "Bò lúc lắc khoai tây" },
                    { 34, "d-034", "vi", "Mì Ý bò bằm" },
                    { 35, "d-035", "vi", "Bánh mì chảo" },
                    { 36, "d-036", "vi", "Bánh tráng trộn đặc biệt" },
                    { 37, "d-037", "vi", "Bánh tráng nướng" },
                    { 38, "d-038", "vi", "Bánh tráng cuốn bò bía" },
                    { 39, "d-039", "vi", "Tré trộn rau răm" },
                    { 40, "d-040", "vi", "Gỏi khô bò" }
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

            migrationBuilder.CreateIndex(
                name: "IX_ShopTranslations_LanguageCode",
                table: "ShopTranslations",
                column: "LanguageCode");

            migrationBuilder.CreateIndex(
                name: "IX_ShopTranslations_ShopId",
                table: "ShopTranslations",
                column: "ShopId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DishTranslations");

            migrationBuilder.DropTable(
                name: "ShopTranslations");

            migrationBuilder.DropTable(
                name: "Dishes");

            migrationBuilder.DropTable(
                name: "Shops");
        }
    }
}
