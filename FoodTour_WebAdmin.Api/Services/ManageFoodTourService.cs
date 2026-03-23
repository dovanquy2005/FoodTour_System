using FoodTour_WebAdmin.Api.Data;
using FoodTour_WebAdmin.Api.Models;
using FoodTour_WebAdmin.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace FoodTour_WebAdmin.Api.Services;

public class ManageFoodTourService
{
    private readonly AppDbContext _context;
    private readonly LangblyTranslateService _translateService;
    private readonly ITtsService _ttsService;
    private readonly ISupabaseStorageService _storageService;
    
    // Cấu hình ngôn ngữ đích
    private readonly string[] _targetLanguages = { "en", "ja", "ru", "zh" };
    // Tất cả ngôn ngữ (bao gồm vi gốc) — dùng cho TTS
    private readonly string[] _allLanguages = { "vi", "en", "ja", "ru", "zh" };

    public ManageFoodTourService(
        AppDbContext context,
        LangblyTranslateService translateService,
        ITtsService ttsService,
        ISupabaseStorageService storageService)
    {
        _context = context;
        _translateService = translateService;
        _ttsService = ttsService;
        _storageService = storageService;
    }

    public async Task<ShopModel> CreateShopWithTranslationAsync(CreateShopRequest request)
    {
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
                Radius = request.Radius,
                Priority = request.Priority,
                Rating = request.Rating,
                IsVisited = request.IsVisited,
                ShopTranslations = new List<ShopTranslationModel>()
            };

            // === BƯỚC 1: Tạo bản gốc tiếng Việt ===
            var viTranslation = new ShopTranslationModel
            {
                LanguageCode = "vi",
                Name = request.Name,
                Address = request.Address,
                Description = request.Description
            };

            // === BƯỚC 2: Dịch song song sang các ngôn ngữ đích ===
            var languageTasks = _targetLanguages.Select(async lang =>
            {
                var nameTask = _translateService.TranslateTextAsync(request.Name, lang);
                var addressTask = _translateService.TranslateTextAsync(request.Address, lang);
                var descTask = _translateService.TranslateTextAsync(request.Description, lang);
                await Task.WhenAll(nameTask, addressTask, descTask);

                return new ShopTranslationModel
                {
                    LanguageCode = lang,
                    Name = await nameTask,
                    Address = await addressTask,
                    Description = await descTask
                };
            });

            var translatedResults = await Task.WhenAll(languageTasks);

            // Gom tất cả translations
            var allTranslations = new List<ShopTranslationModel> { viTranslation };
            allTranslations.AddRange(translatedResults);

            // === BƯỚC 3: TTS + Upload Audio song song cho tất cả ngôn ngữ ===
            var ttsUploadTasks = allTranslations.Select(async t =>
            {
                await GenerateAndUploadShopAudioAsync(t, shopId);
            });
            await Task.WhenAll(ttsUploadTasks);

            // Add translations vào shop
            foreach (var t in allTranslations)
            {
                shop.ShopTranslations.Add(t);
            }

            // === BƯỚC 4: Lưu vào DB ===
            _context.Shops.Add(shop);
            await _context.SaveChangesAsync();
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
                DishTranslations = new List<DishTranslationModel>()
            };

            // Bản gốc tiếng Việt
            var viTranslation = new DishTranslationModel
            {
                LanguageCode = "vi",
                Name = request.Name
            };

            // Dịch song song
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

            var allTranslations = new List<DishTranslationModel> { viTranslation };
            allTranslations.AddRange(translatedResults);

            // TTS + Upload Audio song song
            var ttsUploadTasks = allTranslations.Select(async t =>
            {
                await GenerateAndUploadDishAudioAsync(t, dishId);
            });
            await Task.WhenAll(ttsUploadTasks);

            foreach (var t in allTranslations)
            {
                dish.DishTranslations.Add(t);
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
            shop.Radius = request.Radius;
            shop.Priority = request.Priority;
            shop.Rating = request.Rating;
            shop.IsVisited = request.IsVisited;
            shop.UpdatedAt = DateTime.UtcNow;

            // Cập nhật bản tiếng Việt
            var viTranslation = shop.ShopTranslations.FirstOrDefault(t => t.LanguageCode == "vi");
            if (viTranslation != null)
            {
                viTranslation.Name = request.Name;
                viTranslation.Address = request.Address;
                viTranslation.Description = request.Description;
            }
            else
            {
                viTranslation = new ShopTranslationModel
                {
                    LanguageCode = "vi",
                    Name = request.Name,
                    Address = request.Address,
                    Description = request.Description
                };
                shop.ShopTranslations.Add(viTranslation);
            }

            // Dịch song song
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

            // TTS + Upload Audio cho TẤT CẢ languages (bao gồm vi vừa cập nhật)
            var ttsUploadTasks = shop.ShopTranslations.Select(async t =>
            {
                await GenerateAndUploadShopAudioAsync(t, shopId);
            });
            await Task.WhenAll(ttsUploadTasks);

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
                viTranslation = new DishTranslationModel
                {
                    LanguageCode = "vi",
                    Name = request.Name
                };
                dish.DishTranslations.Add(viTranslation);
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

            // TTS + Upload Audio cho tất cả languages
            var ttsUploadTasks = dish.DishTranslations.Select(async t =>
            {
                await GenerateAndUploadDishAudioAsync(t, dishId);
            });
            await Task.WhenAll(ttsUploadTasks);

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

    // ═══════ HELPER: TTS + Upload cho Shop ═══════
    private async Task GenerateAndUploadShopAudioAsync(ShopTranslationModel translation, string shopId)
    {
        try
        {
            // Tạo nội dung TTS từ Description (kịch bản audio)
            var ttsText = translation.Description;
            if (string.IsNullOrWhiteSpace(ttsText))
                ttsText = translation.Name; // Fallback sang tên nếu mô tả rỗng

            // Gọi TTS API → nhận byte[] MP3
            var audioBytes = await _ttsService.SynthesizeSpeechAsync(ttsText, translation.LanguageCode);
            if (audioBytes.Length == 0) return;

            // Upload trực tiếp lên Supabase Storage (không qua file tạm)
            var fileName = $"shops/{shopId}/{translation.LanguageCode}_{Guid.NewGuid():N}.mp3";
            var audioUrl = await _storageService.UploadAudioAsync(audioBytes, fileName);

            // Cập nhật translation model
            translation.AudioUrl = audioUrl;
            translation.IsAudioGenerated = true;
        }
        catch (Exception)
        {
            // Ghi log nhưng không throw — TTS lỗi không nên block toàn bộ flow
            translation.IsAudioGenerated = false;
        }
    }

    // ═══════ HELPER: TTS + Upload cho Dish ═══════
    private async Task GenerateAndUploadDishAudioAsync(DishTranslationModel translation, string dishId)
    {
        try
        {
            var ttsText = translation.Name;
            if (string.IsNullOrWhiteSpace(ttsText)) return;

            var audioBytes = await _ttsService.SynthesizeSpeechAsync(ttsText, translation.LanguageCode);
            if (audioBytes.Length == 0) return;

            var fileName = $"dishes/{dishId}/{translation.LanguageCode}_{Guid.NewGuid():N}.mp3";
            var audioUrl = await _storageService.UploadAudioAsync(audioBytes, fileName);

            translation.AudioUrl = audioUrl;
            translation.IsAudioGenerated = true;
        }
        catch (Exception)
        {
            translation.IsAudioGenerated = false;
        }
    }
}
