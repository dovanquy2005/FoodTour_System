using FoodTour_WebAdmin.Api.Data;
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

    public async Task<UserModel> RegisterAsync(string email, string password, string fullName, string role = "Customer")
    {
        using var _db = await _dbFactory.CreateDbContextAsync();
        if (await _db.Users.AnyAsync(u => u.Email == email))
            throw new Exception("Email đã được sử dụng.");

        var user = new UserModel
        {
            Email = email,
            FullName = fullName,
            Role = role
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return user;
    }
}
