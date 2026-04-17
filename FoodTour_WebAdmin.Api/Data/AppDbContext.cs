using Microsoft.EntityFrameworkCore;
using FoodTour_WebAdmin.Api.Models;

namespace FoodTour_WebAdmin.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ShopModel> Shops => Set<ShopModel>();
    public DbSet<ShopTranslationModel> ShopTranslations => Set<ShopTranslationModel>();

    public DbSet<UserModel> Users => Set<UserModel>();
    public DbSet<ShopSubmission> ShopSubmissions => Set<ShopSubmission>();
    public DbSet<UserDeviceModel> UserDevices => Set<UserDeviceModel>();
    public DbSet<DownloadLog> DownloadLogs => Set<DownloadLog>();
    
    // Thêm bảng TrialLogs để kiểm soát 3 lần nghe thử qua IP
    public DbSet<TrialLog> TrialLogs => Set<TrialLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ShopModel
        modelBuilder.Entity<ShopModel>(entity =>
        {
            entity.HasKey(e => e.Id);

            // FK: Shop → Owner (SetNull — xóa User thì OwnerId về null, không mất dữ liệu quán)
            entity.HasOne(e => e.Owner)
                  .WithMany()
                  .HasForeignKey(e => e.OwnerId)
                  .OnDelete(DeleteBehavior.SetNull);
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

        // ShopSubmission
        modelBuilder.Entity<ShopSubmission>(entity =>
        {
            entity.HasKey(e => e.Id);
            // Lưu enum thành chuỗi (dễ đọc trong DB, an toàn khi thêm enum value mới)
            entity.Property(e => e.Status).HasConversion<string>();
            // Index tăng tốc truy vấn theo OwnerId và Status (Owner dùng thường xuyên)
            entity.HasIndex(e => e.OwnerId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ShopId);

            // FK: Submission → Owner (Restrict — không cho xóa User khi còn submission)
            entity.HasOne(e => e.Owner)
                  .WithMany()
                  .HasForeignKey(e => e.OwnerId)
                  .OnDelete(DeleteBehavior.Restrict);

            // FK: Submission → Shop (Restrict — không cho xóa Shop khi còn submission chưa xử lý)
            entity.HasOne(e => e.Shop)
                  .WithMany()
                  .HasForeignKey(e => e.ShopId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // UserDeviceModel
        modelBuilder.Entity<UserDeviceModel>(entity =>
        {
            entity.HasKey(e => e.Id);

            // DeviceId phải là duy nhất trên toàn bảng
            entity.HasIndex(e => e.DeviceId).IsUnique();
            entity.Property(e => e.DeviceId).HasMaxLength(36).IsRequired();
            entity.Property(e => e.DeviceName).HasMaxLength(200);

            // FK: Device → User (SetNull — xóa User không mất lịch sử thiết bị)
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // DownloadLog
        modelBuilder.Entity<DownloadLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DownloadedAt);
        });



        /*
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


        */
    }
}
