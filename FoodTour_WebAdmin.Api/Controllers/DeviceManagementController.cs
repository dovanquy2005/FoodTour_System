using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodTour_WebAdmin.Api.Data;
using FoodTour_WebAdmin.Api.Models;
using FoodTour_WebAdmin.Api.Services;

namespace FoodTour_WebAdmin.Api.Controllers;

/// <summary>
/// Quản lý thiết bị mobile: đồng bộ DeviceID và khóa/mở thiết bị.
/// Route: POST /api/device/sync
///         POST /api/device/block/{deviceId}
///         POST /api/device/unblock/{deviceId}
/// </summary>
[ApiController]
[Route("api/device")]
public class DeviceManagementController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IDataUpdateNotifier _notifier;

    public DeviceManagementController(IDbContextFactory<AppDbContext> dbFactory, IDataUpdateNotifier notifier)
    {
        _dbFactory = dbFactory;
        _notifier = notifier;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/device/sync
    // Mobile gọi mỗi lần khởi động để đăng ký hoặc cập nhật LastActive.
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost("sync")]
    public async Task<IActionResult> Sync([FromBody] SyncDeviceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceId))
            return BadRequest(new { message = "DeviceId không được để trống." });

        await using var db = await _dbFactory.CreateDbContextAsync();

        // lấy thông tin thiết bị theo DeviceId
        var device = await db.UserDevices
            .FirstOrDefaultAsync(d => d.DeviceId == request.DeviceId);

        if (device is null)
        {
            // Lần đầu gặp thiết bị này — tạo mới
            device = new UserDeviceModel
            {
                DeviceId = request.DeviceId,
                DeviceName = request.DeviceName ?? "Unknown Device",
                Platform = request.Platform ?? "Unknown",
                LastActive = DateTime.UtcNow
            };
            // thêm thiết bị mới vào database
            db.UserDevices.Add(device);
        }
        else
        {
            // Thiết bị đã biết — chỉ cập nhật LastActive (và thông tin khác nếu có)
            device.LastActive = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(request.DeviceName))
                device.DeviceName = request.DeviceName;
            if (!string.IsNullOrWhiteSpace(request.Platform))
                device.Platform = request.Platform;
        }

        await db.SaveChangesAsync();

        // Dashboard re-render qua Blazor circuit
        _notifier.NotifyDeviceUpdated();

        return Ok(new { message = "Đồng bộ thành công." });
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// DTO — chỉ dùng trong phạm vi API này, không cần tạo file riêng
// ─────────────────────────────────────────────────────────────────────────────
public sealed record SyncDeviceRequest(string DeviceId, string? DeviceName, string? Platform);
