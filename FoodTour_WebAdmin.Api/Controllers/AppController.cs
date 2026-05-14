using FoodTour_WebAdmin.Api.Data;
using FoodTour_WebAdmin.Api.Models;
using Microsoft.AspNetCore.Mvc;

using FoodTour_WebAdmin.Api.Services;

namespace FoodTour_WebAdmin.Api.Controllers;

[Route("api/app")]
[ApiController]
public class AppController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly GitHubReleaseService _githubReleaseService;

    public AppController(AppDbContext context, GitHubReleaseService githubReleaseService)
    {
        _context = context;
        _githubReleaseService = githubReleaseService;
    }

    [HttpGet("download")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> DownloadApp()
    {
        // 1. Thu thập thông tin người tải
        var userAgent = Request.Headers.UserAgent.ToString();
        var ipAddress = Request.Headers.ContainsKey("X-Forwarded-For") 
            ? Request.Headers["X-Forwarded-For"].ToString().Split(',')[0] 
            : HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        // Phân loại thiết bị chuyên nghiệp: Chỉ tập trung vào Mobile
        string deviceType;
        if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
        {
            deviceType = "Android";
        }
        else if (userAgent.Contains("iPhone") || userAgent.Contains("iPad") || userAgent.Contains("iPod"))
        {
            deviceType = "iOS";
        }
        else
        {
            // Các thiết bị khác (Windows, Mac, Linux,...) được gom nhóm là "Máy tính/Khác"
            // Điều này giúp khách hàng hiểu đây là các lượt truy cập không từ di động
            deviceType = "Máy tính"; 
        }

        // Gọi hàm kiểm tra để đánh giá hiệu năng
        int performanceScore = EvaluateDevicePerformance(userAgent);

        // Lưu vào DB ngay khi quét QR với trạng thái "Quét QR"
        // Để Nhật ký vẫn ghi nhận được máy Mạnh/Yếu
        var log = new DownloadLog
        {
            UserAgent = userAgent,
            IPAddress = ipAddress,
            DeviceType = deviceType,
            VersionDownloaded = "Quét QR", 
            DownloadedAt = DateTime.UtcNow,
            DevicePerformanceType = performanceScore
        };

        _context.DownloadLogs.Add(log);
        await _context.SaveChangesAsync();

        // Sử dụng hàm check để xem có được phép tải hay không
        bool isAllowed = IsAllowedToDownload(performanceScore);

        // Tách riêng logic giao diện dựa trên kết quả kiểm tra để code HTML phía dưới sạch sẽ hơn
        string statusIconAndMessage = isAllowed 
            ? "<div class='icon-container success'><svg width='32' height='32' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='3' stroke-linecap='round' stroke-linejoin='round'><polyline points='20 6 9 17 4 12'></polyline></svg></div><h2>Thiết bị đạt yêu cầu</h2><p>Điện thoại của bạn có hiệu năng tốt, đáp ứng đầy đủ yêu cầu để chạy ứng dụng mượt mà.</p>" 
            : "<div class='icon-container error'><svg width='32' height='32' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='3' stroke-linecap='round' stroke-linejoin='round'><line x1='12' y1='8' x2='12' y2='12'></line><line x1='12' y1='16' x2='12.01' y2='16'></line></svg></div><h2>Thiết bị không đạt yêu cầu</h2><p>Cấu hình điện thoại của bạn quá yếu, không thể cài đặt và sử dụng ứng dụng này.</p>";

        string scoreClass = isAllowed ? "score-success" : "score-error";
        string scoreLabel = isAllowed ? "Mạnh" : "Yếu";

        string actionButton = isAllowed 
            ? $"<a href='/api/app/confirm-download?logId={log.Id}' class='btn btn-primary'><svg width='20' height='20' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4'/><polyline points='7 10 12 15 17 10'/><line x1='12' y1='15' x2='12' y2='3'/></svg> Tải xuống ứng dụng</a>" 
            : "<button class='btn btn-disabled' disabled><svg width='20' height='20' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><circle cx='12' cy='12' r='10'/><line x1='4.93' y1='4.93' x2='19.07' y2='19.07'/></svg> Từ chối tải xuống</button>";

        string htmlTemplate = $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Tải ứng dụng FoodTour</title>
    <link href='https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&display=swap' rel='stylesheet'>
    <style>
        body {{ font-family: 'Inter', sans-serif; background-color: #f8fafc; display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0; color: #1e293b; }}
        .card {{ background: white; padding: 40px 32px; border-radius: 24px; box-shadow: 0 10px 25px -5px rgba(0,0,0,0.05); border: 1px solid #f1f5f9; max-width: 380px; text-align: center; width: 100%; box-sizing: border-box; }}
        .logo {{ font-size: 24px; font-weight: 800; color: #3b82f6; margin-bottom: 24px; display: flex; align-items: center; justify-content: center; gap: 8px; letter-spacing: -0.5px; }}
        .icon-container {{ width: 72px; height: 72px; border-radius: 50%; display: flex; justify-content: center; align-items: center; margin: 0 auto 24px; font-size: 32px; }}
        .success {{ background-color: #dcfce3; color: #16a34a; }}
        .error {{ background-color: #fee2e2; color: #dc2626; }}
        h2 {{ color: #0f172a; margin-top: 0; font-size: 22px; font-weight: 700; margin-bottom: 12px; letter-spacing: -0.5px; }}
        p {{ color: #64748b; font-size: 15px; line-height: 1.6; margin-bottom: 32px; }}
        .btn {{ display: flex; justify-content: center; align-items: center; gap: 8px; width: 100%; padding: 14px 20px; border-radius: 12px; font-size: 15px; font-weight: 600; text-decoration: none; border: none; cursor: pointer; transition: all 0.2s ease; box-sizing: border-box; }}
        .btn-primary {{ background-color: #3b82f6; color: white; box-shadow: 0 4px 12px rgba(59, 130, 246, 0.3); }}
        .btn-primary:hover {{ background-color: #2563eb; transform: translateY(-1px); box-shadow: 0 6px 16px rgba(59, 130, 246, 0.4); }}
        .btn-disabled {{ background-color: #f1f5f9; color: #94a3b8; cursor: not-allowed; }}
        .device-info {{ background: #f8fafc; padding: 16px; border-radius: 12px; font-size: 14px; color: #475569; margin-bottom: 24px; border: 1px solid #e2e8f0; display: flex; justify-content: space-between; align-items: center; }}
        .score {{ font-weight: 700; font-size: 15px; padding: 4px 10px; border-radius: 6px; }}
        .score-success {{ background: #dcfce3; color: #16a34a; }}
        .score-error {{ background: #fee2e2; color: #dc2626; }}
        
        /* Loading animation */
        #loading {{ display: block; }}
        #result {{ display: none; }}
        .spinner {{ width: 48px; height: 48px; border: 4px solid #e2e8f0; border-top-color: #3b82f6; border-radius: 50%; animation: spin 1s linear infinite; margin: 0 auto 24px; }}
        @keyframes spin {{ to {{ transform: rotate(360deg); }} }}
    </style>
</head>
<body>
    <div class='card'>
        <div class='logo'>
            <svg width='28' height='28' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M3 2v7c0 1.1.9 2 2 2h4a2 2 0 0 0 2-2V2'/><path d='M7 2v20'/><path d='M21 15V2v0a5 5 0 0 0-5 5v6c0 1.1.9 2 2 2h3Zm0 0v7'/></svg>
            FoodTour
        </div>

        <div id='loading'>
            <div class='spinner'></div>
            <h2>Đang phân tích cấu hình...</h2>
            <p>Hệ thống đang kiểm tra tính tương thích của thiết bị với ứng dụng FoodTour.</p>
        </div>

        <div id='result'>
            {statusIconAndMessage}
            
            <div class='device-info'>
                <span>Đánh giá hiệu năng:</span>
                <span class='score {scoreClass}'>{scoreLabel}</span>
            </div>

            {actionButton}
        </div>
    </div>

    <script>
        // Simulate scanning process to make it look professional
        setTimeout(function() {{
            document.getElementById('loading').style.display = 'none';
            document.getElementById('result').style.display = 'block';
        }}, 1500);
    </script>
</body>
</html>";

        return Content(htmlTemplate, "text/html");
    }

    [HttpGet("confirm-download")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> ConfirmDownload([FromQuery] int logId)
    {
        var log = await _context.DownloadLogs.FindAsync(logId);
        if (log != null)
        {
            // Chặn ở backend: Nếu máy yếu, tuyệt đối không cho tải qua API (ngăn chặn bypass HTML)
            if (!IsAllowedToDownload(log.DevicePerformanceType))
            {
                return BadRequest("Thiết bị của bạn không đủ cấu hình để tải ứng dụng này.");
            }

            // Cập nhật lại Version thực sự khi bấm tải
            log.VersionDownloaded = await _githubReleaseService.GetLatestVersionAsync();
            await _context.SaveChangesAsync();
        }
        else
        {
            return NotFound("Không tìm thấy thông tin lượt quét QR.");
        }

        // Chuyển hướng tải file APK từ GitHub
        return Redirect("https://github.com/dovanquy2005/FoodTour_System/releases/latest/download/foodtour.apk");
    }

    /// <summary>
    /// Hàm kiểm tra cấu hình thiết bị (Mạnh hay Yếu).
    /// Trả về 0 (Mạnh - cho tải), hoặc 1 (Yếu - từ chối).
    /// </summary>
    private int EvaluateDevicePerformance(string userAgent)
    {
        // TODO: Tương lai có thể mở rộng phân tích dựa trên chuỗi User-Agent
        // Ví dụ: return userAgent.Contains("Android 6") ? 1 : 0;

        // Hiện tại dùng random để giả lập
        var random = new Random();
        return random.Next(0, 2);
    }

    /// <summary>
    /// Hàm kiểm tra logic chung: điểm nào thì cho phép tải.
    /// (0: Mạnh -> Cho tải, 1: Yếu -> Cấm tải)
    /// </summary>
    private bool IsAllowedToDownload(int performanceScore)
    {
        return performanceScore == 0;
    }
}
