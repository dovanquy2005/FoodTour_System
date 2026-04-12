namespace FoodTour_WebAdmin.Api.DTOs;

public class RegisterRequest
{
    // ── Thông tin tài khoản ──
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;

    // ── Thông tin quán (bắt buộc, tạo cùng lúc với tài khoản) ──
    public string ShopName { get; set; } = string.Empty;
    public string ShopAddress { get; set; } = string.Empty;
}
