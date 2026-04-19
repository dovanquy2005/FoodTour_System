using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace FoodTour.Mobile;

// ── Android App Links: IntentFilter để lắng nghe Deep Link từ QR ──
// Khi user quét QR dẫn tới https://foodtour-admin-api.onrender.com/foodtour/{shopId},
// Android sẽ tự động mở Activity này thay vì trình duyệt.
[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
[IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = "https",
    DataHost = "foodtour-admin-api.onrender.com",
    DataPathPrefix = "/foodtour",
    AutoVerify = true)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Xử lý Deep Link khi app được mở lần đầu từ QR
        HandleDeepLinkIntent(Intent);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);

        // Xử lý Deep Link khi app đã chạy sẵn (SingleTop mode)
        HandleDeepLinkIntent(intent);
    }

    /// <summary>
    /// Truyền URI từ Android Intent vào hệ thống MAUI App Link.
    /// MAUI sẽ gọi App.OnAppLinkRequestReceived() để xử lý tiếp.
    /// </summary>
    private void HandleDeepLinkIntent(Intent? intent)
    {
        if (intent?.Action == Intent.ActionView && intent.Data != null)
        {
            var uri = new System.Uri(intent.Data.ToString()!);
            System.Diagnostics.Debug.WriteLine($"[DeepLink] Nhận URI từ Intent: {uri}");

            // Chuyển URI vào MAUI Platform để App.OnAppLinkRequestReceived xử lý
            Platform.CurrentActivity?.Window?.DecorView?.Post(() =>
            {
                (Microsoft.Maui.Controls.Application.Current as App)?.SendDeepLink(uri);
            });
        }
    }
}
