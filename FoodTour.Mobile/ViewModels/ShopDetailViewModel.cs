using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using FoodTour.Mobile.Messages;
using FoodTour.Mobile.Models;
using FoodTour.Mobile.Services;
using CommunityToolkit.Mvvm.Messaging;

namespace FoodTour.Mobile.ViewModels
{
    // FIX: Xóa bỏ [QueryProperty] — chuyển sang IQueryAttributable để bọc try-catch,
    // tránh Exception ngầm bị nuốt ở tầng SDK khiến UI kẹt trắng.
    public partial class ShopDetailViewModel : BaseViewModel, IRecipient<LanguageChangedMessage>, IDisposable, IQueryAttributable
    {
        private readonly DatabaseService _dbService;
        private readonly IAudioPlayerService _audioService;
#if ANDROID
        private readonly IHardwareIdService _hardwareIdService;
#endif
        private ShopModel? shop;

        public ShopModel? Shop
        {
            get => shop;
            set
            {
                if (SetProperty(ref shop, value) && value != null)
                {
                    ShopItems.Clear();
                    if (value.ShopItems != null)
                    {
                        foreach (var item in value.ShopItems)
                        {
                            ShopItems.Add(item);
                        }
                    }
                    OnPropertyChanged(nameof(HasShopItems));
                    RefreshShopDataAsync(value.Id);
                }
            }
        }

        public ObservableCollection<ShopItemModel> ShopItems { get; } = new ObservableCollection<ShopItemModel>();

        public bool HasShopItems => ShopItems.Count > 0;

        // ── Thuộc tính Deep Link: nhận từ Shell Navigation ──

        /// <summary>ID quán — dùng khi Deep Link truyền shopId thay vì object ShopData.</summary>
        [ObservableProperty]
        private string? shopId;

        /// <summary>Cờ đánh dấu mở từ Deep Link (QR scan).</summary>
        [ObservableProperty]
        private bool isFromDeepLink;

        /// <summary>Trạng thái Premium của thiết bị.</summary>
        [ObservableProperty]
        private bool isPremium;

        /// <summary>Số lượt nghe thử còn lại trong 24h.</summary>
        [ObservableProperty]
        private int trialRemaining = 3;

        /// <summary>Hardware ID thiết bị — dùng để ghi trial log.</summary>
        [ObservableProperty]
        private string? hardwareId;

        /// <summary>Trạng thái Premium chung (kiểm tra local/chủ động truy vấn) để mở khóa giao diện Premium.</summary>
        [ObservableProperty]
        private bool isDevicePremium;

        // ── Thuộc tính giao diện Trial/Premium ──

        /// <summary>Hiển thị banner thông báo trial/premium.</summary>
        [ObservableProperty]
        private bool showTrialBanner;

        /// <summary>Nội dung banner thông báo.</summary>
        [ObservableProperty]
        private string trialBannerText = string.Empty;

        /// <summary>Hiển thị nút nâng cấp Premium.</summary>
        [ObservableProperty]
        private bool showUpgradeButton;

        /// <summary>Đang trong quá trình tải/phát audio từ Deep Link.</summary>
        [ObservableProperty]
        private bool isDeepLinkLoading;

#if ANDROID
        public ShopDetailViewModel(DatabaseService dbService, IAudioPlayerService audioService, IHardwareIdService hardwareIdService)
        {
            _dbService = dbService;
            _audioService = audioService;
            _hardwareIdService = hardwareIdService;
            WeakReferenceMessenger.Default.Register(this);
            _ = CheckPremiumStatusAsync();
        }
#else
        public ShopDetailViewModel(DatabaseService dbService, IAudioPlayerService audioService)
        {
            _dbService = dbService;
            _audioService = audioService;
            WeakReferenceMessenger.Default.Register(this);
        }
#endif

        private async Task CheckPremiumStatusAsync()
        {
#if ANDROID
            try
            {
                var hId = _hardwareIdService.GetHardwareId();
                var status = await _dbService.CheckDeviceStatusAsync(hId);
                if (status != null)
                {
                    IsDevicePremium = status.IsPremium;
                }
            }
            catch { }
#endif
        }

        // ═══════ FIX: IQueryAttributable — Bọc try-catch toàn bộ logic nhận tham số ═══════

        /// <summary>
        /// Nhận tham số từ Shell Navigation một cách an toàn.
        /// ShopId được gán CUỐI CÙNG để đảm bảo các flag Premium/Trial đã sẵn sàng
        /// trước khi OnShopIdChanged kích hoạt LoadShopAndAutoPlayAsync.
        /// </summary>
        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[ShopDetailVM] ApplyQueryAttributes — {query.Count} params");

                if (query.TryGetValue("ShopData", out var shopObj) && shopObj is ShopModel shopData)
                {
                    Shop = shopData;
                }

                if (query.TryGetValue("IsFromDeepLink", out var isDeepLinkObj) && isDeepLinkObj != null)
                {
                    IsFromDeepLink = Convert.ToBoolean(isDeepLinkObj);
                }

                if (query.TryGetValue("IsPremium", out var isPremiumObj) && isPremiumObj != null)
                {
                    IsPremium = Convert.ToBoolean(isPremiumObj);
                }

                if (query.TryGetValue("TrialRemaining", out var trialObj) && trialObj != null)
                {
                    TrialRemaining = Convert.ToInt32(trialObj);
                }

                if (query.TryGetValue("HardwareId", out var hardwareIdObj) && hardwareIdObj != null)
                {
                    HardwareId = hardwareIdObj.ToString();
                }

                // Gán ShopId CUỐI CÙNG — vì setter sẽ trigger OnShopIdChanged → AutoPlay
                if (query.TryGetValue("ShopId", out var idObj) && idObj != null)
                {
                    ShopId = idObj.ToString();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShopDetailVM] ❌ Lỗi ApplyQueryAttributes: {ex}");
            }
        }

        /// <summary>
        /// Được gọi tự động khi ShopId thay đổi (từ Deep Link navigation).
        /// Load shop từ DB rồi kích hoạt auto-play.
        /// </summary>
        partial void OnShopIdChanged(string? value)
        {
            try
            {
                if (!string.IsNullOrEmpty(value) && IsFromDeepLink)
                {
                    _ = LoadShopAndAutoPlayAsync(value);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShopDetailVM] ❌ Lỗi OnShopIdChanged: {ex}");
            }
        }

        /// <summary>
        /// Tải thông tin shop từ SQLite rồi tự động phát audio theo quyền Premium/Trial.
        /// </summary>
        private async Task LoadShopAndAutoPlayAsync(string id)
        {
            try
            {
                IsDeepLinkLoading = true;
                System.Diagnostics.Debug.WriteLine($"[DeepLink] Đang tải shop: {id}");

                // Tải thông tin shop từ database cục bộ
                var loadedShop = await _dbService.GetShopAsync(id);
                if (loadedShop == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[DeepLink] Không tìm thấy shop: {id}");
                    ShowTrialBanner = true;
                    TrialBannerText = "Không tìm thấy quán này trong dữ liệu. Vui lòng đồng bộ dữ liệu mới.";
                    return;
                }

                // Gán shop data vào property (không dùng setter để tránh gọi lại RefreshShopDataAsync)
                SetProperty(ref shop, loadedShop, nameof(Shop));

                // ── Xử lý audio dựa trên quyền Premium / Trial ──
                await ProcessDeepLinkAudioAsync(loadedShop);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DeepLink] Lỗi tải shop: {ex.Message}");
            }
            finally
            {
                IsDeepLinkLoading = false;
            }
        }

        /// <summary>
        /// Logic phát audio khi mở từ Deep Link:
        /// - Premium: phát toàn bộ audio (ưu tiên offline, fallback online)
        /// - Trial còn lượt: phát audio demo + ghi log trial
        /// - Trial hết lượt: hiện thông báo nâng cấp
        /// </summary>
        private async Task ProcessDeepLinkAudioAsync(ShopModel targetShop)
        {
            if (IsPremium)
            {
                // ── PREMIUM: Phát toàn bộ audio không giới hạn ──
                ShowTrialBanner = true;
                TrialBannerText = "🌟 Premium — Thưởng thức audio thuyết minh đầy đủ";
                ShowUpgradeButton = false;

                await PlayShopAudioAsync(targetShop);
            }
            else if (TrialRemaining > 0)
            {
                // ── TRIAL: Cho nghe thử + ghi log ──
                ShowTrialBanner = true;
                TrialBannerText = $"🎧 Bản nghe thử — Còn {TrialRemaining} lượt trong 24h";
                ShowUpgradeButton = true;

                // Ghi log trial lên server
                if (!string.IsNullOrEmpty(HardwareId) && _dbService != null)
                {
                    var result = await _dbService.RecordTrialAsync(HardwareId, targetShop.Id);
                    if (result != null)
                    {
                        TrialRemaining = result.Remaining;
                        TrialBannerText = $"🎧 Bản nghe thử — Còn {TrialRemaining} lượt trong 24h";
                    }
                }

                await PlayShopAudioAsync(targetShop);
            }
            else
            {
                // ── HẾT LƯỢT TRIAL: Hiện thông báo nâng cấp ──
                ShowTrialBanner = true;
                TrialBannerText = "⏰ Đã hết lượt nghe thử. Nâng cấp Premium để nghe không giới hạn!";
                ShowUpgradeButton = true;
            }
        }

        /// <summary>
        /// Phát audio cho shop — ưu tiên file offline đã cache, nếu chưa có thì stream online.
        /// </summary>
        private async Task PlayShopAudioAsync(ShopModel targetShop)
        {
            try
            {
                if (!string.IsNullOrEmpty(targetShop.AudioUrl))
                {
                    System.Diagnostics.Debug.WriteLine($"[DeepLink] Phát audio: {targetShop.AudioUrl}");
                    await _audioService.PlayShopAsync(targetShop);

                    // ═══ GHI LOG AUDIO ACTIVITY (AppManual — Deep Link) ═══
                    var deviceId = HardwareId ?? App.DeviceId;
                    var langCode = Preferences.Default.Get("AppLanguage", "vi");
                    if (!string.IsNullOrEmpty(deviceId))
                    {
                        _ = Task.Run(() => _dbService.RecordAudioLogAsync(deviceId, targetShop.Id, langCode, "AppManual"));
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[DeepLink] Shop không có audio URL.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DeepLink] Lỗi phát audio: {ex.Message}");
            }
        }

        private async void RefreshShopDataAsync(string shopId)
        {
            try
            {
                var freshShop = await _dbService.GetShopAsync(shopId);
                if (freshShop != null && shop != null)
                {
                    // Chỉ cập nhật UI nếu có sự khác biệt (nhất là sau khi ứng dụng đồng bộ text mới ngầm)
                    if (freshShop.Name != shop.Name || freshShop.Description != shop.Description || freshShop.Address != shop.Address)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            // Bỏ qua setter để tránh gọi lại LoadDishes()
                            SetProperty(ref shop, freshShop, nameof(Shop));
                        });
                    }
                }
            }
            catch { }
        }

        [RelayCommand]
        public async Task PlayItemAudio(ShopItemModel item)
        {
            if (item.IsPremiumOnly && !IsDevicePremium)
            {
                await Application.Current.MainPage.DisplayAlert("Khóa Premium", "Nội dung này dành riêng cho tài khoản Premium. Vui lòng nâng cấp để nghe chi tiết.", "Đóng");
                return;
            }

            if (!string.IsNullOrEmpty(item.AudioUrl))
            {
                await _audioService.PlayAsync(item.AudioUrl);

                // ═══ GHI LOG AUDIO ACTIVITY (AppManual — ShopItem) ═══
                var deviceId = HardwareId ?? App.DeviceId;
                var langCode = Preferences.Default.Get("AppLanguage", "vi");
                var shopId = Shop?.Id ?? ShopId ?? string.Empty;
                if (!string.IsNullOrEmpty(deviceId) && !string.IsNullOrEmpty(shopId))
                {
                    _ = Task.Run(() => _dbService.RecordAudioLogAsync(deviceId, shopId, langCode, "AppManual", Guid.TryParse(item.Id, out var gid) ? gid : null));
                }
            }
        }

        [RelayCommand]
        public async Task GoBack()
        {
            await Shell.Current.GoToAsync("..");
        }

        public async void Receive(LanguageChangedMessage message)
        {
            // Nếu trang chi tiết đang mở và có shop hiện tại, reload lại dữ liệu Text / Translation từ DB
            if (shop != null && !string.IsNullOrEmpty(shop.Id))
            {
                try
                {
                    var updatedShop = await _dbService.GetShopAsync(shop.Id);
                    if (updatedShop != null)
                    {
                        // Gắn lại Shop sẽ tự trigger OnPropertyChanged
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            Shop = updatedShop;
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ShopDetail] Receive LanguageChanged error: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            WeakReferenceMessenger.Default.UnregisterAll(this);
        }
    }
}