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

            // Xóa bảng cũ 1 lần để đảm bảo chuẩn schema mới (Migration)
            if (!Preferences.Default.ContainsKey("DatabaseMigratedV2"))
            {
                await _database.DropTableAsync<ShopModel>();
                await _database.DropTableAsync<DishModel>();
                Preferences.Default.Set("DatabaseMigratedV2", true);
            }

            await _database.CreateTableAsync<ShopModel>();
            await _database.CreateTableAsync<DishModel>();
            await _database.CreateTableAsync<ShopTranslationModel>();
            await _database.CreateTableAsync<DishTranslationModel>();
        }

        private async Task<string> DownloadAndCacheImageAsync(HttpClient httpClient, string apiUrl, string relativeUrl)
        {
            if (string.IsNullOrEmpty(relativeUrl)) return relativeUrl;
            
            try
            {
                var fileName = Path.GetFileName(relativeUrl);
                var localPath = Path.Combine(FileSystem.AppDataDirectory, fileName);
                
                if (!File.Exists(localPath))
                {
                    var fullUrl = apiUrl.TrimEnd('/') + relativeUrl;
                    var imageBytes = await httpClient.GetByteArrayAsync(fullUrl);
                    await File.WriteAllBytesAsync(localPath, imageBytes);
                }
                return localPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Download image error: {ex.Message}");
                return relativeUrl;
            }
        }

        public async Task<bool> SyncDataFromApiAsync(string apiUrl)
        {
            await Init();
            try
            {
                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
                
                // 1. Fetch Shops
                var shopsResponse = await httpClient.GetAsync($"{apiUrl}/api/shops");
                if (shopsResponse.IsSuccessStatusCode)
                {
                    var shops = await shopsResponse.Content.ReadFromJsonAsync<List<ShopModel>>();
                    if (shops != null)
                    {
                        foreach (var shop in shops)
                        {
                            if (!string.IsNullOrEmpty(shop.ImageUrl) && shop.ImageUrl.StartsWith("/"))
                            {
                                shop.ImageUrl = await DownloadAndCacheImageAsync(httpClient, apiUrl, shop.ImageUrl);
                            }

                            await _database!.InsertOrReplaceAsync(shop);
                            if (shop.ShopTranslations != null)
                            {
                                // Xóa translation cũ của shop này để tránh duplicate (Upsert mechanism)
                                var oldTrans = await _database.Table<ShopTranslationModel>().Where(t => t.ShopId == shop.Id).ToListAsync();
                                foreach (var t in oldTrans) await _database.DeleteAsync(t);
                                
                                await _database.InsertAllAsync(shop.ShopTranslations);
                            }
                        }
                    }
                }

                // 2. Fetch Dishes
                var dishesResponse = await httpClient.GetAsync($"{apiUrl}/api/dishes");
                if (dishesResponse.IsSuccessStatusCode)
                {
                    var dishes = await dishesResponse.Content.ReadFromJsonAsync<List<DishModel>>();
                    if (dishes != null)
                    {
                        foreach (var dish in dishes)
                        {
                            if (!string.IsNullOrEmpty(dish.ImageUrl) && dish.ImageUrl.StartsWith("/"))
                            {
                                dish.ImageUrl = await DownloadAndCacheImageAsync(httpClient, apiUrl, dish.ImageUrl);
                            }

                            await _database!.InsertOrReplaceAsync(dish);
                            if (dish.DishTranslations != null)
                            {
                                var oldTrans = await _database.Table<DishTranslationModel>().Where(t => t.DishId == dish.Id).ToListAsync();
                                foreach (var t in oldTrans) await _database.DeleteAsync(t);

                                await _database.InsertAllAsync(dish.DishTranslations);
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
            // 1. Sync Data (Shops, Dishes, Images)
            bool dataSuccess = await SyncDataFromApiAsync(apiUrl);
            
            // 2. Sync All Languages
            try
            {
                await localizationService.PreloadAllLanguagesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Language Sync Error: {ex.Message}");
                // We might still consider it a partial success if data sync worked
            }

            return dataSuccess;
        }

        // --- CÁC HÀM GET & BINDING ---
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
    }
}