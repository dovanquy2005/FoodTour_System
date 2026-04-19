using FoodTour_WebAdmin.Api.Data;
using FoodTour_WebAdmin.Api.Models;
using FoodTour_WebAdmin.Api.DTOs;
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
    public async Task<IActionResult> RecordTrial([FromBody] TrialRequest request) // Chuyển sang nhận Body
    {
        if (string.IsNullOrEmpty(request.Fingerprint))
        {
            return BadRequest(new { success = false, message = "Missing fingerprint" });
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";
        var userAgent = Request.Headers.UserAgent.ToString();
        var twentyFourHoursAgo = DateTime.UtcNow.AddHours(-24);

        // FIX: Kiểm tra theo BrowserFingerprint thay vì IPAddress
        var trialCount = await _context.TrialLogs
            .Where(t => t.BrowserFingerprint == request.Fingerprint 
                     && t.ShopId == request.ShopId 
                     && t.CreatedAt >= twentyFourHoursAgo)
            .CountAsync();

        if (trialCount >= 3)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { success = false, reason = "limit_reached" });
        }

        var log = new TrialLog
        {
            BrowserFingerprint = request.Fingerprint, // Lưu mã vân tay vào đây
            IPAddress = ipAddress, // Vẫn lưu IP để bạn theo dõi/đối soát khi cần
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow,
            ShopId = request.ShopId
        };

        _context.TrialLogs.Add(log);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, remaining = 3 - trialCount - 1 });
    }
}