using Microsoft.EntityFrameworkCore;
using FoodTour_WebAdmin.Api.Models;

namespace FoodTour_WebAdmin.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ShopModel> Shops => Set<ShopModel>();
    public DbSet<ShopTranslationModel> ShopTranslations => Set<ShopTranslationModel>();
    public DbSet<DishModel> Dishes => Set<DishModel>();
    public DbSet<DishTranslationModel> DishTranslations => Set<DishTranslationModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ShopModel
        modelBuilder.Entity<ShopModel>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        // ShopTranslationModel
        modelBuilder.Entity<ShopTranslationModel>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.LanguageCode);
            
            entity.HasOne(e => e.Shop)
                  .WithMany(s => s.ShopTranslations)
                  .HasForeignKey(e => e.ShopId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // DishModel
        modelBuilder.Entity<DishModel>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.Shop)
                  .WithMany(s => s.Dishes)
                  .HasForeignKey(e => e.ShopId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // DishTranslationModel
        modelBuilder.Entity<DishTranslationModel>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.LanguageCode);
            
            entity.HasOne(e => e.Dish)
                  .WithMany(d => d.DishTranslations)
                  .HasForeignKey(e => e.DishId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ═══════ SEED DATA — Vinh Khanh Food Street ═══════

        var shop1 = "s-001"; var shop2 = "s-002"; var shop3 = "s-003";
        var shop4 = "s-004"; var shop5 = "s-005"; var shop6 = "s-006";
        var shop7 = "s-007"; var shop8 = "s-008";

        var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<ShopModel>().HasData(
            new ShopModel { Id = shop1, Latitude = 10.75895, Longitude = 106.70945, Rating = 4.8, ImageUrl = "shop_01.jpg", CreatedAt = seedDate, UpdatedAt = seedDate },
            new ShopModel { Id = shop2, Latitude = 10.76042, Longitude = 106.70589, Rating = 4.5, ImageUrl = "shop_02.jpg", CreatedAt = seedDate, UpdatedAt = seedDate },
            new ShopModel { Id = shop3, Latitude = 10.75933, Longitude = 106.70814, Rating = 4.2, ImageUrl = "shop_03.jpg", CreatedAt = seedDate, UpdatedAt = seedDate },
            new ShopModel { Id = shop4, Latitude = 10.75870, Longitude = 106.70900, Rating = 4.3, ImageUrl = "shop_04.jpg", CreatedAt = seedDate, UpdatedAt = seedDate },
            new ShopModel { Id = shop5, Latitude = 10.75910, Longitude = 106.70930, Rating = 4.6, ImageUrl = "shop_05.jpg", CreatedAt = seedDate, UpdatedAt = seedDate },
            new ShopModel { Id = shop6, Latitude = 10.75885, Longitude = 106.70870, Rating = 4.1, ImageUrl = "shop_06.jpg", CreatedAt = seedDate, UpdatedAt = seedDate },
            new ShopModel { Id = shop7, Latitude = 10.75960, Longitude = 106.70750, Rating = 4.4, ImageUrl = "shop_07.jpg", CreatedAt = seedDate, UpdatedAt = seedDate },
            new ShopModel { Id = shop8, Latitude = 10.75980, Longitude = 106.70700, Rating = 4.0, ImageUrl = "shop_08.jpg", CreatedAt = seedDate, UpdatedAt = seedDate }
        );

        modelBuilder.Entity<ShopTranslationModel>().HasData(
            new ShopTranslationModel { Id = 1, ShopId = shop1, LanguageCode = "vi", Name = "Ốc Oanh 1", Address = "534 Vĩnh Khánh, Phường 8, Quận 4", Description = "Quán ốc nổi tiếng nhất nhì Sài Gòn, nằm trong Michelin Guide 2024. Không gian bình dân nhưng ốc luôn tươi sống, nước chấm đậm đà." },
            new ShopTranslationModel { Id = 2, ShopId = shop2, LanguageCode = "vi", Name = "Ốc Đào II", Address = "232/123 Vĩnh Khánh, Phường 6, Quận 4", Description = "Chi nhánh của thương hiệu ốc Đào nổi tiếng, nước sốt đậm đà, không gian rộng rãi, phù hợp nhóm bạn." },
            new ShopTranslationModel { Id = 3, ShopId = shop3, LanguageCode = "vi", Name = "Ốc Vũ", Address = "395 Vĩnh Khánh, Phường 8, Quận 4", Description = "Quán ốc bình dân với không khí nhộn nhịp đặc trưng của phố ẩm thực. Giá cả hợp lý, menu phong phú." },
            new ShopTranslationModel { Id = 4, ShopId = shop4, LanguageCode = "vi", Name = "Lẩu Dê Vĩnh Khánh", Address = "478 Vĩnh Khánh, Phường 8, Quận 4", Description = "Chuyên lẩu dê và các món dê nướng. Thịt dê tươi, không hôi, nước lẩu thơm ngon đậm vị thuốc bắc." },
            new ShopTranslationModel { Id = 5, ShopId = shop5, LanguageCode = "vi", Name = "Hải Sản Bé Xu", Address = "502 Vĩnh Khánh, Phường 8, Quận 4", Description = "Quán hải sản bình dân với bể hải sản tươi sống ngay trước quán. Tôm hùm, cua ghẹ luôn có sẵn." },
            new ShopTranslationModel { Id = 6, ShopId = shop6, LanguageCode = "vi", Name = "Quán Ốc Thúy", Address = "420 Vĩnh Khánh, Phường 8, Quận 4", Description = "Ốc len xào dừa là đặc sản. Quán nhỏ nhưng đông khách, phục vụ nhanh, giá sinh viên." },
            new ShopTranslationModel { Id = 7, ShopId = shop7, LanguageCode = "vi", Name = "Bò Né 3 Ngon", Address = "350 Vĩnh Khánh, Phường 6, Quận 4", Description = "Chuyên bò né, bít tết và các món ăn sáng kiểu Sài Gòn. Trứng ốp la, pate, bánh mì nóng giòn." },
            new ShopTranslationModel { Id = 8, ShopId = shop8, LanguageCode = "vi", Name = "Bánh Tráng Trộn Cô Ba", Address = "300 Vĩnh Khánh, Phường 6, Quận 4", Description = "Xe bánh tráng trộn nổi tiếng đầu đường. Bánh tráng giòn, nước sốt chua cay đặc biệt, topping đầy đủ." }
        );

        // ═══════ SEED DISHES ═══════
        modelBuilder.Entity<DishModel>().HasData(
            // --- Ốc Oanh 1 ---
            new DishModel { Id = "d-001", ShopId = shop1, Price = 120000, ImageUrl = "dish_01.jpg" },
            new DishModel { Id = "d-002", ShopId = shop1, Price = 50000, ImageUrl = "dish_02.jpg" },
            new DishModel { Id = "d-003", ShopId = shop1, Price = 120000, ImageUrl = "dish_03.jpg" },
            new DishModel { Id = "d-004", ShopId = shop1, Price = 160000, ImageUrl = "dish_04.jpg" },
            new DishModel { Id = "d-005", ShopId = shop1, Price = 120000, ImageUrl = "dish_05.jpg" },

            // --- Ốc Đào II ---
            new DishModel { Id = "d-006", ShopId = shop2, Price = 110000, ImageUrl = "dish_01.jpg" },
            new DishModel { Id = "d-007", ShopId = shop2, Price = 110000, ImageUrl = "dish_02.jpg" },
            new DishModel { Id = "d-008", ShopId = shop2, Price = 110000, ImageUrl = "dish_03.jpg" },
            new DishModel { Id = "d-009", ShopId = shop2, Price = 80000, ImageUrl = "dish_04.jpg" },
            new DishModel { Id = "d-010", ShopId = shop2, Price = 110000, ImageUrl = "dish_05.jpg" },

            // --- Ốc Vũ ---
            new DishModel { Id = "d-011", ShopId = shop3, Price = 100000, ImageUrl = "dish_01.jpg" },
            new DishModel { Id = "d-012", ShopId = shop3, Price = 60000, ImageUrl = "dish_02.jpg" },
            new DishModel { Id = "d-013", ShopId = shop3, Price = 60000, ImageUrl = "dish_03.jpg" },
            new DishModel { Id = "d-014", ShopId = shop3, Price = 110000, ImageUrl = "dish_04.jpg" },
            new DishModel { Id = "d-015", ShopId = shop3, Price = 50000, ImageUrl = "dish_05.jpg" },

            // --- Lẩu Dê Vĩnh Khánh ---
            new DishModel { Id = "d-016", ShopId = shop4, Price = 250000, ImageUrl = "dish_01.jpg" },
            new DishModel { Id = "d-017", ShopId = shop4, Price = 180000, ImageUrl = "dish_02.jpg" },
            new DishModel { Id = "d-018", ShopId = shop4, Price = 150000, ImageUrl = "dish_03.jpg" },
            new DishModel { Id = "d-019", ShopId = shop4, Price = 60000, ImageUrl = "dish_04.jpg" },
            new DishModel { Id = "d-020", ShopId = shop4, Price = 80000, ImageUrl = "dish_05.jpg" },

            // --- Hải Sản Bé Xu ---
            new DishModel { Id = "d-021", ShopId = shop5, Price = 450000, ImageUrl = "dish_01.jpg" },
            new DishModel { Id = "d-022", ShopId = shop5, Price = 320000, ImageUrl = "dish_02.jpg" },
            new DishModel { Id = "d-023", ShopId = shop5, Price = 200000, ImageUrl = "dish_03.jpg" },
            new DishModel { Id = "d-024", ShopId = shop5, Price = 130000, ImageUrl = "dish_04.jpg" },
            new DishModel { Id = "d-025", ShopId = shop5, Price = 380000, ImageUrl = "dish_05.jpg" },

            // --- Quán Ốc Thúy ---
            new DishModel { Id = "d-026", ShopId = shop6, Price = 70000, ImageUrl = "dish_01.jpg" },
            new DishModel { Id = "d-027", ShopId = shop6, Price = 60000, ImageUrl = "dish_02.jpg" },
            new DishModel { Id = "d-028", ShopId = shop6, Price = 90000, ImageUrl = "dish_03.jpg" },
            new DishModel { Id = "d-029", ShopId = shop6, Price = 80000, ImageUrl = "dish_04.jpg" },
            new DishModel { Id = "d-030", ShopId = shop6, Price = 70000, ImageUrl = "dish_05.jpg" },

            // --- Bò Né 3 Ngon ---
            new DishModel { Id = "d-031", ShopId = shop7, Price = 65000, ImageUrl = "dish_01.jpg" },
            new DishModel { Id = "d-032", ShopId = shop7, Price = 120000, ImageUrl = "dish_02.jpg" },
            new DishModel { Id = "d-033", ShopId = shop7, Price = 95000, ImageUrl = "dish_03.jpg" },
            new DishModel { Id = "d-034", ShopId = shop7, Price = 75000, ImageUrl = "dish_04.jpg" },
            new DishModel { Id = "d-035", ShopId = shop7, Price = 55000, ImageUrl = "dish_05.jpg" },

            // --- Bánh Tráng Trộn Cô Ba ---
            new DishModel { Id = "d-036", ShopId = shop8, Price = 30000, ImageUrl = "dish_01.jpg" },
            new DishModel { Id = "d-037", ShopId = shop8, Price = 25000, ImageUrl = "dish_02.jpg" },
            new DishModel { Id = "d-038", ShopId = shop8, Price = 35000, ImageUrl = "dish_03.jpg" },
            new DishModel { Id = "d-039", ShopId = shop8, Price = 40000, ImageUrl = "dish_04.jpg" },
            new DishModel { Id = "d-040", ShopId = shop8, Price = 45000, ImageUrl = "dish_05.jpg" }
        );

        modelBuilder.Entity<DishTranslationModel>().HasData(
            // --- Ốc Oanh 1 ---
            new DishTranslationModel { Id = 1, DishId = "d-001", LanguageCode = "vi", Name = "Càng ghẹ rang muối" },
            new DishTranslationModel { Id = 2, DishId = "d-002", LanguageCode = "vi", Name = "Hào nướng phô mai" },
            new DishTranslationModel { Id = 3, DishId = "d-003", LanguageCode = "vi", Name = "Nghêu hấp Thái" },
            new DishTranslationModel { Id = 4, DishId = "d-004", LanguageCode = "vi", Name = "Bạch tuộc nướng muối ớt" },
            new DishTranslationModel { Id = 5, DishId = "d-005", LanguageCode = "vi", Name = "Chem chép xào tỏi" },

            // --- Ốc Đào II ---
            new DishTranslationModel { Id = 6, DishId = "d-006", LanguageCode = "vi", Name = "Ốc hương rang muối ớt" },
            new DishTranslationModel { Id = 7, DishId = "d-007", LanguageCode = "vi", Name = "Ốc hương xào bơ" },
            new DishTranslationModel { Id = 8, DishId = "d-008", LanguageCode = "vi", Name = "Sò huyết xào tỏi" },
            new DishTranslationModel { Id = 9, DishId = "d-009", LanguageCode = "vi", Name = "Nghêu hấp sả" },
            new DishTranslationModel { Id = 10, DishId = "d-010", LanguageCode = "vi", Name = "Ốc mỡ xào me" },

            // --- Ốc Vũ ---
            new DishTranslationModel { Id = 11, DishId = "d-011", LanguageCode = "vi", Name = "Lưỡi vịt Sapo" },
            new DishTranslationModel { Id = 12, DishId = "d-012", LanguageCode = "vi", Name = "Ốc tỏi nướng mắm" },
            new DishTranslationModel { Id = 13, DishId = "d-013", LanguageCode = "vi", Name = "Răng mực rang muối" },
            new DishTranslationModel { Id = 14, DishId = "d-014", LanguageCode = "vi", Name = "Bò lúc lắc" },
            new DishTranslationModel { Id = 15, DishId = "d-015", LanguageCode = "vi", Name = "Khoai tây chiên" },

            // --- Lẩu Dê Vĩnh Khánh ---
            new DishTranslationModel { Id = 16, DishId = "d-016", LanguageCode = "vi", Name = "Lẩu dê nấm" },
            new DishTranslationModel { Id = 17, DishId = "d-017", LanguageCode = "vi", Name = "Dê nướng tảng" },
            new DishTranslationModel { Id = 18, DishId = "d-018", LanguageCode = "vi", Name = "Dê xào lăn" },
            new DishTranslationModel { Id = 19, DishId = "d-019", LanguageCode = "vi", Name = "Cháo dê đậu xanh" },
            new DishTranslationModel { Id = 20, DishId = "d-020", LanguageCode = "vi", Name = "Tiết canh dê" },

            // --- Hải Sản Bé Xu ---
            new DishTranslationModel { Id = 21, DishId = "d-021", LanguageCode = "vi", Name = "Tôm hùm nướng phô mai" },
            new DishTranslationModel { Id = 22, DishId = "d-022", LanguageCode = "vi", Name = "Cua rang me" },
            new DishTranslationModel { Id = 23, DishId = "d-023", LanguageCode = "vi", Name = "Ghẹ hấp bia" },
            new DishTranslationModel { Id = 24, DishId = "d-024", LanguageCode = "vi", Name = "Mực chiên giòn" },
            new DishTranslationModel { Id = 25, DishId = "d-025", LanguageCode = "vi", Name = "Cá mú hấp Hồng Kông" },

            // --- Quán Ốc Thúy ---
            new DishTranslationModel { Id = 26, DishId = "d-026", LanguageCode = "vi", Name = "Ốc len xào dừa" },
            new DishTranslationModel { Id = 27, DishId = "d-027", LanguageCode = "vi", Name = "Ốc bươu xào sả ớt" },
            new DishTranslationModel { Id = 28, DishId = "d-028", LanguageCode = "vi", Name = "Sò điệp nướng mỡ hành" },
            new DishTranslationModel { Id = 29, DishId = "d-029", LanguageCode = "vi", Name = "Ốc giác luộc" },
            new DishTranslationModel { Id = 30, DishId = "d-030", LanguageCode = "vi", Name = "Nghêu nướng mỡ hành" },

            // --- Bò Né 3 Ngon ---
            new DishTranslationModel { Id = 31, DishId = "d-031", LanguageCode = "vi", Name = "Bò né truyền thống" },
            new DishTranslationModel { Id = 32, DishId = "d-032", LanguageCode = "vi", Name = "Bò bít tết Úc" },
            new DishTranslationModel { Id = 33, DishId = "d-033", LanguageCode = "vi", Name = "Bò lúc lắc khoai tây" },
            new DishTranslationModel { Id = 34, DishId = "d-034", LanguageCode = "vi", Name = "Mì Ý bò bằm" },
            new DishTranslationModel { Id = 35, DishId = "d-035", LanguageCode = "vi", Name = "Bánh mì chảo" },

            // --- Bánh Tráng Trộn Cô Ba ---
            new DishTranslationModel { Id = 36, DishId = "d-036", LanguageCode = "vi", Name = "Bánh tráng trộn đặc biệt" },
            new DishTranslationModel { Id = 37, DishId = "d-037", LanguageCode = "vi", Name = "Bánh tráng nướng" },
            new DishTranslationModel { Id = 38, DishId = "d-038", LanguageCode = "vi", Name = "Bánh tráng cuốn bò bía" },
            new DishTranslationModel { Id = 39, DishId = "d-039", LanguageCode = "vi", Name = "Tré trộn rau răm" },
            new DishTranslationModel { Id = 40, DishId = "d-040", LanguageCode = "vi", Name = "Gỏi khô bò" }
        );
    }
}
