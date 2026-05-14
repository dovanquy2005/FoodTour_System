using FoodTour_WebAdmin.Api.Data;
using FoodTour_WebAdmin.Api.Models;
using FoodTour_WebAdmin.Api.DTOs;
using FoodTour_WebAdmin.Api.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FoodTour_WebAdmin.Api.Services;

public class ManageFoodTourService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly LangblyTranslateService _translateService;
    private readonly ITtsService _ttsService;
    private readonly ISupabaseStorageService _storageService;
    private readonly ILogger<ManageFoodTourService> _logger;
    // SignalR Hub — dùng để đẩy thông báo cập nhật tới Mobile App
    private readonly IHubContext<UpdateHub> _hubContext;
    private readonly IDataUpdateNotifier _notifier;
    
    // Cấu hình ngôn ngữ đích
    private readonly string[] _targetLanguages = { "en", "ja", "ru", "zh" };
    // Tất cả ngôn ngữ (bao gồm vi gốc) — dùng cho TTS
    private readonly string[] _allLanguages = { "vi", "en", "ja", "ru", "zh" };

    public ManageFoodTourService(
        IDbContextFactory<AppDbContext> contextFactory,
        LangblyTranslateService translateService,
        ITtsService ttsService,
        ISupabaseStorageService storageService,
        ILogger<ManageFoodTourService> logger,
        IHubContext<UpdateHub> hubContext,
        IDataUpdateNotifier notifier)
    {
        _contextFactory = contextFactory;
        _translateService = translateService;
        _ttsService = ttsService;
        _storageService = storageService;
        _logger = logger;
        _hubContext = hubContext;
        _notifier = notifier;
    }

    public async Task<ShopModel> CreateShopWithTranslationAsync(CreateShopRequest request)
    {
        using var _context = await _contextFactory.CreateDbContextAsync();
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
                IsActive  = request.IsActive,
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

            // Khởi tạo danh sách translations với bản tiếng Việt
            var allTranslations = new List<ShopTranslationModel> { viTranslation };

            if (request.IsActive)
            {
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
                allTranslations.AddRange(translatedResults);

                // === BƯỚC 3: TTS + Upload Audio song song cho tất cả ngôn ngữ ===
                var ttsUploadTasks = allTranslations.Select(async t =>
                {
                    await GenerateAndUploadShopAudioAsync(t, shopId);
                });
                await Task.WhenAll(ttsUploadTasks);
            }

            // Add translations vào shop
            foreach (var t in allTranslations)
            {
                shop.ShopTranslations.Add(t);
            }

            // === BƯỚC 4: Lưu vào DB ===
            _context.Shops.Add(shop);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Thông báo tới tất cả Mobile App đang kết nối: có Shop mới được tạo
            await _hubContext.Clients.All.SendAsync("ReceiveUpdate", shop.Id);
            _notifier.NotifyShopUpdated();

            return shop;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }


    public async Task UpdateShopWithTranslationAsync(string shopId, CreateShopRequest request)
    {
        using var _context = await _contextFactory.CreateDbContextAsync();
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
            shop.IsActive  = request.IsActive;
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

            if (request.IsActive)
            {
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
            }

            _context.Shops.Update(shop);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Thông báo tới tất cả Mobile App: Shop vừa được cập nhật (audio, radius, text...)
            await _hubContext.Clients.All.SendAsync("ReceiveUpdate", shopId);
            _notifier.NotifyShopUpdated();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }


    // ═══════ Hàm tổng hợp Audio và tải lên (TTS & Upload) ═══════
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
        catch (Exception ex)
        {
            // Ghi log lỗi chi tiết để dễ debug (VD: thiếu bucket 'audios', API key Google sai...)
            _logger.LogError(ex, "Lỗi khi tạo Audio TTS hoặc Upload lên Supabase cho Shop '{ShopId}', ngôn ngữ '{Lang}'", shopId, translation.LanguageCode);
            translation.IsAudioGenerated = false;
        }
    }

    // ═══════ CRUD CHO SHOP ITEMS (PREMIUM) ═══════

    public async Task<ShopItem> CreateShopItemWithTranslationAsync(string shopId, CreateShopItemRequest request)
    {
        using var _context = await _contextFactory.CreateDbContextAsync();
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var shop = await _context.Shops.FindAsync(shopId);
            if (shop == null) throw new Exception("Shop not found");

            var shopItemId = Guid.NewGuid();
            var item = new ShopItem
            {
                Id = shopItemId,
                ShopId = shopId, // using string because ShopId is string
                IsPremiumOnly = request.IsPremiumOnly,
                ShopItemTranslations = new List<ShopItemTranslation>()
            };

            // === BƯỚC 1: Tạo bản gốc tiếng Việt ===
            var viTranslation = new ShopItemTranslation
            {
                LanguageCode = "vi",
                Title = request.Title,
                Description = request.Description
            };

            var allTranslations = new List<ShopItemTranslation> { viTranslation };

            // === BƯỚC 2: Dịch song song sang các ngôn ngữ đích ===
            var languageTasks = _targetLanguages.Select(async lang =>
            {
                var titleTask = _translateService.TranslateTextAsync(request.Title, lang);
                var descTask = _translateService.TranslateTextAsync(request.Description, lang);
                await Task.WhenAll(titleTask, descTask);

                return new ShopItemTranslation
                {
                    LanguageCode = lang,
                    Title = await titleTask,
                    Description = await descTask
                };
            });

            var translatedResults = await Task.WhenAll(languageTasks);
            allTranslations.AddRange(translatedResults);

            // === BƯỚC 3: TTS + Upload Audio song song cho tất cả ngôn ngữ ===
            var ttsUploadTasks = allTranslations.Select(async t =>
            {
                await GenerateAndUploadShopItemAudioAsync(t, shopItemId);
            });
            await Task.WhenAll(ttsUploadTasks);

            foreach (var t in allTranslations)
            {
                item.ShopItemTranslations.Add(t);
            }

            // === BƯỚC 4: Lưu vào DB ===
            _context.ShopItems.Add(item);
            
            // Cập nhật Shop UpdatedAt
            shop.UpdatedAt = DateTime.UtcNow;
            _context.Shops.Update(shop);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _hubContext.Clients.All.SendAsync("ReceiveUpdate", shopId);

            return item;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task UpdateShopItemWithTranslationAsync(string shopId, Guid itemId, CreateShopItemRequest request)
    {
        using var _context = await _contextFactory.CreateDbContextAsync();
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var item = await _context.ShopItems.Include(i => i.ShopItemTranslations).Include(i => i.Shop).FirstOrDefaultAsync(i => i.Id == itemId && i.ShopId.ToString() == shopId);
            if (item == null) return;

            item.IsPremiumOnly = request.IsPremiumOnly;
            item.Shop.UpdatedAt = DateTime.UtcNow;

            // Cập nhật bản tiếng Việt
            var viTranslation = item.ShopItemTranslations.FirstOrDefault(t => t.LanguageCode == "vi");
            if (viTranslation != null)
            {
                viTranslation.Title = request.Title;
                viTranslation.Description = request.Description;
            }
            else
            {
                viTranslation = new ShopItemTranslation
                {
                    LanguageCode = "vi",
                    Title = request.Title,
                    Description = request.Description
                };
                item.ShopItemTranslations.Add(viTranslation);
            }

            // Dịch song song
            var languageTasks = _targetLanguages.Select(async lang =>
            {
                var titleTask = _translateService.TranslateTextAsync(request.Title, lang);
                var descTask = _translateService.TranslateTextAsync(request.Description, lang);
                await Task.WhenAll(titleTask, descTask);
                return new { lang, title = await titleTask, desc = await descTask };
            });

            var translatedResults = await Task.WhenAll(languageTasks);

            foreach (var result in translatedResults)
            {
                var existingTranslation = item.ShopItemTranslations.FirstOrDefault(t => t.LanguageCode == result.lang);
                if (existingTranslation != null)
                {
                    existingTranslation.Title = result.title;
                    existingTranslation.Description = result.desc;
                }
                else
                {
                    item.ShopItemTranslations.Add(new ShopItemTranslation
                    {
                        LanguageCode = result.lang,
                        Title = result.title,
                        Description = result.desc
                    });
                }
            }

            // TTS + Upload Audio
            var ttsUploadTasks = item.ShopItemTranslations.Select(async t =>
            {
                await GenerateAndUploadShopItemAudioAsync(t, item.Id);
            });
            await Task.WhenAll(ttsUploadTasks);

            _context.ShopItems.Update(item);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _hubContext.Clients.All.SendAsync("ReceiveUpdate", shopId);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task DeleteShopItemAsync(string shopId, Guid itemId)
    {
        using var _context = await _contextFactory.CreateDbContextAsync();
        var item = await _context.ShopItems.Include(i => i.Shop).FirstOrDefaultAsync(i => i.Id == itemId && i.ShopId.ToString() == shopId);
        if (item == null) return;

        item.Shop.UpdatedAt = DateTime.UtcNow;
        _context.ShopItems.Remove(item);
        await _context.SaveChangesAsync();
        
        await _hubContext.Clients.All.SendAsync("ReceiveUpdate", shopId);
    }

    // ═══════ HELPER: TTS + Upload cho ShopItem ═══════
    private async Task GenerateAndUploadShopItemAudioAsync(ShopItemTranslation translation, Guid shopItemId)
    {
        try
        {
            var ttsText = translation.Description;
            if (string.IsNullOrWhiteSpace(ttsText))
                ttsText = translation.Title;

            var audioBytes = await _ttsService.SynthesizeSpeechAsync(ttsText, translation.LanguageCode);
            if (audioBytes.Length == 0) return;

            var fileName = $"shopitems/{shopItemId}/{translation.LanguageCode}_{Guid.NewGuid():N}.mp3";
            var audioUrl = await _storageService.UploadAudioAsync(audioBytes, fileName);

            translation.AudioUrl = audioUrl;
            translation.IsAudioGenerated = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo Audio TTS hoặc Upload lên Supabase cho ShopItem '{ShopItemId}', ngôn ngữ '{Lang}'", shopItemId, translation.LanguageCode);
            translation.IsAudioGenerated = false;
        }
    }

}
