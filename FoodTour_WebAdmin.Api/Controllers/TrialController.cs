using FoodTour_WebAdmin.Api.Data;
using FoodTour_WebAdmin.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace FoodTour_WebAdmin.Api.Controllers;

[Route("api/trial")]
[ApiController]
public class TrialController : ControllerBase
{
    private readonly AppDbContext _context;

    public TrialController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("record")]
    public async Task<IActionResult> RecordTrial([FromQuery] string? shopId)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";
        var userAgent = Request.Headers.UserAgent.ToString();

        // 1. Kiểm tra xem IP này đã dùng bao nhiêu lần trong 24h qua
        var twentyFourHoursAgo = DateTime.UtcNow.AddHours(-24);
        var trialCount = await _context.TrialLogs
            .Where(t => t.IPAddress == ipAddress && t.CreatedAt >= twentyFourHoursAgo)
            .CountAsync();

        // 2. Chặn nếu vượt quá 3 lần
        if (trialCount >= 3)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { success = false, reason = "limit_reached" });
        }

        // 3. Nếu chưa vượt quá, thêm log mới
        var log = new TrialLog
        {
            IPAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow,
            ShopId = shopId
        };

        _context.TrialLogs.Add(log);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, remaining = 3 - trialCount - 1 });
    }
}
