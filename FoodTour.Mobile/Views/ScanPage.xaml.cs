using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
using FoodTour.Mobile.Models;
using FoodTour.Mobile.ViewModels;
using FoodTour.Mobile.Extensions;

namespace FoodTour.Mobile.Views
{
    public partial class ScanPage : ContentPage
    {
        private bool _isAnimating;
        private bool _isTorchOn = false;
        private ScanViewModel? _viewModel;
        private readonly ImageUrlConverter _imageConverter = new();
        private readonly Dictionary<string, (Button PlayBtn, Slider ProgressSlider, Label TimeLabel)> _cardControls = new();

        public ScanPage(ScanViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
            _viewModel = viewModel;

            barcodeReader.Options = new BarcodeReaderOptions
            {
                Formats = BarcodeFormats.All, 
                AutoRotate = true,
                Multiple = false
            };

            _viewModel.PlaybackEnded += OnPlaybackEndedHandler;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        // ═══════ LIFECYCLE ═══════

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await RequestCameraPermission();

            viewfinderBorder.Stroke = Color.FromArgb("#E8672A");
            scanLine.Color = Color.FromArgb("#E8672A");
            scanLine.TranslationY = 20;

            _isAnimating = true;
            StartScanLineAnimation();

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(300);

                if (!this.IsVisible) return;

                if (_viewModel != null)
                {
                    if (!_viewModel.IsSuccess)
                        _viewModel.IsScanning = true;
                }

                if (barcodeReader != null)
                {
                    barcodeReader.IsVisible = false;
                    barcodeReader.IsVisible = true;

                    barcodeReader.CameraLocation = CameraLocation.Front;
                    barcodeReader.CameraLocation = CameraLocation.Rear;

                    barcodeReader.IsTorchOn = _isTorchOn;

                    if (_viewModel != null && _viewModel.IsScanning)
                        barcodeReader.IsDetecting = true;
                }
            });
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            
            _isAnimating = false;
            scanLine.CancelAnimations();

            if (barcodeReader != null)
            {
                barcodeReader.IsDetecting = false;

                if (_isTorchOn)
                {
                    _isTorchOn = false;
                    barcodeReader.IsTorchOn = false;
                    torchButton.BackgroundColor = Color.FromArgb("#44FFFFFF");
                    torchIcon.Text = "🔦";
                }
            }
        }

        // ═══════ MÀN HÌNH DANH SÁCH ═══════
        
        private void OnPlaybackEndedHandler()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ResetAllMediaControls();
            });
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ScanViewModel.CurrentPosition) ||
                e.PropertyName == nameof(ScanViewModel.Duration) ||
                e.PropertyName == nameof(ScanViewModel.IsPlaying))
            {
                UpdateMediaControlsState();
            }
            else if (e.PropertyName == nameof(ScanViewModel.Shops) && _viewModel != null)
            {
                RenderShopCards(_viewModel.Shops);
            }
            else if (e.PropertyName == nameof(ScanViewModel.IsScanning) && _viewModel != null)
            {
                if (_viewModel.IsScanning)
                {
                    _isAnimating = true;
                    StartScanLineAnimation();
                    if (barcodeReader != null) barcodeReader.IsDetecting = true;
                    
                    viewfinderBorder.Stroke = Color.FromArgb("#E8672A");
                    scanLine.Color = Color.FromArgb("#E8672A");
                }
            }
        }

        private void ResetAllMediaControls()
        {
            foreach (var kvp in _cardControls)
            {
                var (playBtn, slider, timeLabel) = kvp.Value;
                playBtn.Text = "▶";
                slider.Value = 0;
                timeLabel.Text = "0:00 / 0:00";
            }
        }

        private void UpdateMediaControlsState()
        {
            if (_viewModel == null) return;

            foreach (var kvp in _cardControls)
            {
                var (playBtn, slider, timeLabel) = kvp.Value;
                bool isThisShop = kvp.Key == _viewModel.PlayingShopId;

                if (isThisShop)
                {
                    playBtn.Text = _viewModel.IsPlaying ? "⏸" : "▶";

                    if (_viewModel.Duration > 0)
                    {
                        slider.Maximum = _viewModel.Duration;
                        slider.Value = _viewModel.CurrentPosition;
                    }

                    timeLabel.Text = $"{FormatTime(_viewModel.CurrentPosition)} / {FormatTime(_viewModel.Duration)}";
                }
                else
                {
                    playBtn.Text = "▶";
                    slider.Maximum = 100;
                    slider.Value = 0;
                    timeLabel.Text = "0:00 / 0:00";
                }
            }
        }

        private string FormatTime(double seconds)
        {
            if (seconds <= 0) return "0:00";
            int mins = (int)(seconds / 60);
            int secs = (int)(seconds % 60);
            return $"{mins}:{secs:D2}";
        }

        private void RenderShopCards(IList<ShopModel> shops)
        {
            ShopListContainer.Children.Clear();
            _cardControls.Clear();

            if (shops == null || shops.Count == 0) return;

            foreach (var shop in shops)
            {
                var card = CreateShopCard(shop);
                ShopListContainer.Children.Add(card);
            }
        }

        private Border CreateShopCard(ShopModel shop)
        {
            var card = new Border
            {
                BackgroundColor = Colors.White,
                StrokeThickness = 0,
                Padding = new Thickness(12)
            };

            var roundedRect = new RoundRectangle { CornerRadius = 16 };
            card.StrokeShape = roundedRect;

            var cardShadow = new Shadow
            {
                Radius = 6,
                Opacity = 0.08f,
                Offset = new Point(0, 2)
            };
            card.Shadow = cardShadow;

            var mainGrid = new Grid { ColumnDefinitions = new ColumnDefinitionCollection {
                new ColumnDefinition { Width = new GridLength(80) },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            }};

            var imageBorder = new Border
            {
                WidthRequest = 80,
                HeightRequest = 80,
                StrokeThickness = 0,
                BackgroundColor = Color.FromArgb("#FFF0E6"),
                Margin = new Thickness(0, 0, 12, 0)
            };
            imageBorder.StrokeShape = new RoundRectangle { CornerRadius = 12 };

            var resolvedUrl = _imageConverter.Convert(shop.ImageUrl, typeof(string), null, System.Globalization.CultureInfo.InvariantCulture) as string;
            ImageSource imageSource;
            if (!string.IsNullOrEmpty(resolvedUrl))
            {
                imageSource = resolvedUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? ImageSource.FromUri(new Uri(resolvedUrl))
                    : ImageSource.FromFile(resolvedUrl);
            }
            else
            {
                imageSource = ImageSource.FromFile("explore.png");
            }
            var image = new Image
            {
                Source = imageSource,
                Aspect = Aspect.Fill,
                InputTransparent = true
            };
            imageBorder.Content = image;

            var infoStack = new VerticalStackLayout
            {
                Spacing = 2,
                VerticalOptions = LayoutOptions.Center
            };

            var nameLabel = new Label
            {
                Text = shop.Name ?? "Unknown",
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#2D1F14"),
                LineBreakMode = LineBreakMode.TailTruncation,
                MaxLines = 1
            };

            var addressLabel = new Label
            {
                Text = shop.Address ?? "",
                FontSize = 12,
                TextColor = Color.FromArgb("#8C7B6B"),
                LineBreakMode = LineBreakMode.TailTruncation,
                MaxLines = 1,
                Margin = new Thickness(0, 0, 0, 4)
            };

            var mediaBar = CreateMediaControlBar(shop);

            infoStack.Add(nameLabel);
            infoStack.Add(addressLabel);
            infoStack.Add(mediaBar);

            Grid.SetColumn(imageBorder, 0);
            Grid.SetColumn(infoStack, 1);

            mainGrid.Children.Add(imageBorder);
            mainGrid.Children.Add(infoStack);

            card.Content = mainGrid;

            return card;
        }

        private View CreateMediaControlBar(ShopModel shop)
        {
            var mediaGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                Margin = new Thickness(0, 0, 0, 0),
                BackgroundColor = Colors.Transparent,
                Padding = new Thickness(0, 0, 0, 0)
            };

            var playBtn = new Button
            {
                Text = "▶",
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                BackgroundColor = Color.FromArgb("#E8672A"),
                WidthRequest = 32,
                HeightRequest = 32,
                CornerRadius = 16,
                Padding = 0,
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            playBtn.Clicked += async (s, e) =>
            {
                if (_viewModel == null) return;

                if (_viewModel.PlayingShopId == shop.Id)
                {
                    await _viewModel.TogglePlayPauseCommand.ExecuteAsync(null);
                }
                else
                {
                    await _viewModel.PlayShopAudioCommand.ExecuteAsync(shop);
                }
            };

            var slider = new Slider
            {
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                HeightRequest = 24, // Thinner slider look
                MinimumTrackColor = Color.FromArgb("#E8672A"),
                MaximumTrackColor = Color.FromArgb("#D0D0D0"),
                ThumbColor = Color.FromArgb("#E8672A"),
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            slider.DragStarted += (s, e) =>
            {
                if (_viewModel?.PlayingShopId == shop.Id)
                {
                    slider.Value = _viewModel.CurrentPosition;
                }
            };
            slider.DragCompleted += (s, e) =>
            {
                if (_viewModel != null && _viewModel.PlayingShopId == shop.Id)
                {
                    _viewModel.SeekAudioCommand.Execute(slider.Value);
                }
            };

            var timeLabel = new Label
            {
                Text = "0:00 / 0:00",
                FontSize = 10,
                TextColor = Color.FromArgb("#8C7B6B"),
                VerticalOptions = LayoutOptions.Center,
                WidthRequest = 60
            };

            Grid.SetColumn(playBtn, 0);
            Grid.SetColumn(slider, 1);
            Grid.SetColumn(timeLabel, 2);

            mediaGrid.Children.Add(playBtn);
            mediaGrid.Children.Add(slider);
            mediaGrid.Children.Add(timeLabel);

            if (!string.IsNullOrEmpty(shop.Id))
            {
                _cardControls[shop.Id] = (playBtn, slider, timeLabel);
            }

            return mediaGrid;
        }

        // ═══════ NÚT BẬT/TẮT ĐÈN FLASH ═══════
        private void ToggleTorch_Tapped(object? sender, TappedEventArgs e)
        {
            _isTorchOn = !_isTorchOn;
            
            if (barcodeReader != null)
            {
                barcodeReader.IsTorchOn = _isTorchOn;
            }

            torchButton.BackgroundColor = _isTorchOn
                ? Color.FromArgb("#E8672A")
                : Color.FromArgb("#44FFFFFF");
            torchIcon.Text = _isTorchOn ? "💡" : "🔦";
        }

        // ═══════ KIỂM TRA QUYỀN CAMERA ═══════

        private async Task RequestCameraPermission()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Camera>();
                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlert("Lỗi", "Vui lòng cấp quyền sử dụng camera để quét mã QR", "OK");
                }
            }
        }

        // ═══════ ANIMATION QUÉT LASER ═══════

        private void StartScanLineAnimation()
        {
            if (scanLine == null) return;

            var animation = new Animation(v => scanLine.TranslationY = v, 20, 220);
            animation.Commit(this, "ScanLineAnimation", 16, 2000, Easing.Linear, (v, c) =>
            {
                if (_isAnimating)
                {
                    var reverseAnimation = new Animation(v => scanLine.TranslationY = v, 220, 20);
                    reverseAnimation.Commit(this, "ReverseScanLineAnimation", 16, 2000, Easing.Linear, (v, c) =>
                    {
                        if (_isAnimating)
                            StartScanLineAnimation(); 
                    });
                }
            }, () => false);
        }

        // ═══════ XỬ LÝ KẾT QUẢ QUÉT ═══════

        private void BarcodeReader_BarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
        {
            var barcodes = e.Results;
            if (barcodes == null || barcodes.Length == 0) return;

            var firstBarcode = barcodes[0].Value;

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (barcodeReader != null)
                {
                    barcodeReader.IsDetecting = false;
                }

                _isAnimating = false;

                viewfinderBorder.Stroke = Colors.Green;
                scanLine.Color = Colors.Green;

                await Task.Delay(500);

                if (_viewModel != null && _viewModel.IsScanning)
                {
                    await _viewModel.ProcessQrCodeCommand.ExecuteAsync(firstBarcode);
                }
            });
        }
    }
}