using Microsoft.AspNetCore.Mvc;

namespace FoodTour_WebAdmin.Api.Controllers;

/// <summary>
/// Phục vụ Deep Link cho Android App Links:
/// 1. Trả file xác thực assetlinks.json để Android verify domain.
/// 2. Route fallback /foodtour/{shopId} khi user mở URL trên trình duyệt (chưa cài app).
/// </summary>
[ApiController]
public class DeepLinkController : ControllerBase
{
    // ─────────────────────────────────────────────────────────────────────────
    // GET /.well-known/assetlinks.json
    // File xác thực Android App Links — Android dùng để verify domain ownership.
    // ─────────────────────────────────────────────────────────────────────────
    [HttpGet("/.well-known/assetlinks.json")]
    public IActionResult GetAssetLinks()
    {
        // Trả về JSON xác thực cho Android App Links
        // PLACEHOLDER: Thay sha256_cert_fingerprints bằng fingerprint thật từ keystore
        var assetLinks = new[]
        {
            new
            {
                relation = new[] { "delegate_permission/common.handle_all_urls" },
                target = new
                {
                    @namespace = "android_app",
                    package_name = "com.companyname.foodtour.mobile",
                    sha256_cert_fingerprints = new[]
                    {
                        // TODO: Thay bằng SHA256 fingerprint thật từ foodtour.keystore
                        // Chạy: keytool -list -v -keystore foodtour.keystore -alias <alias>
                        "14:6D:E9:83:C5:73:06:50:D8:EE:B9:95:2F:34:FC:64:16:A0:83:42:E6:1D:BE:A8:8A:04:96:B2:3F:CF:44:E5"
                    }
                }
            }
        };

        return new JsonResult(assetLinks);
    }
/*
    // ─────────────────────────────────────────────────────────────────────────
    // GET /foodtour/{shopId}
    // Trang fallback khi user mở Deep Link trên trình duyệt (chưa cài app).
    // Hiển thị trang hướng dẫn tải app hoặc redirect về trang Blazor FoodTour.
    // ─────────────────────────────────────────────────────────────────────────
    [HttpGet("/foodtour/{shopId}")]
    public IActionResult FoodTourFallback(string shopId)
    {
        // Tạo trang HTML đơn giản hướng dẫn tải app
        // Nếu app đã cài, Android sẽ KHÔNG bao giờ vào route này (App Links tự mở app)
        var html = $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>FoodTour - Quét QR Nghe Thuyết Minh</title>
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        body {{
            font-family: 'Segoe UI', system-ui, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            color: white;
            padding: 20px;
        }}
        .card {{
            background: rgba(255,255,255,0.15);
            backdrop-filter: blur(20px);
            border-radius: 24px;
            padding: 40px 32px;
            text-align: center;
            max-width: 400px;
            width: 100%;
            box-shadow: 0 8px 32px rgba(0,0,0,0.2);
        }}
        .icon {{ font-size: 64px; margin-bottom: 16px; }}
        h1 {{ font-size: 24px; margin-bottom: 8px; }}
        p {{ font-size: 14px; opacity: 0.9; margin-bottom: 24px; line-height: 1.6; }}
        .btn {{
            display: inline-block;
            background: white;
            color: #764ba2;
            font-weight: 700;
            font-size: 16px;
            padding: 14px 32px;
            border-radius: 50px;
            text-decoration: none;
            transition: transform 0.2s;
        }}
        .btn:hover {{ transform: scale(1.05); }}
        .shop-id {{ font-size: 12px; opacity: 0.6; margin-top: 20px; }}
    </style>
</head>
<body>
    <div class=""card"">
        <div class=""icon"">🎧</div>
        <h1>FoodTour Audio Guide</h1>
        <p>Để nghe thuyết minh về quán ăn này, vui lòng tải ứng dụng FoodTour trên điện thoại.</p>
        <a href=""/api/app/download"" class=""btn"">📱 Tải App Ngay</a>
        <div class=""shop-id"">Shop ID: {shopId}</div>
    </div>
</body>
</html>";

        return Content(html, "text/html");
    }

    */
}
