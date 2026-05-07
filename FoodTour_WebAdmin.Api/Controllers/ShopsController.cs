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
    // Chỉ trả về các quán đã kích hoạt (IsActive = true) cho Mobile App
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ShopModel>>> GetAll()
    {
        using var _db = await _dbFactory.CreateDbContextAsync();
        var shops = await _db.Shops
            .Include(s => s.ShopTranslations)
            .Include(s => s.ShopItems).ThenInclude(si => si.ShopItemTranslations)
            .Where(s => s.IsActive)           // ← chỉ gửi quán đang kích hoạt
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
            .Include(s => s.ShopItems).ThenInclude(si => si.ShopItemTranslations)
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

    // ═══════ CRUD CHO SHOP ITEMS (PREMIUM) ═══════
    [HttpPost("{shopId}/items")]
    public async Task<ActionResult<ShopItem>> AddShopItem(string shopId, [FromBody] FoodTour_WebAdmin.Api.DTOs.CreateShopItemRequest request)
    {
        try
        {
            var item = await _manageService.CreateShopItemWithTranslationAsync(shopId, request);
            return Ok(item);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{shopId}/items/{itemId}")]
    public async Task<ActionResult> UpdateShopItem(string shopId, Guid itemId, [FromBody] FoodTour_WebAdmin.Api.DTOs.CreateShopItemRequest request)
    {
        try
        {
            await _manageService.UpdateShopItemWithTranslationAsync(shopId, itemId, request);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{shopId}/items/{itemId}")]
    public async Task<IActionResult> DeleteShopItem(string shopId, Guid itemId)
    {
        try
        {
            await _manageService.DeleteShopItemAsync(shopId, itemId);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
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

        // Lọc các shop có UpdatedAt > sinceDate (bao gồm cả quán mới deactivated để app xóa)
        var updatedShops = await _db.Shops
            .Include(s => s.ShopTranslations)
            .Include(s => s.ShopItems).ThenInclude(si => si.ShopItemTranslations)
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

            // ShopItems audio files
            foreach (var item in shop.ShopItems)
            {
                if (item.ShopItemTranslations != null)
                {
                    foreach (var itemTrans in item.ShopItemTranslations)
                    {
                        if (!string.IsNullOrEmpty(itemTrans.AudioUrl) && itemTrans.IsAudioGenerated)
                            estimatedSize += 500_000;
                    }
                }
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

    // ═══════ API TEST JMETER (MÔ PHỎNG LOGIC ĐI BỘ TRÊN MOBILE) ═══════
    // POST: api/shops/nearby
    // Dành cho JMeter test Logic Priority & Distance tại các vùng giao thoa
    [HttpPost("nearby")]
    public async Task<ActionResult> GetNearbyShops([FromBody] FoodTour_WebAdmin.Api.DTOs.NearbyRequest request)
    {
        using var _db = await _dbFactory.CreateDbContextAsync();
        
        // 1. Lấy tất cả quán đang kích hoạt
        var activeShops = await _db.Shops
            .Include(s => s.ShopTranslations)
            .Where(s => s.IsActive)
            .ToListAsync();

        // 2. Tính toán khoảng cách và lọc quán nằm trong bán kính
        var nearbyShops = activeShops
            .Select(s => new 
            {
                Shop = s,
                DistanceMeters = CalculateDistance(request.Lat, request.Lng, s.Latitude, s.Longitude),
                ActivationRadius = s.Radius > 0 ? s.Radius : 50.0 // Mặc định 50m nếu quán chưa set
            })
            .Where(x => x.DistanceMeters <= x.ActivationRadius)
            // 3. LOGIC ƯU TIÊN: Ưu tiên Priority (số nhỏ hơn) -> Ưu tiên khoảng cách (gần hơn)
            .OrderBy(x => x.Shop.Priority) 
            .ThenBy(x => x.DistanceMeters) 
            .Select(x => new 
            {
                Id = x.Shop.Id,
                Name = x.Shop.Name,
                Priority = x.Shop.Priority,
                DistanceMeters = Math.Round(x.DistanceMeters, 2),
                Radius = x.ActivationRadius,
                Latitude = x.Shop.Latitude,
                Longitude = x.Shop.Longitude
            })
            .ToList();

        return Ok(new
        {
            UserLocation = new { request.Lat, request.Lng },
            TotalFound = nearbyShops.Count,
            // Kết quả trả về sẽ xếp Quán ưu tiên cao nhất (hoặc gần nhất nếu cùng Priority) lên đầu tiên [0]
            Results = nearbyShops
        });
    }

    // Helper: Tính khoảng cách (mét) giữa 2 tọa độ GPS (Công thức Haversine - tương đương Location.CalculateDistance trên Mobile)
    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = (lat2 - lat1) * Math.PI / 180.0;
        var dLon = (lon2 - lon1) * Math.PI / 180.0;

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        // Bán kính trái đất ~6371km = 6371000m
        return 6371000 * c;
    }
}
