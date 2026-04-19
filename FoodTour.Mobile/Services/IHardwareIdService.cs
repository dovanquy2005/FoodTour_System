namespace FoodTour.Mobile.Services;

/// <summary>
/// Interface để lấy Hardware ID thực tế của thiết bị.
/// Trên Android: sử dụng Settings.Secure.AndroidId.
/// Dùng cho Deep Link để định danh thiết bị không cần đăng nhập.
/// </summary>
public interface IHardwareIdService
{
    /// <summary>
    /// Trả về Hardware ID duy nhất của thiết bị.
    /// Trên Android: AndroidId (16 ký tự hex, không đổi sau factory reset từ Android 8+).
    /// </summary>
    string GetHardwareId();
}
