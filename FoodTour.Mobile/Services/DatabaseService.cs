    using SQLite;
    using FoodTour.Mobile.Models;
    using FoodTour.Mobile.Messages;
    using System.Net.Http.Json;
    using System.Text.Json;
    using CommunityToolkit.Mvvm.Messaging;

    namespace FoodTour.Mobile.Services
    {
        public class DatabaseService
        {
            private SQLiteAsyncConnection? _database;
            private readonly SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);
            private bool _isInitialized;

            // URL base của API server
            private string API_BASE_URL => AppConfig.ApiBaseUrl;

            async Task Init()
            {
                if (_isInitialized && _database is not null)
                    return;

                await _initLock.WaitAsync();
                try
                {
                    if (_isInitialized && _database is not null)
                        return;

                    var dbPath = Path.Combine(FileSystem.AppDataDirectory, "FoodTour.db3");
                    _database = new SQLiteAsyncConnection(dbPath);

                // Migration V3: Reset bảng cũ để đảm bảo chuẩn schema mới
                // (thêm Radius, Priority, CreatedAt, UpdatedAt, AudioUrl, IsAudioGenerated; đổi PK Translation)
                if (!Preferences.Default.ContainsKey("DatabaseMigratedV3"))
                {
                    await _database.DropTableAsync<ShopTranslationModel>();
                    await _database.DropTableAsync<ShopModel>();
                    Preferences.Default.Set("DatabaseMigratedV3", true);
                }

                await _database.CreateTableAsync<ShopModel>();
                await _database.CreateTableAsync<ShopTranslationModel>();

                // Migration V4: Thêm bảng NotificationModel cho hệ thống cập nhật qua thông báo
                await _database.CreateTableAsync<NotificationModel>();

                // Migration V5: Bảng LocalDevice lưu DeviceID bền vững (thay thế Preferences)
                //await _database.CreateTableAsync<LocalDeviceModel>();

                // Migration V6: Thêm bảng ShopItemModel cho tính năng Truyện nội bộ (Premium)
                await _database.CreateTableAsync<ShopItemModel>();
                await _database.CreateTableAsync<ShopItemTranslationModel>();

                _isInitialized = true;
            }
            finally
            {
                _initLock.Release();
            }
        }
        
            /// <summary>
            /// Đẩy DeviceId và DeviceName lên Backend (POST /api/device/sync).
            /// Nếu đã đồng bộ thành công rồi thì chỉ cập nhật LastActive.
            /// Không throw — lỗi mạng được log im lặng để không ảnh hưởng startup.
            /// Trả về true nếu thiết bị đang bị Khóa (Blocked), ngược lại false.
            /// </summary>
            public async Task<bool> SyncDeviceToServerAsync(string deviceId, string deviceName)
            {
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    System.Diagnostics.Debug.WriteLine("[DeviceSync] Không có mạng, bỏ qua sync.");
                    return false;
                }

                try
                {
                    using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

                    var payload = new
                    {
                        DeviceId   = deviceId,
                        DeviceName = deviceName,
                        Platform   = DeviceInfo.Platform.ToString()
                    };

                    var url = $"{API_BASE_URL}/api/device/sync";
                    System.Diagnostics.Debug.WriteLine($"[DeviceSync] Calling URL: {url}");

                    var response = await httpClient.PostAsJsonAsync(url, payload);

                    if (response.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DeviceSync] Đồng bộ thành công: {deviceId}");
                        return true;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[DeviceSync] Server trả về {response.StatusCode}");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    // Offline hoặc server chưa khởi — im lặng, không crash app
                    System.Diagnostics.Debug.WriteLine($"[DeviceSync] Lỗi: {ex.Message}");
                    return false;
                }
            }

            // ═══════ IMAGE & AUDIO CACHING ═══════

            private async Task<string> DownloadAndCacheFileAsync(HttpClient httpClient, string apiUrl, string relativeUrl)
            {
                if (string.IsNullOrEmpty(relativeUrl)) return relativeUrl;
                
                try
                {
                    var fileName = Path.GetFileName(new Uri(relativeUrl).LocalPath);
                    var localPath = Path.Combine(FileSystem.AppDataDirectory, fileName);
                    
                    if (!File.Exists(localPath))
                    {
                        var fullUrl = relativeUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) 
                            ? relativeUrl 
                            : apiUrl.TrimEnd('/') + (relativeUrl.StartsWith("/") ? relativeUrl : "/" + relativeUrl);
                            
                        // Nếu đã chuyển sang dùng fallback localhost
                        if (AppConfig.UseLocalFallback && fullUrl.Contains("onrender.com"))
                        {
                            var cloudUrl = "https://foodtour-admin-api.onrender.com";
                            fullUrl = fullUrl.Replace(cloudUrl, AppConfig.ApiBaseUrl);
                        }

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
                    bool hasChanges = false;
                    int updatedShopCount = 0;
                    DateTime? maxUpdatedAt = null;

                    var shopsResponse = await SendWithRetryAsync(httpClient, $"{apiUrl}/api/shops");
                    if (shopsResponse.IsSuccessStatusCode)
                    {
                        var shops = await shopsResponse.Content.ReadFromJsonAsync<List<ShopModel>>();
                        if (shops != null && shops.Count > 0)
                        {
                            maxUpdatedAt = shops.Max(s => s.UpdatedAt);
                            // Pre-fetch dữ liệu cũ để so sánh UpdatedAt và dọn cache
                            var existingShops = await _database!.Table<ShopModel>().ToListAsync();
                            var existingShopDict = existingShops.ToDictionary(s => s.Id);
                            // Lưu danh sách Shop ID đã thay đổi để dọn cache và broadcast event
                            var modifiedShopIds = new List<string>();

                            // Lấy translation cũ để so sánh AudioUrl — dùng cho cache busting
                            var existingTranslations = await _database.Table<ShopTranslationModel>().ToListAsync();
                            var existingTransDict = existingTranslations
                                .GroupBy(t => t.ShopId)
                                .ToDictionary(g => g.Key, g => g.ToList());

                            // Lấy shop item translation cũ để xoa cache busting
                            var existingItemTranslations = await _database.Table<ShopItemTranslationModel>().ToListAsync();
                            var existingItemTransList = existingItemTranslations.ToList();

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
                                            hasChanges = true;
                                            updatedShopCount++;
                                            modifiedShopIds.Add(shop.Id);
                                        }
                                    }
                                    else
                                    {
                                        db.Insert(shop);
                                        hasChanges = true;
                                        updatedShopCount++;
                                        modifiedShopIds.Add(shop.Id);
                                    }
                                }
                            });
                            
                            // ──── 2. Dọn dẹp Shop bị xóa/deactivated ────
                            // Nếu shop không có trong danh sách active từ server, ta xóa khỏi máy khách
                            var serverShopIds = shops.Select(s => s.Id).ToList();
                            var shopsToDelete = existingShops.Where(s => !serverShopIds.Contains(s.Id)).ToList();
                            if (shopsToDelete.Count > 0)
                            {
                                await _database.RunInTransactionAsync(db =>
                                {
                                    foreach (var shop in shopsToDelete)
                                    {
                                        db.Delete(shop);
                                        // Xóa triệt để các bản dịch và item liên quan để tránh rác DB
                                        db.Execute("DELETE FROM ShopTranslationModel WHERE ShopId = ?", shop.Id);
                                        db.Execute("DELETE FROM ShopItemTranslationModel WHERE ShopItemId IN (SELECT Id FROM ShopItemModel WHERE ShopId = ?)", shop.Id);
                                        db.Execute("DELETE FROM ShopItemModel WHERE ShopId = ?", shop.Id);
                                        hasChanges = true;
                                        updatedShopCount++; // Đếm cả lượt xóa để trigger notification
                                    }
                                });
                                System.Diagnostics.Debug.WriteLine($"[SyncData] Đã xóa {shopsToDelete.Count} shop không còn active từ server.");
                            }

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
                                    
                                    if (shop.ShopItems != null)
                                    {
                                        foreach (var item in shop.ShopItems)
                                        {
                                            db.InsertOrReplace(item);
                                            if (item.ShopItemTranslations != null)
                                            {
                                                foreach (var itemTrans in item.ShopItemTranslations)
                                                {
                                                    db.InsertOrReplace(itemTrans);
                                                }
                                            }
                                        }
                                    }
                                }
                            });

                            // ──── Cache Busting: Dọn file media cũ trước khi tải mới ────
                            // Chỉ xóa cache cho các shop ĐÃ THAY ĐỔI (tránh xóa nhầm cache đang dùng)
                            foreach (var shop in shops.Where(s => modifiedShopIds.Contains(s.Id)))
                            {
                                // Xóa ảnh shop cũ nếu URL thay đổi
                                if (!string.IsNullOrEmpty(shop.ImageUrl))
                                {
                                    DeleteOldCachedFile(shop.ImageUrl);
                                }

                                // Xóa audio cũ của shop này theo từng ngôn ngữ
                                if (existingTransDict.TryGetValue(shop.Id, out var oldTransList))
                                {
                                    foreach (var oldTrans in oldTransList)
                                    {
                                        if (!string.IsNullOrEmpty(oldTrans.AudioUrl))
                                        {
                                            DeleteOldCachedFile(oldTrans.AudioUrl);
                                        }
                                    }
                                }

                                // Xóa audio cũ của ShopItem
                                if (shop.ShopItems != null)
                                {
                                    foreach (var item in shop.ShopItems)
                                    {
                                        var oldItemTransList = existingItemTransList.Where(t => t.ShopItemId == item.Id).ToList();
                                        foreach (var oldTrans in oldItemTransList)
                                        {
                                            if (!string.IsNullOrEmpty(oldTrans.AudioUrl))
                                            {
                                                DeleteOldCachedFile(oldTrans.AudioUrl);
                                            }
                                        }
                                    }
                                }
                            }

                            // Tải ảnh và audio MỚI về cache (ngoài transaction, vì là I/O network)
                            foreach (var shop in shops)
                            {
                                if (!string.IsNullOrEmpty(shop.ImageUrl))
                                {
                                    await DownloadAndCacheFileAsync(httpClient, apiUrl, shop.ImageUrl);
                                }
                                if (shop.ShopTranslations != null)
                                {
                                    foreach (var trans in shop.ShopTranslations)
                                    {
                                        if (!string.IsNullOrEmpty(trans.AudioUrl))
                                        {
                                            await DownloadAndCacheFileAsync(httpClient, apiUrl, trans.AudioUrl);
                                        }
                                    }
                                }
                                if (shop.ShopItems != null)
                                {
                                    foreach (var item in shop.ShopItems)
                                    {
                                        if (item.ShopItemTranslations != null)
                                        {
                                            foreach (var itemTrans in item.ShopItemTranslations)
                                            {
                                                if (!string.IsNullOrEmpty(itemTrans.AudioUrl))
                                                {
                                                    await DownloadAndCacheFileAsync(httpClient, apiUrl, itemTrans.AudioUrl);
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            // ──── Broadcast event: thông báo file audio mới đã sẵn sàng ────
                            // WalkingSimulationService sẽ reload _shops, AudioPlayerService sẽ hot-reload player
                            if (modifiedShopIds.Count > 0)
                            {
                                WeakReferenceMessenger.Default.Send(new AudioFilesUpdatedMessage(modifiedShopIds));
                                System.Diagnostics.Debug.WriteLine($"[SyncData] Broadcast AudioFilesUpdatedMessage cho {modifiedShopIds.Count} shop đã thay đổi.");
                            }
                        }
                    }


                    // Lưu thời điểm đồng bộ thành công để CheckForUpdatesAsync không tạo notification trùng
                    if (hasChanges && updatedShopCount > 0)
                    {
                        var notification = new NotificationModel
                        {
                            Title = "Notify_SyncComplete", 
                            Description = updatedShopCount.ToString(), // Số lượng shop cập nhật
                            Type = "DataUpdate",
                            TotalSize = 0, // Dữ liệu đã tự động tải xong
                            IsDownloaded = true,
                            Status = "Updated", // Trạng thái đã cập nhật
                            CreatedAt = DateTime.UtcNow,
                            UpdatedShopIdsJson = "[]" // Fake rỗng vì đã up xong
                        };
                        await _database!.InsertAsync(notification);
                        System.Diagnostics.Debug.WriteLine($"[SyncData] Tạo History Notification cho {updatedShopCount} cập nhật ngầm.");
                    }

                    // Dùng Max UpdatedAt của server để tránh lệch múi giờ (clock drift) giữa client và server
                    if (maxUpdatedAt.HasValue)
                    {
                        var lastSyncStr = Preferences.Default.Get("LastSyncTime", string.Empty);
                        bool shouldUpdate = true;
                        if (!string.IsNullOrEmpty(lastSyncStr) && DateTime.TryParse(lastSyncStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var currentLastSync))
                        {
                            if (currentLastSync >= maxUpdatedAt.Value) 
                                shouldUpdate = false;
                        }

                        if (shouldUpdate)
                        {
                            Preferences.Default.Set("LastSyncTime", maxUpdatedAt.Value.ToString("O"));
                            System.Diagnostics.Debug.WriteLine($"[SyncData] Đã lưu LastSyncTime: {maxUpdatedAt.Value:O}");
                        }
                    }

                    return hasChanges;
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

            private string? GetLocalPathIfExists(string? url)
            {
                if (string.IsNullOrEmpty(url)) return url;
                try
                {
                    var fileName = Path.GetFileName(new Uri(url).LocalPath);
                    var localPath = Path.Combine(FileSystem.AppDataDirectory, fileName);
                    if (File.Exists(localPath)) return localPath;
                }
                catch { }
                return url;
            }

            public async Task<List<ShopModel>> GetShopsAsync()
            {
                await Init();
                var langCode = Preferences.Default.Get("AppLanguage", "vi");
                var shops = await _database!.Table<ShopModel>().ToListAsync();

                foreach (var shop in shops)
                {
                    shop.ImageUrl = GetLocalPathIfExists(shop.ImageUrl) ?? shop.ImageUrl;

                    var trans = await _database.Table<ShopTranslationModel>()
                        .Where(t => t.ShopId == shop.Id && t.LanguageCode == langCode)
                        .FirstOrDefaultAsync();

                    if (trans != null)
                    {
                        shop.Name = trans.Name;
                        shop.Address = trans.Address;
                        shop.Description = trans.Description;
                        shop.AudioUrl = GetLocalPathIfExists(trans.AudioUrl) ?? trans.AudioUrl;
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
                    shop.ImageUrl = GetLocalPathIfExists(shop.ImageUrl) ?? shop.ImageUrl;

                    var trans = await _database.Table<ShopTranslationModel>()
                        .Where(t => t.ShopId == shop.Id && t.LanguageCode == langCode)
                        .FirstOrDefaultAsync();

                    if (trans != null)
                    {
                        shop.Name = trans.Name;
                        shop.Address = trans.Address;
                        shop.Description = trans.Description;
                        shop.AudioUrl = GetLocalPathIfExists(trans.AudioUrl) ?? trans.AudioUrl;
                    }
                    
                    var items = await _database.Table<ShopItemModel>().Where(i => i.ShopId == shop.Id).ToListAsync();
                    foreach (var item in items)
                    {
                        var itemTrans = await _database.Table<ShopItemTranslationModel>()
                            .Where(t => t.ShopItemId == item.Id && t.LanguageCode == langCode)
                            .FirstOrDefaultAsync();

                        if (itemTrans != null)
                        {
                            item.Title = itemTrans.Title;
                            item.Description = itemTrans.Description;
                            item.AudioUrl = GetLocalPathIfExists(itemTrans.AudioUrl) ?? itemTrans.AudioUrl;
                        }
                    }
                    shop.ShopItems = items ?? new List<ShopItemModel>();
                }
                return shop;
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
                        if (!string.IsNullOrEmpty(shop.ImageUrl))
                        {
                            await DownloadAndCacheFileAsync(httpClient, apiUrl, shop.ImageUrl);
                        }
                    }

                    // Tải audio files cho tất cả translation
                    var shopTranslations = await _database.Table<ShopTranslationModel>().ToListAsync();
                    foreach (var trans in shopTranslations)
                    {
                        if (!string.IsNullOrEmpty(trans.AudioUrl))
                        {
                            await DownloadAndCacheFileAsync(httpClient, apiUrl, trans.AudioUrl);
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

            // ═══════ NOTIFICATION-BASED UPDATE SYSTEM ═══════

            /// <summary>
            /// Gửi HTTP request với cơ chế retry cho trường hợp server Render đang ngủ (cold start).
            /// Thử lại tối đa 3 lần với exponential backoff: 2s → 4s → 8s.
            /// </summary>
            private async Task<HttpResponseMessage> SendWithRetryAsync(HttpClient client, string url, int maxRetries = 3)
            {
                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    // Nếu đã đổi sang fallback proxy, thì cập nhật URL cho chính xác
                    if (AppConfig.UseLocalFallback && url.Contains("onrender.com"))
                    {
                        var cloudUrl = "https://foodtour-admin-api.onrender.com";
                        url = url.Replace(cloudUrl, AppConfig.ApiBaseUrl);
                    }

                    try
                    {
                        var response = await client.GetAsync(url);
                        // Nếu server trả về 5xx (như 521: web server is down), fallback hoặc retry
                        if ((int)response.StatusCode >= 500)
                        {
                            if (!AppConfig.IsLocalEnvironment && !AppConfig.UseLocalFallback)
                            {
                                System.Diagnostics.Debug.WriteLine($"[Cloud Down {(int)response.StatusCode}] Fallback sang Localhost...");
                                AppConfig.UseLocalFallback = true;
                                var cloudUrl = "https://foodtour-admin-api.onrender.com";
                                url = url.Replace(cloudUrl, AppConfig.ApiBaseUrl);
                                
                                response = await client.GetAsync(url);
                                if (response.IsSuccessStatusCode)
                                    return response;
                            }

                            if (attempt < maxRetries)
                            {
                                var delay = (int)Math.Pow(2, attempt) * 1000; // 2s, 4s, 8s
                                System.Diagnostics.Debug.WriteLine($"[Retry] Server trả về {response.StatusCode}, thử lại sau {delay}ms (lần {attempt}/{maxRetries})");
                                await Task.Delay(delay);
                                continue;
                            }
                        }
                        return response;
                    }
                    catch (Exception ex)
                    {
                        // Lỗi kết nối tạm thời (transient failure) hoặc server sập
                        if (!AppConfig.IsLocalEnvironment && !AppConfig.UseLocalFallback)
                        {
                            System.Diagnostics.Debug.WriteLine($"[Cloud Error {ex.Message}] Fallback sang Localhost...");
                            AppConfig.UseLocalFallback = true;
                            var cloudUrl = "https://foodtour-admin-api.onrender.com";
                            url = url.Replace(cloudUrl, AppConfig.ApiBaseUrl);
                            
                            try
                            {
                                var responseLocal = await client.GetAsync(url);
                                if (responseLocal.IsSuccessStatusCode)
                                    return responseLocal;
                            }
                            catch { }
                        }

                        if (attempt < maxRetries)
                        {
                            var delay = (int)Math.Pow(2, attempt) * 1000;
                            System.Diagnostics.Debug.WriteLine($"[Retry] Lỗi: {ex.Message}, thử lại sau {delay}ms (lần {attempt}/{maxRetries})");
                            await Task.Delay(delay);
                        }
                        else if (attempt == maxRetries)
                        {
                            // Tới lần cuối mà vẫn lỗi thì ném
                            throw;
                        }
                    }
                }
                return await client.GetAsync(url);
            }

            /// <summary>
            /// Kiểm tra xem server có bản cập nhật mới không.
            /// Gửi LastSyncTime lên API, nếu có thay đổi thì tạo bản ghi NotificationModel trong SQLite.
            /// </summary>
            public async Task<bool> CheckForUpdatesAsync()
            {
                await Init();

                // Chỉ kiểm tra khi có mạng
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    System.Diagnostics.Debug.WriteLine("[CheckForUpdates] Không có mạng, bỏ qua.");
                    return false;
                }

                try
                {
                    using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

                    // Lấy thời điểm đồng bộ lần cuối, mặc định là DateTime.MinValue nếu chưa bao giờ sync
                    var lastSyncStr = Preferences.Default.Get("LastSyncTime", string.Empty);
                    var sinceParam = string.IsNullOrEmpty(lastSyncStr) ? "" : $"?since={Uri.EscapeDataString(lastSyncStr)}";

                    var response = await SendWithRetryAsync(httpClient, $"{API_BASE_URL}/api/shops/updates{sinceParam}");

                    if (!response.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CheckForUpdates] API trả về {response.StatusCode}");
                        return false;
                    }

                    // Parse JSON response từ API
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    bool hasUpdates = root.GetProperty("hasUpdates").GetBoolean();
                    if (!hasUpdates)
                    {
                        System.Diagnostics.Debug.WriteLine("[CheckForUpdates] Không có bản cập nhật mới.");
                        return false;
                    }

                    // Lấy danh sách shop IDs được cập nhật
                    var updatedShopIds = new List<string>();
                    foreach (var id in root.GetProperty("updatedShopIds").EnumerateArray())
                    {
                        updatedShopIds.Add(id.GetString() ?? "");
                    }

                    long totalEstimatedSize = root.GetProperty("totalEstimatedSize").GetInt64();

                    // Dùng trực tiếp dung lượng ước tính từ server API để tránh tốn data 4G
                    // (không gọi HEAD request cho từng file nữa)
                    long finalSize = totalEstimatedSize;

                    // Kiểm tra xem đã có notification cho batch shopIds này chưa (tránh tạo trùng)
                    var shopIdsJson = JsonSerializer.Serialize(updatedShopIds);
                    var existingNotifications = await _database!.Table<NotificationModel>()
                        .Where(n => n.Status == "Available" || n.Status == "Error")
                        .ToListAsync();

                    // Nếu đã có notification với cùng shopIds và chưa tải, không tạo lại, NHƯNG vẫn nhắc UI
                    if (existingNotifications.Any(n => n.UpdatedShopIdsJson == shopIdsJson))
                    {
                        System.Diagnostics.Debug.WriteLine("[CheckForUpdates] Notification đã tồn tại, gửi lại trigger.");
                        return true;
                    }

                    // Tạo bản ghi thông báo mới trong SQLite
                    var notification = new NotificationModel
                    {
                        Title = "Notify_UpdateAvailable", // Key localization, resolve ở UI
                        Description = updatedShopIds.Count.ToString(), // Số quán ăn, format ở UI
                        Type = "DataUpdate",
                        TotalSize = finalSize,
                        IsDownloaded = false,
                        Status = "Available",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedShopIdsJson = shopIdsJson
                    };

                    await _database.InsertAsync(notification);
                    System.Diagnostics.Debug.WriteLine($"[CheckForUpdates] Tạo notification mới: {updatedShopIds.Count} shops, ~{finalSize / 1024}KB");
                    return true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CheckForUpdates] Lỗi: {ex.Message}");
                    return false;
                }
            }

            /// <summary>
            /// Lấy dung lượng file từ URL bằng HEAD request.
            /// Trả về 0 nếu không lấy được.
            /// </summary>
            private async Task<long> GetFileSizeAsync(HttpClient httpClient, string url)
            {
                try
                {
                    var fullUrl = url.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                        ? url
                        : API_BASE_URL.TrimEnd('/') + (url.StartsWith("/") ? url : "/" + url);

                    using var request = new HttpRequestMessage(HttpMethod.Head, fullUrl);
                    var response = await httpClient.SendAsync(request);
                    return response.Content.Headers.ContentLength ?? 0;
                }
                catch
                {
                    return 0;
                }
            }

            /// <summary>
            /// Xóa toàn bộ lịch sử thông báo (Clear Logs)
            /// </summary>
            public async Task ClearAllNotificationsAsync()
            {
                await Init();
                await _database!.DeleteAllAsync<NotificationModel>();
            }

            /// <summary>
            /// Thực hiện tải bản cập nhật cho một notification cụ thể.
            /// 1. Cập nhật text (SQLite) âm thầm
            /// 2. Xóa file media cũ rồi tải file mới
            /// 3. Cập nhật trạng thái notification
            /// </summary>
            public async Task<bool> DownloadUpdateAsync(NotificationModel notification)
            {
                await Init();

                // Chỉ tải media khi có mạng
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    System.Diagnostics.Debug.WriteLine("[DownloadUpdate] Không có mạng.");
                    return false;
                }

                try
                {
                    // Đánh dấu đang tải
                    notification.Status = "Downloading";
                    await _database!.UpdateAsync(notification);

                    using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

                    // Parse danh sách shop IDs cần cập nhật
                    var shopIds = JsonSerializer.Deserialize<List<string>>(notification.UpdatedShopIdsJson) ?? new List<string>();

                    // ──── 1. Cập nhật dữ liệu Text (Shops + Translations) ────
                    List<ShopModel>? targetShops = null;
                    var shopsResponse = await SendWithRetryAsync(httpClient, $"{API_BASE_URL}/api/shops");
                    if (shopsResponse.IsSuccessStatusCode)
                    {
                        var allShops = await shopsResponse.Content.ReadFromJsonAsync<List<ShopModel>>();
                        if (allShops != null)
                        {
                            // Chỉ cập nhật các shop nằm trong danh sách
                            targetShops = allShops.Where(s => shopIds.Contains(s.Id)).ToList();

                            // ──── 2. Dọn dẹp Shop bị deactivated ────
                            // Nếu ID có trong thông báo update nhưng không có trong list API active -> nghĩa là đã bị ẩn
                            var targetShopIds = targetShops.Select(s => s.Id).ToHashSet();
                            var deletedShopIds = shopIds.Where(id => !targetShopIds.Contains(id)).ToList();
                            if (deletedShopIds.Count > 0)
                            {
                                await _database.RunInTransactionAsync(db =>
                                {
                                    foreach (var id in deletedShopIds)
                                    {
                                        db.Execute("DELETE FROM ShopModel WHERE Id = ?", id);
                                        db.Execute("DELETE FROM ShopTranslationModel WHERE ShopId = ?", id);
                                        db.Execute("DELETE FROM ShopItemTranslationModel WHERE ShopItemId IN (SELECT Id FROM ShopItemModel WHERE ShopId = ?)", id);
                                        db.Execute("DELETE FROM ShopItemModel WHERE ShopId = ?", id);
                                    }
                                });
                                System.Diagnostics.Debug.WriteLine($"[DownloadUpdate] Đã xóa {deletedShopIds.Count} shop không còn active.");
                            }

                            // Upsert shops
                            await _database.RunInTransactionAsync(db =>
                            {
                                foreach (var shop in targetShops)
                                {
                                    db.InsertOrReplace(shop);
                                }
                            });

                            // Upsert translations
                            await _database.RunInTransactionAsync(db =>
                            {
                                foreach (var shop in targetShops)
                                {
                                    if (shop.ShopTranslations != null)
                                    {
                                        foreach (var trans in shop.ShopTranslations)
                                        {
                                            db.InsertOrReplace(trans);
                                        }
                                    }
                                    if (shop.ShopItems != null)
                                    {
                                        foreach (var item in shop.ShopItems)
                                        {
                                            db.InsertOrReplace(item);
                                            if (item.ShopItemTranslations != null)
                                            {
                                                foreach (var itemTrans in item.ShopItemTranslations)
                                                {
                                                    db.InsertOrReplace(itemTrans);
                                                }
                                            }
                                        }
                                    }
                                }
                            });

                            // ──── 2. Tải Media (xóa file cũ trước, tải mới) ────
                            foreach (var shop in targetShops)
                            {
                                // Xóa và tải lại ảnh shop
                                if (!string.IsNullOrEmpty(shop.ImageUrl))
                                {
                                    DeleteOldCachedFile(shop.ImageUrl);
                                    await DownloadAndCacheFileAsync(httpClient, API_BASE_URL, shop.ImageUrl);
                                }

                                // Xóa và tải lại audio files
                                if (shop.ShopTranslations != null)
                                {
                                    foreach (var trans in shop.ShopTranslations)
                                    {
                                        if (!string.IsNullOrEmpty(trans.AudioUrl))
                                        {
                                            DeleteOldCachedFile(trans.AudioUrl);
                                            await DownloadAndCacheFileAsync(httpClient, API_BASE_URL, trans.AudioUrl);
                                        }
                                    }
                                }

                                if (shop.ShopItems != null)
                                {
                                    foreach (var item in shop.ShopItems)
                                    {
                                        if (item.ShopItemTranslations != null)
                                        {
                                            foreach (var itemTrans in item.ShopItemTranslations)
                                            {
                                                if (!string.IsNullOrEmpty(itemTrans.AudioUrl))
                                                {
                                                    DeleteOldCachedFile(itemTrans.AudioUrl);
                                                    await DownloadAndCacheFileAsync(httpClient, API_BASE_URL, itemTrans.AudioUrl);
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            // ──── Broadcast cho Audio Player biết file mới đã sẵn sàng ────
                            // WalkingSimulationService sẽ nhận message này và reload audio đang phát
                            // mà không cần khởi động lại ứng dụng.
                            WeakReferenceMessenger.Default.Send(new AudioFilesUpdatedMessage(shopIds));
                            System.Diagnostics.Debug.WriteLine($"[DownloadUpdate] Đã broadcast AudioFilesUpdatedMessage cho {shopIds.Count} shop.");
                        }
                    }

                    // ──── 4. Cập nhật trạng thái và LastSyncTime ────
                    notification.Status = "Updated";
                    notification.IsDownloaded = true;
                    await _database.UpdateAsync(notification);

                    // Lưu thời điểm đồng bộ thành công (Tránh clock drift giữ client và server)
                    if (targetShops != null && targetShops.Any())
                    {
                        var maxUpdatedAt = targetShops.Max(s => s.UpdatedAt);
                        
                        var lastSyncStr = Preferences.Default.Get("LastSyncTime", string.Empty);
                        bool shouldUpdate = true;
                        if (!string.IsNullOrEmpty(lastSyncStr) && DateTime.TryParse(lastSyncStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var currentLastSync))
                        {
                            if (currentLastSync >= maxUpdatedAt) 
                                shouldUpdate = false; // Thuật toán: không kéo ngược LastSyncTime
                        }
                        
                        if (shouldUpdate)
                        {
                            Preferences.Default.Set("LastSyncTime", maxUpdatedAt.ToString("O"));
                        }
                    }
                    else
                    {
                        Preferences.Default.Set("LastSyncTime", DateTime.UtcNow.ToString("O"));
                    }

                    System.Diagnostics.Debug.WriteLine($"[DownloadUpdate] Hoàn tất cập nhật {shopIds.Count} shops.");
                    return true;
                }
                catch (Exception ex)
                {
                    // Đánh dấu lỗi để người dùng có thể thử lại
                    notification.Status = "Error";
                    await _database!.UpdateAsync(notification);

                    System.Diagnostics.Debug.WriteLine($"[DownloadUpdate] Lỗi: {ex.Message}");
                    return false;
                }
            }

            /// <summary>
            /// Xóa file media cũ đã cache trong AppDataDirectory trước khi tải file mới.
            /// Tránh rác bộ nhớ khi file trên server thay đổi.
            /// </summary>
            private void DeleteOldCachedFile(string url)
            {
                try
                {
                    var fileName = Path.GetFileName(new Uri(url, UriKind.RelativeOrAbsolute).IsAbsoluteUri
                        ? new Uri(url).LocalPath
                        : url);
                    var localPath = Path.Combine(FileSystem.AppDataDirectory, fileName);
                    if (File.Exists(localPath))
                    {
                        File.Delete(localPath);
                        System.Diagnostics.Debug.WriteLine($"[DeleteOldCache] Đã xóa: {fileName}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DeleteOldCache] Lỗi xóa file: {ex.Message}");
                }
            }

            /// <summary>
            /// Lấy danh sách tất cả thông báo từ SQLite, sắp xếp theo thời gian mới nhất.
            /// </summary>
            public async Task<List<NotificationModel>> GetNotificationsAsync()
            {
                await Init();
                return await _database!.Table<NotificationModel>()
                    .OrderByDescending(n => n.CreatedAt)
                    .ToListAsync();
            }

            /// <summary>
            /// Cập nhật trạng thái của một notification trong SQLite.
            /// </summary>
            public async Task UpdateNotificationAsync(NotificationModel notification)
            {
                await Init();
                await _database!.UpdateAsync(notification);
            }

            // ═══════ DEEP LINK — DEVICE STATUS & TRIAL ═══════

            /// <summary>
            /// Gọi API kiểm tra trạng thái Premium và số lần trial còn lại của thiết bị.
            /// Dùng cho luồng Deep Link: kiểm tra quyền trước khi phát audio.
            /// </summary>
            public async Task<DeviceStatusResult?> CheckDeviceStatusAsync(string hardwareId)
            {
                if (string.IsNullOrEmpty(hardwareId))
                    return null;

                try
                {
                    using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                    var url = $"{API_BASE_URL}/api/device/status/{Uri.EscapeDataString(hardwareId)}";

                    System.Diagnostics.Debug.WriteLine($"[DeviceStatus] Đang gọi: {url}");
                    var response = await httpClient.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadFromJsonAsync<DeviceStatusResult>();
                        System.Diagnostics.Debug.WriteLine($"[DeviceStatus] Premium: {result?.IsPremium}, TrialRemaining: {result?.TrialRemaining}");
                        return result;
                    }

                    System.Diagnostics.Debug.WriteLine($"[DeviceStatus] Server trả về: {response.StatusCode}");
                    return null;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DeviceStatus] Lỗi: {ex.Message}");
                    return null;
                }
            }

            /// <summary>
            /// Ghi log một lượt nghe (Trial/Analytics) cho thiết bị qua API.
            /// triggerType: 0 = Web, 1 = AppScan, 2 = AppAuto.
            /// Giá trị triggerType được truyền qua QUERY PARAMETER (?type=N),
            /// KHÔNG qua JSON body — tránh hoàn toàn lỗi enum deserialization.
            /// </summary>
            public async Task<TrialResult?> RecordTrialAsync(string hardwareId, string shopId, int triggerType = 1)
            {
                if (string.IsNullOrEmpty(hardwareId))
                    return null;

                try
                {
                    using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

                    // ── triggerType truyền qua query string, body chỉ chứa DeviceId + ShopId ──
                    var url = $"{API_BASE_URL}/api/device/trial?type={triggerType}";

                    var response = await httpClient.PostAsJsonAsync(url, new
                    {
                        DeviceId = hardwareId,
                        ShopId = shopId
                    });

                    System.Diagnostics.Debug.WriteLine($"[Trial] POST {url} → {response.StatusCode}");

                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadFromJsonAsync<TrialResult>();
                        System.Diagnostics.Debug.WriteLine($"[Trial] Allowed={result?.Allowed}, Remaining={result?.Remaining}");
                        return result;
                    }

                    System.Diagnostics.Debug.WriteLine($"[Trial] Server error: {response.StatusCode}");
                    return null;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Trial] Exception: {ex.Message}");
                    return null;
                }
            }

            /// <summary>
            /// Ghi log lượt nghe audio vào bảng AudioActivityLogs trên server.
            /// Fire-and-forget — không chặn luồng phát audio.
            /// source: "Web", "AppManual", "AppAuto"
            /// </summary>
            public async Task RecordAudioLogAsync(string deviceId, string shopId, string languageCode, string source, Guid? shopItemId = null)
            {
                if (string.IsNullOrEmpty(deviceId) || string.IsNullOrEmpty(shopId))
                    return;

                try
                {
                    using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                    var url = $"{API_BASE_URL}/api/audiologs/record";

                    var body = new
                    {
                        DeviceId = deviceId,
                        ShopId = shopId,
                        ShopItemId = shopItemId,
                        LanguageCode = languageCode,
                        Source = source
                    };

                    var response = await httpClient.PostAsJsonAsync(url, body);
                    System.Diagnostics.Debug.WriteLine($"[AudioLog] POST {url} → {response.StatusCode} (source={source})");
                }
                catch (Exception ex)
                {
                    // Im lặng — không được làm gián đoạn trải nghiệm người dùng
                    System.Diagnostics.Debug.WriteLine($"[AudioLog] Exception: {ex.Message}");
                }
            }
        }

        // ═══════ DTO cho Deep Link API Response ═══════

        /// <summary>Kết quả kiểm tra trạng thái thiết bị từ API /api/device/status.</summary>
        public class DeviceStatusResult
        {
            public bool IsPremium { get; set; }
            public DateTime? PremiumExpiry { get; set; }
            public int TrialCount { get; set; }
            public int MaxTrial { get; set; }
            public int TrialRemaining { get; set; }
        }

        /// <summary>Kết quả ghi trial từ API /api/device/trial.</summary>
        public class TrialResult
        {
            public bool Allowed { get; set; }
            public int Remaining { get; set; }
            public string? Reason { get; set; }
        }
    }
