using Microsoft.EntityFrameworkCore;
using FoodTour_WebAdmin.Api.Models;

namespace FoodTour_WebAdmin.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Shop> Shops => Set<Shop>();
    public DbSet<Dish> Dishes => Set<Dish>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure relationships
        modelBuilder.Entity<Shop>()
            .HasMany(s => s.Dishes)
            .WithOne(d => d.Shop)
            .HasForeignKey(d => d.ShopId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Dish>()
            .HasIndex(d => d.ShopId);

        // ═══════ SEED DATA — Vinh Khanh Food Street ═══════

        var shop1 = "s-001"; var shop2 = "s-002"; var shop3 = "s-003";
        var shop4 = "s-004"; var shop5 = "s-005"; var shop6 = "s-006";
        var shop7 = "s-007"; var shop8 = "s-008";

        var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Shop>().HasData(
            new Shop
            {
                Id = shop1,
                Name = "Ốc Oanh 1",
                Address = "534 Vĩnh Khánh, Phường 8, Quận 4",
                Latitude = 10.75895, Longitude = 106.70945,
                Description = "Quán ốc nổi tiếng nhất nhì Sài Gòn, nằm trong Michelin Guide 2024. Không gian bình dân nhưng ốc luôn tươi sống, nước chấm đậm đà.",
                Rating = 4.8, ImageUrl = "shop_01.jpg",
                CreatedAt = seedDate, UpdatedAt = seedDate
            },
            new Shop
            {
                Id = shop2,
                Name = "Ốc Đào II",
                Address = "232/123 Vĩnh Khánh, Phường 6, Quận 4",
                Latitude = 10.76042, Longitude = 106.70589,
                Description = "Chi nhánh của thương hiệu ốc Đào nổi tiếng, nước sốt đậm đà, không gian rộng rãi, phù hợp nhóm bạn.",
                Rating = 4.5, ImageUrl = "shop_02.jpg",
                CreatedAt = seedDate, UpdatedAt = seedDate
            },
            new Shop
            {
                Id = shop3,
                Name = "Ốc Vũ",
                Address = "395 Vĩnh Khánh, Phường 8, Quận 4",
                Latitude = 10.75933, Longitude = 106.70814,
                Description = "Quán ốc bình dân với không khí nhộn nhịp đặc trưng của phố ẩm thực. Giá cả hợp lý, menu phong phú.",
                Rating = 4.2, ImageUrl = "shop_03.jpg",
                CreatedAt = seedDate, UpdatedAt = seedDate
            },
            new Shop
            {
                Id = shop4,
                Name = "Lẩu Dê Vĩnh Khánh",
                Address = "478 Vĩnh Khánh, Phường 8, Quận 4",
                Latitude = 10.75870, Longitude = 106.70900,
                Description = "Chuyên lẩu dê và các món dê nướng. Thịt dê tươi, không hôi, nước lẩu thơm ngon đậm vị thuốc bắc.",
                Rating = 4.3, ImageUrl = "shop_04.jpg",
                CreatedAt = seedDate, UpdatedAt = seedDate
            },
            new Shop
            {
                Id = shop5,
                Name = "Hải Sản Bé Xu",
                Address = "502 Vĩnh Khánh, Phường 8, Quận 4",
                Latitude = 10.75910, Longitude = 106.70930,
                Description = "Quán hải sản bình dân với bể hải sản tươi sống ngay trước quán. Tôm hùm, cua ghẹ luôn có sẵn.",
                Rating = 4.6, ImageUrl = "shop_05.jpg",
                CreatedAt = seedDate, UpdatedAt = seedDate
            },
            new Shop
            {
                Id = shop6,
                Name = "Quán Ốc Thúy",
                Address = "420 Vĩnh Khánh, Phường 8, Quận 4",
                Latitude = 10.75885, Longitude = 106.70870,
                Description = "Ốc len xào dừa là đặc sản. Quán nhỏ nhưng đông khách, phục vụ nhanh, giá sinh viên.",
                Rating = 4.1, ImageUrl = "shop_06.jpg",
                CreatedAt = seedDate, UpdatedAt = seedDate
            },
            new Shop
            {
                Id = shop7,
                Name = "Bò Né 3 Ngon",
                Address = "350 Vĩnh Khánh, Phường 6, Quận 4",
                Latitude = 10.75960, Longitude = 106.70750,
                Description = "Chuyên bò né, bít tết và các món ăn sáng kiểu Sài Gòn. Trứng ốp la, pate, bánh mì nóng giòn.",
                Rating = 4.4, ImageUrl = "shop_07.jpg",
                CreatedAt = seedDate, UpdatedAt = seedDate
            },
            new Shop
            {
                Id = shop8,
                Name = "Bánh Tráng Trộn Cô Ba",
                Address = "300 Vĩnh Khánh, Phường 6, Quận 4",
                Latitude = 10.75980, Longitude = 106.70700,
                Description = "Xe bánh tráng trộn nổi tiếng đầu đường. Bánh tráng giòn, nước sốt chua cay đặc biệt, topping đầy đủ.",
                Rating = 4.0, ImageUrl = "shop_08.jpg",
                CreatedAt = seedDate, UpdatedAt = seedDate
            }
        );

        // ═══════ SEED DISHES ═══════
        modelBuilder.Entity<Dish>().HasData(
            // --- Ốc Oanh 1 ---
            new Dish { Id = "d-001", ShopId = shop1, Name = "Càng ghẹ rang muối", Price = 120000, ImageUrl = "dish_01.jpg" },
            new Dish { Id = "d-002", ShopId = shop1, Name = "Hào nướng phô mai", Price = 50000, ImageUrl = "dish_02.jpg" },
            new Dish { Id = "d-003", ShopId = shop1, Name = "Nghêu hấp Thái", Price = 120000, ImageUrl = "dish_03.jpg" },
            new Dish { Id = "d-004", ShopId = shop1, Name = "Bạch tuộc nướng muối ớt", Price = 160000, ImageUrl = "dish_04.jpg" },
            new Dish { Id = "d-005", ShopId = shop1, Name = "Chem chép xào tỏi", Price = 120000, ImageUrl = "dish_05.jpg" },

            // --- Ốc Đào II ---
            new Dish { Id = "d-006", ShopId = shop2, Name = "Ốc hương rang muối ớt", Price = 110000, ImageUrl = "dish_01.jpg" },
            new Dish { Id = "d-007", ShopId = shop2, Name = "Ốc hương xào bơ", Price = 110000, ImageUrl = "dish_02.jpg" },
            new Dish { Id = "d-008", ShopId = shop2, Name = "Sò huyết xào tỏi", Price = 110000, ImageUrl = "dish_03.jpg" },
            new Dish { Id = "d-009", ShopId = shop2, Name = "Nghêu hấp sả", Price = 80000, ImageUrl = "dish_04.jpg" },
            new Dish { Id = "d-010", ShopId = shop2, Name = "Ốc mỡ xào me", Price = 110000, ImageUrl = "dish_05.jpg" },

            // --- Ốc Vũ ---
            new Dish { Id = "d-011", ShopId = shop3, Name = "Lưỡi vịt Sapo", Price = 100000, ImageUrl = "dish_01.jpg" },
            new Dish { Id = "d-012", ShopId = shop3, Name = "Ốc tỏi nướng mắm", Price = 60000, ImageUrl = "dish_02.jpg" },
            new Dish { Id = "d-013", ShopId = shop3, Name = "Răng mực rang muối", Price = 60000, ImageUrl = "dish_03.jpg" },
            new Dish { Id = "d-014", ShopId = shop3, Name = "Bò lúc lắc", Price = 110000, ImageUrl = "dish_04.jpg" },
            new Dish { Id = "d-015", ShopId = shop3, Name = "Khoai tây chiên", Price = 50000, ImageUrl = "dish_05.jpg" },

            // --- Lẩu Dê Vĩnh Khánh ---
            new Dish { Id = "d-016", ShopId = shop4, Name = "Lẩu dê nấm", Price = 250000, ImageUrl = "dish_01.jpg" },
            new Dish { Id = "d-017", ShopId = shop4, Name = "Dê nướng tảng", Price = 180000, ImageUrl = "dish_02.jpg" },
            new Dish { Id = "d-018", ShopId = shop4, Name = "Dê xào lăn", Price = 150000, ImageUrl = "dish_03.jpg" },
            new Dish { Id = "d-019", ShopId = shop4, Name = "Cháo dê đậu xanh", Price = 60000, ImageUrl = "dish_04.jpg" },
            new Dish { Id = "d-020", ShopId = shop4, Name = "Tiết canh dê", Price = 80000, ImageUrl = "dish_05.jpg" },

            // --- Hải Sản Bé Xu ---
            new Dish { Id = "d-021", ShopId = shop5, Name = "Tôm hùm nướng phô mai", Price = 450000, ImageUrl = "dish_01.jpg" },
            new Dish { Id = "d-022", ShopId = shop5, Name = "Cua rang me", Price = 320000, ImageUrl = "dish_02.jpg" },
            new Dish { Id = "d-023", ShopId = shop5, Name = "Ghẹ hấp bia", Price = 200000, ImageUrl = "dish_03.jpg" },
            new Dish { Id = "d-024", ShopId = shop5, Name = "Mực chiên giòn", Price = 130000, ImageUrl = "dish_04.jpg" },
            new Dish { Id = "d-025", ShopId = shop5, Name = "Cá mú hấp Hồng Kông", Price = 380000, ImageUrl = "dish_05.jpg" },

            // --- Quán Ốc Thúy ---
            new Dish { Id = "d-026", ShopId = shop6, Name = "Ốc len xào dừa", Price = 70000, ImageUrl = "dish_01.jpg" },
            new Dish { Id = "d-027", ShopId = shop6, Name = "Ốc bươu xào sả ớt", Price = 60000, ImageUrl = "dish_02.jpg" },
            new Dish { Id = "d-028", ShopId = shop6, Name = "Sò điệp nướng mỡ hành", Price = 90000, ImageUrl = "dish_03.jpg" },
            new Dish { Id = "d-029", ShopId = shop6, Name = "Ốc giác luộc", Price = 80000, ImageUrl = "dish_04.jpg" },
            new Dish { Id = "d-030", ShopId = shop6, Name = "Nghêu nướng mỡ hành", Price = 70000, ImageUrl = "dish_05.jpg" },

            // --- Bò Né 3 Ngon ---
            new Dish { Id = "d-031", ShopId = shop7, Name = "Bò né truyền thống", Price = 65000, ImageUrl = "dish_01.jpg" },
            new Dish { Id = "d-032", ShopId = shop7, Name = "Bò bít tết Úc", Price = 120000, ImageUrl = "dish_02.jpg" },
            new Dish { Id = "d-033", ShopId = shop7, Name = "Bò lúc lắc khoai tây", Price = 95000, ImageUrl = "dish_03.jpg" },
            new Dish { Id = "d-034", ShopId = shop7, Name = "Mì Ý bò bằm", Price = 75000, ImageUrl = "dish_04.jpg" },
            new Dish { Id = "d-035", ShopId = shop7, Name = "Bánh mì chảo", Price = 55000, ImageUrl = "dish_05.jpg" },

            // --- Bánh Tráng Trộn Cô Ba ---
            new Dish { Id = "d-036", ShopId = shop8, Name = "Bánh tráng trộn đặc biệt", Price = 30000, ImageUrl = "dish_01.jpg" },
            new Dish { Id = "d-037", ShopId = shop8, Name = "Bánh tráng nướng", Price = 25000, ImageUrl = "dish_02.jpg" },
            new Dish { Id = "d-038", ShopId = shop8, Name = "Bánh tráng cuốn bò bía", Price = 35000, ImageUrl = "dish_03.jpg" },
            new Dish { Id = "d-039", ShopId = shop8, Name = "Tré trộn rau răm", Price = 40000, ImageUrl = "dish_04.jpg" },
            new Dish { Id = "d-040", ShopId = shop8, Name = "Gỏi khô bò", Price = 45000, ImageUrl = "dish_05.jpg" }
        );
    }
}
