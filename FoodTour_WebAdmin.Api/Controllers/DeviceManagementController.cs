using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodTour_WebAdmin.Api.Data;
using FoodTour_WebAdmin.Api.Models;

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

    public DeviceManagementController(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
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

        var device = await db.UserDevices
            .FirstOrDefaultAsync(d => d.DeviceId == request.DeviceId);

        if (device is null)
        {
            // Lần đầu gặp thiết bị này — tạo mới
            device = new UserDeviceModel
            {
                DeviceId   = request.DeviceId,
                DeviceName = request.DeviceName ?? "Unknown Device",
                LastActive = DateTime.UtcNow,
                IsBlocked  = false
            };
            db.UserDevices.Add(device);
        }
        else
        {
            // Thiết bị đã biết — chỉ cập nhật LastActive (và DeviceName nếu có)
            device.LastActive = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(request.DeviceName))
                device.DeviceName = request.DeviceName;
        }

        await db.SaveChangesAsync();

        return Ok(new
        {
            deviceId  = device.DeviceId,
            isBlocked = device.IsBlocked,
            message   = "Đồng bộ thành công."
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/device/block/{deviceId}
    // Admin khóa một thiết bị.
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost("block/{deviceId}")]
    public async Task<IActionResult> Block(string deviceId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var device = await db.UserDevices
            .FirstOrDefaultAsync(d => d.DeviceId == deviceId);

        if (device is null)
            return NotFound(new { message = $"Không tìm thấy thiết bị '{deviceId}'." });

        device.IsBlocked = true;
        await db.SaveChangesAsync();

        return Ok(new { message = "Thiết bị đã bị khóa." });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/device/unblock/{deviceId}
    // Admin mở khóa một thiết bị.
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost("unblock/{deviceId}")]
    public async Task<IActionResult> Unblock(string deviceId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var device = await db.UserDevices
            .FirstOrDefaultAsync(d => d.DeviceId == deviceId);

        if (device is null)
            return NotFound(new { message = $"Không tìm thấy thiết bị '{deviceId}'." });

        device.IsBlocked = false;
        await db.SaveChangesAsync();

        return Ok(new { message = "Thiết bị đã được mở khóa." });
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// DTO — chỉ dùng trong phạm vi API này, không cần tạo file riêng
// ─────────────────────────────────────────────────────────────────────────────
public sealed record SyncDeviceRequest(string DeviceId, string? DeviceName);
