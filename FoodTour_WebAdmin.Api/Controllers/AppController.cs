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
    public async Task<IActionResult> DownloadApp()
    {
        var userAgent = Request.Headers.UserAgent.ToString();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";

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

        var log = new DownloadLog
        {
            UserAgent = userAgent,
            IPAddress = ipAddress,
            DeviceType = deviceType,
            // Lấy phiên bản động từ GitHub Service giúp hệ thống luôn chính xác
            VersionDownloaded = await _githubReleaseService.GetLatestVersionAsync(),
            DownloadedAt = DateTime.UtcNow
        };

        _context.DownloadLogs.Add(log);
        await _context.SaveChangesAsync();

        // Sử dụng smartUrl để đảm bảo khách luôn nhận được bản mới nhất
        return Redirect("https://github.com/dovanquy2005/FoodTour_System/releases/latest/download/foodtour.apk");
    }
}
