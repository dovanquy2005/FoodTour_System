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
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    ImageUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Latitude = table.Column<double>(type: "REAL", nullable: false),
                    Longitude = table.Column<double>(type: "REAL", nullable: false),
                    IsVisited = table.Column<bool>(type: "INTEGER", nullable: false),
                    Rating = table.Column<double>(type: "REAL", nullable: false),
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
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Price = table.Column<double>(type: "REAL", nullable: false),
                    ImageUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
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

            migrationBuilder.InsertData(
                table: "Shops",
                columns: new[] { "Id", "Address", "CreatedAt", "Description", "ImageUrl", "IsVisited", "Latitude", "Longitude", "Name", "Rating", "UpdatedAt" },
                values: new object[,]
                {
                    { "s-001", "534 Vĩnh Khánh, Phường 8, Quận 4", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Quán ốc nổi tiếng nhất nhì Sài Gòn, nằm trong Michelin Guide 2024. Không gian bình dân nhưng ốc luôn tươi sống, nước chấm đậm đà.", "shop_01.jpg", false, 10.75895, 106.70945, "Ốc Oanh 1", 4.7999999999999998, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "s-002", "232/123 Vĩnh Khánh, Phường 6, Quận 4", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Chi nhánh của thương hiệu ốc Đào nổi tiếng, nước sốt đậm đà, không gian rộng rãi, phù hợp nhóm bạn.", "shop_02.jpg", false, 10.76042, 106.70589, "Ốc Đào II", 4.5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "s-003", "395 Vĩnh Khánh, Phường 8, Quận 4", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Quán ốc bình dân với không khí nhộn nhịp đặc trưng của phố ẩm thực. Giá cả hợp lý, menu phong phú.", "shop_03.jpg", false, 10.75933, 106.70814, "Ốc Vũ", 4.2000000000000002, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "s-004", "478 Vĩnh Khánh, Phường 8, Quận 4", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Chuyên lẩu dê và các món dê nướng. Thịt dê tươi, không hôi, nước lẩu thơm ngon đậm vị thuốc bắc.", "shop_04.jpg", false, 10.758699999999999, 106.709, "Lẩu Dê Vĩnh Khánh", 4.2999999999999998, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "s-005", "502 Vĩnh Khánh, Phường 8, Quận 4", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Quán hải sản bình dân với bể hải sản tươi sống ngay trước quán. Tôm hùm, cua ghẹ luôn có sẵn.", "shop_05.jpg", false, 10.7591, 106.7093, "Hải Sản Bé Xu", 4.5999999999999996, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "s-006", "420 Vĩnh Khánh, Phường 8, Quận 4", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Ốc len xào dừa là đặc sản. Quán nhỏ nhưng đông khách, phục vụ nhanh, giá sinh viên.", "shop_06.jpg", false, 10.758850000000001, 106.70869999999999, "Quán Ốc Thúy", 4.0999999999999996, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "s-007", "350 Vĩnh Khánh, Phường 6, Quận 4", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Chuyên bò né, bít tết và các món ăn sáng kiểu Sài Gòn. Trứng ốp la, pate, bánh mì nóng giòn.", "shop_07.jpg", false, 10.759600000000001, 106.7075, "Bò Né 3 Ngon", 4.4000000000000004, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "s-008", "300 Vĩnh Khánh, Phường 6, Quận 4", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Xe bánh tráng trộn nổi tiếng đầu đường. Bánh tráng giòn, nước sốt chua cay đặc biệt, topping đầy đủ.", "shop_08.jpg", false, 10.7598, 106.70699999999999, "Bánh Tráng Trộn Cô Ba", 4.0, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Dishes",
                columns: new[] { "Id", "ImageUrl", "Name", "Price", "ShopId" },
                values: new object[,]
                {
                    { "d-001", "dish_01.jpg", "Càng ghẹ rang muối", 120000.0, "s-001" },
                    { "d-002", "dish_02.jpg", "Hào nướng phô mai", 50000.0, "s-001" },
                    { "d-003", "dish_03.jpg", "Nghêu hấp Thái", 120000.0, "s-001" },
                    { "d-004", "dish_04.jpg", "Bạch tuộc nướng muối ớt", 160000.0, "s-001" },
                    { "d-005", "dish_05.jpg", "Chem chép xào tỏi", 120000.0, "s-001" },
                    { "d-006", "dish_01.jpg", "Ốc hương rang muối ớt", 110000.0, "s-002" },
                    { "d-007", "dish_02.jpg", "Ốc hương xào bơ", 110000.0, "s-002" },
                    { "d-008", "dish_03.jpg", "Sò huyết xào tỏi", 110000.0, "s-002" },
                    { "d-009", "dish_04.jpg", "Nghêu hấp sả", 80000.0, "s-002" },
                    { "d-010", "dish_05.jpg", "Ốc mỡ xào me", 110000.0, "s-002" },
                    { "d-011", "dish_01.jpg", "Lưỡi vịt Sapo", 100000.0, "s-003" },
                    { "d-012", "dish_02.jpg", "Ốc tỏi nướng mắm", 60000.0, "s-003" },
                    { "d-013", "dish_03.jpg", "Răng mực rang muối", 60000.0, "s-003" },
                    { "d-014", "dish_04.jpg", "Bò lúc lắc", 110000.0, "s-003" },
                    { "d-015", "dish_05.jpg", "Khoai tây chiên", 50000.0, "s-003" },
                    { "d-016", "dish_01.jpg", "Lẩu dê nấm", 250000.0, "s-004" },
                    { "d-017", "dish_02.jpg", "Dê nướng tảng", 180000.0, "s-004" },
                    { "d-018", "dish_03.jpg", "Dê xào lăn", 150000.0, "s-004" },
                    { "d-019", "dish_04.jpg", "Cháo dê đậu xanh", 60000.0, "s-004" },
                    { "d-020", "dish_05.jpg", "Tiết canh dê", 80000.0, "s-004" },
                    { "d-021", "dish_01.jpg", "Tôm hùm nướng phô mai", 450000.0, "s-005" },
                    { "d-022", "dish_02.jpg", "Cua rang me", 320000.0, "s-005" },
                    { "d-023", "dish_03.jpg", "Ghẹ hấp bia", 200000.0, "s-005" },
                    { "d-024", "dish_04.jpg", "Mực chiên giòn", 130000.0, "s-005" },
                    { "d-025", "dish_05.jpg", "Cá mú hấp Hồng Kông", 380000.0, "s-005" },
                    { "d-026", "dish_01.jpg", "Ốc len xào dừa", 70000.0, "s-006" },
                    { "d-027", "dish_02.jpg", "Ốc bươu xào sả ớt", 60000.0, "s-006" },
                    { "d-028", "dish_03.jpg", "Sò điệp nướng mỡ hành", 90000.0, "s-006" },
                    { "d-029", "dish_04.jpg", "Ốc giác luộc", 80000.0, "s-006" },
                    { "d-030", "dish_05.jpg", "Nghêu nướng mỡ hành", 70000.0, "s-006" },
                    { "d-031", "dish_01.jpg", "Bò né truyền thống", 65000.0, "s-007" },
                    { "d-032", "dish_02.jpg", "Bò bít tết Úc", 120000.0, "s-007" },
                    { "d-033", "dish_03.jpg", "Bò lúc lắc khoai tây", 95000.0, "s-007" },
                    { "d-034", "dish_04.jpg", "Mì Ý bò bằm", 75000.0, "s-007" },
                    { "d-035", "dish_05.jpg", "Bánh mì chảo", 55000.0, "s-007" },
                    { "d-036", "dish_01.jpg", "Bánh tráng trộn đặc biệt", 30000.0, "s-008" },
                    { "d-037", "dish_02.jpg", "Bánh tráng nướng", 25000.0, "s-008" },
                    { "d-038", "dish_03.jpg", "Bánh tráng cuốn bò bía", 35000.0, "s-008" },
                    { "d-039", "dish_04.jpg", "Tré trộn rau răm", 40000.0, "s-008" },
                    { "d-040", "dish_05.jpg", "Gỏi khô bò", 45000.0, "s-008" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dishes_ShopId",
                table: "Dishes",
                column: "ShopId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Dishes");

            migrationBuilder.DropTable(
                name: "Shops");
        }
    }
}
