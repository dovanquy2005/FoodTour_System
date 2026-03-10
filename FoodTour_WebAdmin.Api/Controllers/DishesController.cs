using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodTour_WebAdmin.Api.Data;
using FoodTour_WebAdmin.Api.Models;

namespace FoodTour_WebAdmin.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DishesController : ControllerBase
{
    private readonly AppDbContext _db;

    public DishesController(AppDbContext db) => _db = db;

    // GET: api/dishes
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DishModel>>> GetAll([FromQuery] string? shopId = null)
    {
        var query = _db.Dishes.AsQueryable();

        if (!string.IsNullOrEmpty(shopId))
            query = query.Where(d => d.ShopId == shopId);

        var dishesList = await query.Include(d => d.DishTranslations).ToListAsync();
        var dishes = dishesList.OrderBy(d => d.Name).ToList();
        return Ok(dishes);
    }

    // GET: api/dishes/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<DishModel>> GetById(string id)
    {
        var DishModel = await _db.Dishes.Include(d => d.DishTranslations).FirstOrDefaultAsync(d => d.Id == id);

        if (DishModel is null)
            return NotFound(new { message = $"DishModel with id '{id}' not found." });

        return Ok(DishModel);
    }

    // POST: api/dishes
    [HttpPost]
    public async Task<ActionResult<DishModel>> Create([FromBody] DishModel DishModel)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Verify ShopModel exists
        var shopExists = await _db.Shops.AnyAsync(s => s.Id == DishModel.ShopId);
        if (!shopExists)
            return BadRequest(new { message = $"ShopModel with id '{DishModel.ShopId}' does not exist." });

        if (string.IsNullOrEmpty(DishModel.Id))
            DishModel.Id = Guid.NewGuid().ToString();

        _db.Dishes.Add(DishModel);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = DishModel.Id }, DishModel);
    }

    // PUT: api/dishes/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<DishModel>> Update(string id, [FromBody] DishModel DishModel)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var existing = await _db.Dishes.FindAsync(id);
        if (existing is null)
            return NotFound(new { message = $"DishModel with id '{id}' not found." });

        // Verify ShopModel exists if ShopId changed
        if (existing.ShopId != DishModel.ShopId)
        {
            var shopExists = await _db.Shops.AnyAsync(s => s.Id == DishModel.ShopId);
            if (!shopExists)
                return BadRequest(new { message = $"ShopModel with id '{DishModel.ShopId}' does not exist." });
        }

        existing.ShopId = DishModel.ShopId;
        existing.Name = DishModel.Name;
        existing.Price = DishModel.Price;
        existing.ImageUrl = DishModel.ImageUrl;

        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    // DELETE: api/dishes/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var DishModel = await _db.Dishes.FindAsync(id);
        if (DishModel is null)
            return NotFound(new { message = $"DishModel with id '{id}' not found." });

        _db.Dishes.Remove(DishModel);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
