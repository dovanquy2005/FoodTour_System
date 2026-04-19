using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodTour_WebAdmin.Api.Data;
using FoodTour_WebAdmin.Api.Models;

namespace FoodTour_WebAdmin.Api.Controllers;

/// <summary>
/// API kiểm tra trạng thái thiết bị (Premium / Trial) phục vụ Deep Link.
/// Mobile gọi khi nhận Deep Link để quyết định phát audio đầy đủ hay giới hạn.
/// </summary>
[ApiController]
[Route("api/device")]
public class DeviceStatusController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    // Giới hạn số lần nghe thử trong 24 giờ
    private const int MaxTrialPerDay = 3;

    public DeviceStatusController(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/device/status/{deviceId}
    // Kiểm tra trạng thái Premium và số lần trial còn lại của thiết bị.
    // Nếu deviceId chưa tồn tại → tự động đăng ký mới với IsPremium = false.
    // ─────────────────────────────────────────────────────────────────────────
    [HttpGet("status/{deviceId}")]
    public async Task<IActionResult> CheckDeviceStatus(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            return BadRequest(new { message = "DeviceId không được để trống." });

        await using var db = await _dbFactory.CreateDbContextAsync();

        // Tìm thiết bị trong database
        var device = await db.UserDevices
            .FirstOrDefaultAsync(d => d.DeviceId == deviceId);

        if (device is null)
        {
            // Thiết bị chưa từng đăng ký — tạo mới với IsPremium = false
            device = new UserDeviceModel
            {
                DeviceId = deviceId,
                DeviceName = "Deep Link Device",
                Platform = "Android",
                LastActive = DateTime.UtcNow,
                IsPremium = false,
                PremiumExpiry = null
            };
            db.UserDevices.Add(device);
            await db.SaveChangesAsync();
        }

        // Kiểm tra xem Premium đã hết hạn chưa (nếu có thời hạn)
        bool isPremium = device.IsPremium;
        if (isPremium && device.PremiumExpiry.HasValue && device.PremiumExpiry.Value < DateTime.UtcNow)
        {
            // Premium đã hết hạn — tự động đánh dấu lại
            isPremium = false;
            device.IsPremium = false;
            await db.SaveChangesAsync();
        }

        // Đếm số lần trial trong 24 giờ qua
        var twentyFourHoursAgo = DateTime.UtcNow.AddHours(-24);
        var trialCount = await db.TrialLogs
            .Where(t => t.DeviceId == deviceId && t.CreatedAt >= twentyFourHoursAgo)
            .CountAsync();

        return Ok(new
        {
            isPremium,
            premiumExpiry = device.PremiumExpiry,
            trialCount,
            maxTrial = MaxTrialPerDay,
            trialRemaining = Math.Max(0, MaxTrialPerDay - trialCount)
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/device/trial
    // Ghi log một lượt nghe thử (Trial) cho thiết bị.
    // Kiểm tra giới hạn 3 lần / 24h dựa trên DeviceId (Hardware ID).
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost("trial")]
    public async Task<IActionResult> RecordDeviceTrial([FromBody] DeviceTrialRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceId))
            return BadRequest(new { message = "DeviceId không được để trống." });

        await using var db = await _dbFactory.CreateDbContextAsync();

        // Đếm số lần trial trong 24h qua
        var twentyFourHoursAgo = DateTime.UtcNow.AddHours(-24);
        var trialCount = await db.TrialLogs
            .Where(t => t.DeviceId == request.DeviceId && t.CreatedAt >= twentyFourHoursAgo)
            .CountAsync();

        // Chặn nếu đã vượt giới hạn
        if (trialCount >= MaxTrialPerDay)
        {
            return Ok(new
            {
                allowed = false,
                remaining = 0,
                reason = "limit_reached"
            });
        }

        // Ghi log trial mới
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";
        var log = new TrialLog
        {
            DeviceId = request.DeviceId,
            ShopId = request.ShopId ?? string.Empty,
            IPAddress = ipAddress,
            UserAgent = Request.Headers.UserAgent.ToString(),
            CreatedAt = DateTime.UtcNow
        };

        db.TrialLogs.Add(log);
        await db.SaveChangesAsync();

        return Ok(new
        {
            allowed = true,
            remaining = MaxTrialPerDay - trialCount - 1
        });
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// DTO — Request body cho API ghi trial
// ─────────────────────────────────────────────────────────────────────────────
public sealed record DeviceTrialRequest(string DeviceId, string? ShopId);
