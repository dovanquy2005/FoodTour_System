using Android.Provider;
using Application = Android.App.Application;

namespace FoodTour.Mobile.Services;

/// <summary>
/// Triển khai lấy Hardware ID trên Android bằng Settings.Secure.AndroidId.
/// AndroidId là mã hex 16 ký tự, duy nhất cho mỗi tổ hợp (thiết bị + user + app signing key).
/// Từ Android 8.0 trở lên, giá trị này ổn định và không đổi sau factory reset.
/// </summary>
public class HardwareIdService : IHardwareIdService
{
    public string GetHardwareId()
    {
        try
        {
            // Lấy AndroidId từ Settings.Secure — không cần quyền đặc biệt
            var context = Application.Context;
            var androidId = Settings.Secure.GetString(
                context.ContentResolver,
                Settings.Secure.AndroidId);

            return androidId ?? "unknown-hardware-id";
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HardwareId] Lỗi lấy AndroidId: {ex.Message}");
            return "unknown-hardware-id";
        }
    }
}
