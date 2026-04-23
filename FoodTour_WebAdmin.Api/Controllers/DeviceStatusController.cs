using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodTour_WebAdmin.Api.Data;
using FoodTour_WebAdmin.Api.Models;
using FoodTour_WebAdmin.Api.Services;

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
    private readonly IDataUpdateNotifier _notifier;

    // Giới hạn số lần quét QR chủ động trong 24 giờ (chỉ áp dụng cho AppScan)
    private const int MaxScanTrialPerDay = 3;

    public DeviceStatusController(IDbContextFactory<AppDbContext> dbFactory, IDataUpdateNotifier notifier)
    {
        _dbFactory = dbFactory;
        _notifier = notifier;
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

        // Đếm số lần quét QR chủ động (AppScan) trong 24 giờ qua
        // Chỉ đếm AppScan vì AppAuto không bị giới hạn
        var twentyFourHoursAgo = DateTime.UtcNow.AddHours(-24);
        var scanTrialCount = await db.TrialLogs
            .Where(t => t.DeviceId == deviceId
                     && t.TriggerType == TriggerType.AppScan
                     && t.CreatedAt >= twentyFourHoursAgo)
            .CountAsync();

        return Ok(new
        {
            isPremium,
            premiumExpiry = device.PremiumExpiry,
            trialCount = scanTrialCount,
            maxTrial = MaxScanTrialPerDay,
            trialRemaining = Math.Max(0, MaxScanTrialPerDay - scanTrialCount)
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/device/trial
    // Ghi log một lượt nghe (Trial/Analytics) cho thiết bị.
    // Chính sách phân loại theo TriggerType:
    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/device/trial?type=2
    // triggerType truyền qua QUERY PARAMETER (không qua JSON body) để tránh
    // mọi vấn đề enum/int deserialization giữa Mobile ↔ Backend.
    //   type=0 → Web, type=1 → AppScan, type=2 → AppAuto
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost("trial")]
    public async Task<IActionResult> RecordDeviceTrial(
        [FromBody] DeviceTrialRequest request,
        [FromQuery(Name = "type")] int type = 1)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceId))
            return BadRequest(new { message = "DeviceId không được để trống." });

        await using var db = await _dbFactory.CreateDbContextAsync();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";

        // ── Chuyển đổi int → enum, log để debug ──
        var triggerType = type switch
        {
            2 => TriggerType.AppAuto,
            1 => TriggerType.AppScan,
            0 => TriggerType.Web,
            _ => TriggerType.AppScan
        };
        Console.WriteLine($"[TrialAPI] DeviceId={request.DeviceId}, ShopId={request.ShopId}, " +
                          $"type(query)={type}, TriggerType={triggerType}");

        // ── AppAuto (2): Luôn cho phép — chỉ ghi log để Heatmap/Analytics ──
        if (triggerType == TriggerType.AppAuto)
        {
            db.TrialLogs.Add(new TrialLog
            {
                DeviceId = request.DeviceId,
                ShopId = request.ShopId ?? string.Empty,
                IPAddress = ipAddress,
                UserAgent = Request.Headers.UserAgent.ToString(),
                CreatedAt = DateTime.UtcNow,
                TriggerType = TriggerType.AppAuto
            });
            await db.SaveChangesAsync();
            _notifier.NotifyTrialRecorded();

            return Ok(new { allowed = true, remaining = -1, triggerType = 2 });
        }

        // ── AppScan (1) / Web (0): Giới hạn 3 lần / 24h ──
        var since = DateTime.UtcNow.AddHours(-24);
        var count = await db.TrialLogs
            .Where(t => t.DeviceId == request.DeviceId
                     && t.TriggerType == triggerType
                     && t.CreatedAt >= since)
            .CountAsync();

        if (count >= MaxScanTrialPerDay)
        {
            return Ok(new { allowed = false, remaining = 0, reason = "limit_reached", triggerType = type });
        }

        db.TrialLogs.Add(new TrialLog
        {
            DeviceId = request.DeviceId,
            ShopId = request.ShopId ?? string.Empty,
            IPAddress = ipAddress,
            UserAgent = Request.Headers.UserAgent.ToString(),
            CreatedAt = DateTime.UtcNow,
            TriggerType = triggerType
        });
        await db.SaveChangesAsync();
        _notifier.NotifyTrialRecorded();

        return Ok(new { allowed = true, remaining = MaxScanTrialPerDay - count - 1, triggerType = type });
    }
}

// DTO — chỉ chứa DeviceId + ShopId, TriggerType đã chuyển sang query param
public class DeviceTrialRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public string? ShopId { get; set; }
}

