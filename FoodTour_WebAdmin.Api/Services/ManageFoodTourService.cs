using FoodTour_WebAdmin.Api.Data;
using FoodTour_WebAdmin.Api.Models;
using FoodTour_WebAdmin.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace FoodTour_WebAdmin.Api.Services;

public class ManageFoodTourService
{
    private readonly AppDbContext _context;
    private readonly LangblyTranslateService _translateService;
    
    // Cấu hình ngôn ngữ đích
    private readonly string[] _targetLanguages = { "en", "ja", "ru", "zh" };

    public ManageFoodTourService(AppDbContext context, LangblyTranslateService translateService)
    {
        _context = context;
        _translateService = translateService;
    }

    public async Task<ShopModel> CreateShopWithTranslationAsync(CreateShopRequest request)
    {
        // Bắt đầu Transaction
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var shopId = Guid.NewGuid().ToString();
            var shop = new ShopModel
            {
                Id = shopId,
                ImageUrl = request.ImageUrl,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Rating = request.Rating,
                IsVisited = request.IsVisited,
                ShopTranslations = new List<ShopTranslationModel>
                {
                    // Insert bản thu tiếng Việt gốc (vi) vào danh sách dịch
                    new ShopTranslationModel
                    {
                        LanguageCode = "vi",
                        Name = request.Name,
                        Address = request.Address,
                        Description = request.Description
                    }
                }
            };

            // Dùng LINQ Select để gom mảng Task. Thiết lập dịch song song toàn bộ.
            var languageTasks = _targetLanguages.Select(async lang =>
            {
                // Dịch Name, Address, Description CÙNG MỘT LÚC (gọi 3 request song song cho 1 ngôn ngữ)
                var nameTask = _translateService.TranslateTextAsync(request.Name, lang);
                var addressTask = _translateService.TranslateTextAsync(request.Address, lang);
                var descTask = _translateService.TranslateTextAsync(request.Description, lang);

                // Chờ cả 3 thuộc tính của ngôn ngữ này dịch xong
                await Task.WhenAll(nameTask, addressTask, descTask);

                return new ShopTranslationModel
                {
                    LanguageCode = lang,
                    Name = await nameTask,
                    Address = await addressTask,
                    Description = await descTask
                };
            });

            // Chờ TẤT CẢ các task ngôn ngữ chạy xong song song với WhenAll
            var translatedResults = await Task.WhenAll(languageTasks);

            // Add các language đã dịch vào collection
            foreach (var translation in translatedResults)
            {
                shop.ShopTranslations.Add(translation);
            }

            // Lưu thay đổi vào DB
            _context.Shops.Add(shop);
            await _context.SaveChangesAsync();
            
            // Commit đồng bộ dữ liệu chuẩn
            await transaction.CommitAsync();

            return shop;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<DishModel> CreateDishWithTranslationAsync(CreateDishRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var dishId = Guid.NewGuid().ToString();
            var dish = new DishModel
            {
                Id = dishId,
                ShopId = request.ShopId,
                Price = request.Price,
                ImageUrl = request.ImageUrl,
                DishTranslations = new List<DishTranslationModel>
                {
                    new DishTranslationModel
                    {
                        LanguageCode = "vi",
                        Name = request.Name
                    }
                }
            };

            // Chạy đa luồng gửi request dịch từng ngôn ngữ mục tiêu
            var languageTasks = _targetLanguages.Select(async lang =>
            {
                var translatedName = await _translateService.TranslateTextAsync(request.Name, lang);
                return new DishTranslationModel
                {
                    LanguageCode = lang,
                    Name = translatedName
                };
            });

            var translatedResults = await Task.WhenAll(languageTasks);

            foreach (var translation in translatedResults)
            {
                dish.DishTranslations.Add(translation);
            }

            _context.Dishes.Add(dish);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return dish;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task UpdateShopWithTranslationAsync(string shopId, CreateShopRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var shop = await _context.Shops.Include(s => s.ShopTranslations).FirstOrDefaultAsync(s => s.Id == shopId);
            if (shop == null) return;

            shop.ImageUrl = request.ImageUrl;
            shop.Latitude = request.Latitude;
            shop.Longitude = request.Longitude;
            shop.Rating = request.Rating;
            shop.IsVisited = request.IsVisited;
            shop.UpdatedAt = DateTime.UtcNow;

            var viTranslation = shop.ShopTranslations.FirstOrDefault(t => t.LanguageCode == "vi");
            if (viTranslation != null)
            {
                viTranslation.Name = request.Name;
                viTranslation.Address = request.Address;
                viTranslation.Description = request.Description;
            }
            else
            {
                 shop.ShopTranslations.Add(new ShopTranslationModel
                 {
                     LanguageCode = "vi",
                     Name = request.Name,
                     Address = request.Address,
                     Description = request.Description
                 });
            }

            var languageTasks = _targetLanguages.Select(async lang =>
            {
                var nameTask = _translateService.TranslateTextAsync(request.Name, lang);
                var addressTask = _translateService.TranslateTextAsync(request.Address, lang);
                var descTask = _translateService.TranslateTextAsync(request.Description, lang);
                await Task.WhenAll(nameTask, addressTask, descTask);
                return new { lang, name = await nameTask, address = await addressTask, desc = await descTask };
            });

            var translatedResults = await Task.WhenAll(languageTasks);

            foreach (var result in translatedResults)
            {
                var existingTranslation = shop.ShopTranslations.FirstOrDefault(t => t.LanguageCode == result.lang);
                if (existingTranslation != null)
                {
                    existingTranslation.Name = result.name;
                    existingTranslation.Address = result.address;
                    existingTranslation.Description = result.desc;
                }
                else
                {
                    shop.ShopTranslations.Add(new ShopTranslationModel
                    {
                        LanguageCode = result.lang,
                        Name = result.name,
                        Address = result.address,
                        Description = result.desc
                    });
                }
            }

            _context.Shops.Update(shop);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task UpdateDishWithTranslationAsync(string dishId, CreateDishRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var dish = await _context.Dishes.Include(d => d.DishTranslations).FirstOrDefaultAsync(d => d.Id == dishId);
            if (dish == null) return;

            dish.ShopId = request.ShopId;
            dish.Price = request.Price;
            dish.ImageUrl = request.ImageUrl;

            var viTranslation = dish.DishTranslations.FirstOrDefault(t => t.LanguageCode == "vi");
            if (viTranslation != null)
            {
                viTranslation.Name = request.Name;
            }
            else
            {
                dish.DishTranslations.Add(new DishTranslationModel
                {
                    LanguageCode = "vi",
                    Name = request.Name
                });
            }

            var languageTasks = _targetLanguages.Select(async lang =>
            {
                var translatedName = await _translateService.TranslateTextAsync(request.Name, lang);
                return new { lang, name = translatedName };
            });

            var translatedResults = await Task.WhenAll(languageTasks);

            foreach (var result in translatedResults)
            {
                var existingTranslation = dish.DishTranslations.FirstOrDefault(t => t.LanguageCode == result.lang);
                if (existingTranslation != null)
                {
                    existingTranslation.Name = result.name;
                }
                else
                {
                    dish.DishTranslations.Add(new DishTranslationModel
                    {
                        LanguageCode = result.lang,
                        Name = result.name
                    });
                }
            }

            _context.Dishes.Update(dish);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
