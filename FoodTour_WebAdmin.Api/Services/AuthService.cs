using FoodTour_WebAdmin.Api.Data;
using FoodTour_WebAdmin.Api.DTOs;
using FoodTour_WebAdmin.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FoodTour_WebAdmin.Api.Services;

public class AuthService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly PasswordHasher<UserModel> _passwordHasher;

    public AuthService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
        _passwordHasher = new PasswordHasher<UserModel>();
    }

    public async Task<UserModel?> CheckLogin(string email, string password)
    {
        using var _db = await _dbFactory.CreateDbContextAsync();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return null;

        try
        {
            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (result == PasswordVerificationResult.Failed) return null;
            
            // Nếu thuật toán mã hóa thay đổi, hệ thống sẽ yêu cầu rehash
            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, password);
                await _db.SaveChangesAsync();
            }
        }
        catch (FormatException)
        {
            // Xử lý khi DB chứa chuỗi không phải Hash (VD: Admin nhập bằng tay 'Admin123' vào bảng Supabase)
            if (user.PasswordHash == password)
            {
                // Mật khẩu đúng dạng Text -> tự động nâng cấp (Hash) và lưu vào DB
                user.PasswordHash = _passwordHasher.HashPassword(user, password);
                await _db.SaveChangesAsync();
            }
            else
            {
                return null;
            }
        }

        return user;
    }

    /// <summary>
    /// Đăng ký Onboarding: Tạo tài khoản Owner + Shop + ShopTranslation trong 1 Transaction.
    /// Nếu bất kỳ bước nào lỗi → rollback toàn bộ, không có orphan data.
    /// </summary>
    public async Task<UserModel> RegisterAsync(RegisterRequest request)
    {
        using var db = await _dbFactory.CreateDbContextAsync();

        // Kiểm tra email trùng trước transaction để báo lỗi sớm
        if (await db.Users.AnyAsync(u => u.Email == request.Email))
            throw new Exception("Email này đã được sử dụng. Vui lòng chọn email khác.");

        // ── Transaction: đảm bảo User + Shop cùng được tạo hoặc cùng rollback ──
        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            // BƯỚC 1: Tạo tài khoản Owner
            var user = new UserModel
            {
                Id        = Guid.NewGuid().ToString(),
                Email     = request.Email,
                FullName  = request.FullName,
                Role      = "Owner",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
            db.Users.Add(user);
            await db.SaveChangesAsync(); // flush để lấy user.Id

            // BƯỚC 2: Tạo Shop gắn liền với Owner
            var shopId = Guid.NewGuid().ToString();
            var shop = new ShopModel
            {
                Id        = shopId,
                OwnerId   = user.Id,    // ← Gán ngay OwnerId
                ImageUrl  = string.Empty,
                Latitude  = 0,
                Longitude = 0,
                Radius    = 50,         // Bán kính geofence mặc định 50m
                Priority  = 0,
                Rating    = 0,
                IsActive  = false,           // K\u00edch ho\u1ea1t m\u1eb7c \u0111\u1ecbnh khi \u0111\u0103ng k\u00fd
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            db.Shops.Add(shop);

            // BƯỚC 3: Tạo ShopTranslation tiếng Việt với thông tin Owner vừa nhập
            var viTranslation = new ShopTranslationModel
            {
                ShopId           = shopId,
                LanguageCode     = "vi",
                Name             = request.ShopName.Trim(),
                Address          = request.ShopAddress.Trim(),
                Description      = string.Empty, // Owner bổ sung sau qua màn hình MyShop
                IsAudioGenerated = false,
            };
            db.ShopTranslations.Add(viTranslation);

            await db.SaveChangesAsync();

            // BƯỚC 4: Commit toàn bộ
            await transaction.CommitAsync();

            System.Diagnostics.Debug.WriteLine(
                $"[AuthService] Onboarding OK — User: {user.Email} | Shop: {shopId}");

            return user;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Overload giữ nguyên để Admin có thể tạo tài khoản không kèm shop (VD: tạo Admin account).
    /// </summary>
    public async Task<UserModel> RegisterAsync(string email, string password, string fullName, string role = "Owner")
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        if (await db.Users.AnyAsync(u => u.Email == email))
            throw new Exception("Email đã được sử dụng.");

        var user = new UserModel
        {
            Email    = email,
            FullName = fullName,
            Role     = role,
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, password);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }
}
