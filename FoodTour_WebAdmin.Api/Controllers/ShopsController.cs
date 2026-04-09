using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodTour_WebAdmin.Api.Data;
using FoodTour_WebAdmin.Api.Models;

namespace FoodTour_WebAdmin.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShopsController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly FoodTour_WebAdmin.Api.Services.ManageFoodTourService _manageService;

    public ShopsController(IDbContextFactory<AppDbContext> dbFactory, FoodTour_WebAdmin.Api.Services.ManageFoodTourService manageService)
    {
        _dbFactory = dbFactory;
        _manageService = manageService;
    }

    // GET: api/shops
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ShopModel>>> GetAll()
    {
        using var _db = await _dbFactory.CreateDbContextAsync();
        var shops = await _db.Shops
            .Include(s => s.ShopTranslations)
            .OrderByDescending(s => s.Rating)
            .ToListAsync();
        return Ok(shops);
    }

    // GET: api/shops/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<ShopModel>> GetById(string id)
    {
        using var _db = await _dbFactory.CreateDbContextAsync();
        var ShopModel = await _db.Shops
            .Include(s => s.ShopTranslations)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (ShopModel is null)
            return NotFound(new { message = $"ShopModel with id '{id}' not found." });

        return Ok(ShopModel);
    }

    // POST: api/shops
    [HttpPost]
    public async Task<ActionResult<ShopModel>> Create([FromBody] FoodTour_WebAdmin.Api.DTOs.CreateShopRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var shop = await _manageService.CreateShopWithTranslationAsync(request);

        return CreatedAtAction(nameof(GetById), new { id = shop.Id }, shop);
    }

    // PUT: api/shops/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(string id, [FromBody] FoodTour_WebAdmin.Api.DTOs.CreateShopRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            await _manageService.UpdateShopWithTranslationAsync(id, request);
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi khi cập nhật Shop: " + ex.Message });
        }
    }

    // DELETE: api/shops/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        using var _db = await _dbFactory.CreateDbContextAsync();
        var ShopModel = await _db.Shops.FindAsync(id);
        if (ShopModel is null)
            return NotFound(new { message = $"ShopModel with id '{id}' not found." });

        _db.Shops.Remove(ShopModel);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // GET: api/shops/updates?since={ISO 8601 timestamp}
    // Kiểm tra xem có bản cập nhật mới kể từ lần đồng bộ cuối
    [HttpGet("updates")]
    public async Task<ActionResult> CheckForUpdates([FromQuery] string? since = null)
    {
        using var _db = await _dbFactory.CreateDbContextAsync();

        // Sử dụng cờ UTC để Npgsql/PostgreSQL không báo lỗi "Cannot write DateTime with Kind=Unspecified"
        DateTime sinceDate = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
        
        if (!string.IsNullOrEmpty(since))
        {
            if (!DateTime.TryParse(since, null, System.Globalization.DateTimeStyles.RoundtripKind, out sinceDate))
            {
                return BadRequest(new { message = "Invalid 'since' format. Use ISO 8601 (e.g., 2026-01-01T00:00:00Z)." });
            }
            // Đảm bảo là UTC
            if (sinceDate.Kind == DateTimeKind.Unspecified)
            {
                sinceDate = DateTime.SpecifyKind(sinceDate, DateTimeKind.Utc);
            }
        }

        // Lọc các shop có UpdatedAt > sinceDate
        var updatedShops = await _db.Shops
            .Include(s => s.ShopTranslations)
            .Where(s => s.UpdatedAt > sinceDate)
            .ToListAsync();

        if (updatedShops.Count == 0)
        {
            return Ok(new { hasUpdates = false, updatedShopIds = Array.Empty<string>(), totalEstimatedSize = 0L });
        }

        var updatedShopIds = updatedShops.Select(s => s.Id).ToList();

        // Ước tính dung lượng: đếm số file media cần tải (ảnh shop + audio translations)
        long estimatedSize = 0;
        foreach (var shop in updatedShops)
        {
            // Ảnh shop: ước tính ~200KB mỗi ảnh
            if (!string.IsNullOrEmpty(shop.ImageUrl))
                estimatedSize += 200_000;

            // Audio files: ước tính ~500KB mỗi file
            foreach (var trans in shop.ShopTranslations)
            {
                if (!string.IsNullOrEmpty(trans.AudioUrl) && trans.IsAudioGenerated)
                    estimatedSize += 500_000;
            }
        }

        return Ok(new
        {
            hasUpdates = true,
            updatedShopIds = updatedShopIds,
            totalEstimatedSize = estimatedSize
        });
    }

    // GET: api/shops/stats
    [HttpGet("stats")]
    public async Task<ActionResult> GetStats()
    {
        using var _db = await _dbFactory.CreateDbContextAsync();
        var totalShops = await _db.Shops.CountAsync();
        var avgRating = totalShops > 0 ? await _db.Shops.AverageAsync(s => s.Rating) : 0;
        var topShop = await _db.Shops.OrderByDescending(s => s.Rating).FirstOrDefaultAsync();

        return Ok(new
        {
            totalShops,
            averageRating = Math.Round(avgRating, 1),
            topShopName = topShop?.Name ?? "N/A"
        });
    }
}
