using SQLite;
using FoodTour.Mobile.Models;
using System.Net.Http.Json;

namespace FoodTour.Mobile.Services
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection? _database;

        async Task Init()
        {
            if (_database is not null)
                return;

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "FoodTour.db3");
            _database = new SQLiteAsyncConnection(dbPath);

            // Migration V3: Reset bảng cũ để đảm bảo chuẩn schema mới
            // (thêm Radius, Priority, CreatedAt, UpdatedAt, AudioUrl, IsAudioGenerated; đổi PK Translation)
            if (!Preferences.Default.ContainsKey("DatabaseMigratedV3"))
            {
                await _database.DropTableAsync<ShopTranslationModel>();
                await _database.DropTableAsync<DishTranslationModel>();
                await _database.DropTableAsync<ShopModel>();
                await _database.DropTableAsync<DishModel>();
                Preferences.Default.Set("DatabaseMigratedV3", true);
            }

            await _database.CreateTableAsync<ShopModel>();
            await _database.CreateTableAsync<DishModel>();
            await _database.CreateTableAsync<ShopTranslationModel>();
            await _database.CreateTableAsync<DishTranslationModel>();
        }

        // ═══════ IMAGE & AUDIO CACHING ═══════

        private async Task<string> DownloadAndCacheFileAsync(HttpClient httpClient, string apiUrl, string relativeUrl)
        {
            if (string.IsNullOrEmpty(relativeUrl)) return relativeUrl;
            
            try
            {
                var fileName = Path.GetFileName(relativeUrl);
                var localPath = Path.Combine(FileSystem.AppDataDirectory, fileName);
                
                if (!File.Exists(localPath))
                {
                    var fullUrl = apiUrl.TrimEnd('/') + relativeUrl;
                    var fileBytes = await httpClient.GetByteArrayAsync(fullUrl);
                    await File.WriteAllBytesAsync(localPath, fileBytes);
                }
                return localPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Download file error ({relativeUrl}): {ex.Message}");
                return relativeUrl;
            }
        }

        // ═══════ SYNC ═══════

        public async Task<bool> SyncDataFromApiAsync(string apiUrl)
        {
            await Init();

            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                System.Diagnostics.Debug.WriteLine("SyncDataFromApiAsync: Không có kết nối mạng, bỏ qua đồng bộ.");
                return false;
            }

            try
            {
                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

                // ──── 1. Sync Shops ────
                var shopsResponse = await httpClient.GetAsync($"{apiUrl}/api/shops");
                if (shopsResponse.IsSuccessStatusCode)
                {
                    var shops = await shopsResponse.Content.ReadFromJsonAsync<List<ShopModel>>();
                    if (shops != null && shops.Count > 0)
                    {
                        // Pre-fetch existing shops để so sánh UpdatedAt
                        var existingShops = await _database!.Table<ShopModel>().ToListAsync();
                        var existingShopDict = existingShops.ToDictionary(s => s.Id);

                        await _database.RunInTransactionAsync(db =>
                        {
                            foreach (var shop in shops)
                            {
                                if (existingShopDict.TryGetValue(shop.Id, out var existing))
                                {
                                    // Chỉ update nếu dữ liệu server mới hơn
                                    if (shop.UpdatedAt > existing.UpdatedAt)
                                    {
                                        db.InsertOrReplace(shop);
                                    }
                                }
                                else
                                {
                                    db.Insert(shop);
                                }
                            }
                        });

                        // Upsert translations (dùng server PK nên InsertOrReplace là đúng)
                        await _database.RunInTransactionAsync(db =>
                        {
                            foreach (var shop in shops)
                            {
                                if (shop.ShopTranslations != null)
                                {
                                    foreach (var trans in shop.ShopTranslations)
                                    {
                                        db.InsertOrReplace(trans);
                                    }
                                }
                            }
                        });

                        // Tải ảnh và audio về cache (ngoài transaction, vì là I/O network)
                        foreach (var shop in shops)
                        {
                            if (!string.IsNullOrEmpty(shop.ImageUrl) && shop.ImageUrl.StartsWith("/"))
                            {
                                await DownloadAndCacheFileAsync(httpClient, apiUrl, shop.ImageUrl);
                            }
                            if (shop.ShopTranslations != null)
                            {
                                foreach (var trans in shop.ShopTranslations)
                                {
                                    if (!string.IsNullOrEmpty(trans.AudioUrl) && trans.AudioUrl.StartsWith("/"))
                                    {
                                        await DownloadAndCacheFileAsync(httpClient, apiUrl, trans.AudioUrl);
                                    }
                                }
                            }
                        }
                    }
                }

                // ──── 2. Sync Dishes ────
                var dishesResponse = await httpClient.GetAsync($"{apiUrl}/api/dishes");
                if (dishesResponse.IsSuccessStatusCode)
                {
                    var dishes = await dishesResponse.Content.ReadFromJsonAsync<List<DishModel>>();
                    if (dishes != null && dishes.Count > 0)
                    {
                        // Dishes không có UpdatedAt trên server, dùng InsertOrReplace trực tiếp
                        await _database!.RunInTransactionAsync(db =>
                        {
                            foreach (var dish in dishes)
                            {
                                db.InsertOrReplace(dish);
                            }
                        });

                        // Upsert dish translations
                        await _database.RunInTransactionAsync(db =>
                        {
                            foreach (var dish in dishes)
                            {
                                if (dish.DishTranslations != null)
                                {
                                    foreach (var trans in dish.DishTranslations)
                                    {
                                        db.InsertOrReplace(trans);
                                    }
                                }
                            }
                        });

                        // Tải ảnh Dish về cache
                        foreach (var dish in dishes)
                        {
                            if (!string.IsNullOrEmpty(dish.ImageUrl) && dish.ImageUrl.StartsWith("/"))
                            {
                                await DownloadAndCacheFileAsync(httpClient, apiUrl, dish.ImageUrl);
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Sync Error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> FullSyncAsync(string apiUrl, ILocalizationService localizationService)
        {
            bool dataSuccess = await SyncDataFromApiAsync(apiUrl);
            
            try
            {
                await localizationService.PreloadAllLanguagesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Language Sync Error: {ex.Message}");
            }

            return dataSuccess;
        }

        // ═══════ GET & BINDING ═══════

        public async Task<List<ShopModel>> GetShopsAsync()
        {
            await Init();
            var langCode = Preferences.Default.Get("AppLanguage", "vi");
            var shops = await _database!.Table<ShopModel>().ToListAsync();

            foreach (var shop in shops)
            {
                var trans = await _database.Table<ShopTranslationModel>()
                    .Where(t => t.ShopId == shop.Id && t.LanguageCode == langCode)
                    .FirstOrDefaultAsync();

                if (trans != null)
                {
                    shop.Name = trans.Name;
                    shop.Address = trans.Address;
                    shop.Description = trans.Description;
                    shop.AudioUrl = trans.AudioUrl;
                }
            }
            return shops;
        }

        public async Task<ShopModel?> GetShopAsync(string id)
        {
            await Init();
            var langCode = Preferences.Default.Get("AppLanguage", "vi");
            var shop = await _database!.Table<ShopModel>().Where(i => i.Id == id).FirstOrDefaultAsync();
            if (shop != null)
            {
                var trans = await _database.Table<ShopTranslationModel>()
                    .Where(t => t.ShopId == shop.Id && t.LanguageCode == langCode)
                    .FirstOrDefaultAsync();

                if (trans != null)
                {
                    shop.Name = trans.Name;
                    shop.Address = trans.Address;
                    shop.Description = trans.Description;
                    shop.AudioUrl = trans.AudioUrl;
                }
            }
            return shop;
        }

        public async Task<List<DishModel>> GetDishesByShopAsync(string shopId)
        {
            await Init();
            var langCode = Preferences.Default.Get("AppLanguage", "vi");
            var dishes = await _database!.Table<DishModel>().Where(d => d.ShopId == shopId).ToListAsync();

            foreach (var dish in dishes)
            {
                var trans = await _database.Table<DishTranslationModel>()
                    .Where(t => t.DishId == dish.Id && t.LanguageCode == langCode)
                    .FirstOrDefaultAsync();

                if (trans != null)
                {
                    dish.Name = trans.Name;
                }
            }
            return dishes;
        }

        public async Task<int> AddShopAsync(ShopModel shop)
        {
            await Init();
            if (string.IsNullOrEmpty(shop.Id)) shop.Id = Guid.NewGuid().ToString();
            return await _database!.InsertAsync(shop);
        }

        public async Task<int> DeleteShopAsync(ShopModel shop)
        {
            await Init();
            return await _database!.DeleteAsync(shop);
        }

        // ═══════ IMAGE MANAGEMENT ═══════

        /// <summary>
        /// Tải tất cả ảnh và audio của Shops và Dishes từ server về cache cục bộ.
        /// Chỉ tải những asset chưa có trong cache.
        /// </summary>
        public async Task<bool> DownloadAllAssetsAsync(string apiUrl)
        {
            await Init();

            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                System.Diagnostics.Debug.WriteLine("DownloadAllImagesAsync: Không có kết nối mạng.");
                return false;
            }

            try
            {
                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

                var shops = await _database!.Table<ShopModel>().ToListAsync();
                foreach (var shop in shops)
                {
                    if (!string.IsNullOrEmpty(shop.ImageUrl) && shop.ImageUrl.StartsWith("/"))
                    {
                        await DownloadAndCacheFileAsync(httpClient, apiUrl, shop.ImageUrl);
                    }
                }

                // Tải audio files cho tất cả translation
                var shopTranslations = await _database.Table<ShopTranslationModel>().ToListAsync();
                foreach (var trans in shopTranslations)
                {
                    if (!string.IsNullOrEmpty(trans.AudioUrl) && trans.AudioUrl.StartsWith("/"))
                    {
                        await DownloadAndCacheFileAsync(httpClient, apiUrl, trans.AudioUrl);
                    }
                }

                var dishes = await _database.Table<DishModel>().ToListAsync();
                foreach (var dish in dishes)
                {
                    if (!string.IsNullOrEmpty(dish.ImageUrl) && dish.ImageUrl.StartsWith("/"))
                    {
                        await DownloadAndCacheFileAsync(httpClient, apiUrl, dish.ImageUrl);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DownloadAllImagesAsync Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Xóa tất cả file ảnh và audio đã cache trong AppDataDirectory.
        /// </summary>
        public Task<int> ClearImageCacheAsync()
        {
            int deletedCount = 0;
            var cachedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".mp3", ".wav", ".ogg" };
            var files = Directory.GetFiles(FileSystem.AppDataDirectory);

            foreach (var file in files)
            {
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (cachedExtensions.Contains(ext))
                {
                    File.Delete(file);
                    deletedCount++;
                }
            }

            System.Diagnostics.Debug.WriteLine($"ClearImageCacheAsync: Đã xóa {deletedCount} file cache.");
            return Task.FromResult(deletedCount);
        }
    }
}