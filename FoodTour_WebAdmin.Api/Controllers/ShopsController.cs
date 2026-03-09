using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodTour_WebAdmin.Api.Data;
using FoodTour_WebAdmin.Api.Models;

namespace FoodTour_WebAdmin.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShopsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ShopsController(AppDbContext db) => _db = db;

    // GET: api/shops
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Shop>>> GetAll()
    {
        var shops = await _db.Shops
            .OrderByDescending(s => s.Rating)
            .ToListAsync();
        return Ok(shops);
    }

    // GET: api/shops/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Shop>> GetById(string id)
    {
        var shop = await _db.Shops
            .Include(s => s.Dishes)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (shop is null)
            return NotFound(new { message = $"Shop with id '{id}' not found." });

        return Ok(shop);
    }

    // POST: api/shops
    [HttpPost]
    public async Task<ActionResult<Shop>> Create([FromBody] Shop shop)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (string.IsNullOrEmpty(shop.Id))
            shop.Id = Guid.NewGuid().ToString();

        shop.CreatedAt = DateTime.UtcNow;
        shop.UpdatedAt = DateTime.UtcNow;

        _db.Shops.Add(shop);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = shop.Id }, shop);
    }

    // PUT: api/shops/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<Shop>> Update(string id, [FromBody] Shop shop)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var existing = await _db.Shops.FindAsync(id);
        if (existing is null)
            return NotFound(new { message = $"Shop with id '{id}' not found." });

        existing.Name = shop.Name;
        existing.Address = shop.Address;
        existing.Description = shop.Description;
        existing.ImageUrl = shop.ImageUrl;
        existing.Latitude = shop.Latitude;
        existing.Longitude = shop.Longitude;
        existing.IsVisited = shop.IsVisited;
        existing.Rating = shop.Rating;
        existing.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    // DELETE: api/shops/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var shop = await _db.Shops.FindAsync(id);
        if (shop is null)
            return NotFound(new { message = $"Shop with id '{id}' not found." });

        _db.Shops.Remove(shop);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // GET: api/shops/stats
    [HttpGet("stats")]
    public async Task<ActionResult> GetStats()
    {
        var totalShops = await _db.Shops.CountAsync();
        var totalDishes = await _db.Dishes.CountAsync();
        var avgRating = totalShops > 0 ? await _db.Shops.AverageAsync(s => s.Rating) : 0;
        var topShop = await _db.Shops.OrderByDescending(s => s.Rating).FirstOrDefaultAsync();

        return Ok(new
        {
            totalShops,
            totalDishes,
            averageRating = Math.Round(avgRating, 1),
            topShopName = topShop?.Name ?? "N/A"
        });
    }
}
