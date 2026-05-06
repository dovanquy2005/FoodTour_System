using FoodTour.Mobile.Models;
using FoodTour.Mobile.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using FoodTour.Mobile.Extensions;

namespace FoodTour.Mobile.Views
{
    public partial class GlobalShopListPage : ContentPage
    {
        private GlobalShopListViewModel? _viewModel;
        private readonly ImageUrlConverter _imageConverter = new();
        private readonly Dictionary<string, (ImageButton PlayBtn, Slider ProgressSlider, Label TimeLabel)> _cardControls = new();

        public GlobalShopListPage(GlobalShopListViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
            _viewModel = viewModel;
            _viewModel.PlaybackEnded += OnPlaybackEndedHandler;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is GlobalShopListViewModel vm)
            {
                _viewModel = vm;
                vm.PropertyChanged += ViewModel_PropertyChanged;
                await vm.LoadShopsCommand.ExecuteAsync(null);
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (_viewModel != null)
            {
                _viewModel.PlaybackEnded -= OnPlaybackEndedHandler;
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }
        }

        private void OnPlaybackEndedHandler()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ResetAllMediaControls();
            });
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GlobalShopListViewModel.CurrentPosition) ||
                e.PropertyName == nameof(GlobalShopListViewModel.Duration) ||
                e.PropertyName == nameof(GlobalShopListViewModel.IsPlaying))
            {
                UpdateMediaControlsState();
            }
        }

        private void ResetAllMediaControls()
        {
            foreach (var kvp in _cardControls)
            {
                var (playBtn, slider, timeLabel) = kvp.Value;
                playBtn.Source = ImageSource.FromFile("play.png");
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
                    playBtn.Source = _viewModel.IsPlaying
                        ? ImageSource.FromFile("pause.png")
                        : ImageSource.FromFile("play.png");

                    if (_viewModel.Duration > 0)
                    {
                        slider.Maximum = _viewModel.Duration;
                        slider.Value = _viewModel.CurrentPosition;
                    }

                    timeLabel.Text = $"{FormatTime(_viewModel.CurrentPosition)} / {FormatTime(_viewModel.Duration)}";
                }
                else
                {
                    playBtn.Source = ImageSource.FromFile("play.png");
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

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GlobalShopListViewModel.Shops) && _viewModel != null)
            {
                RenderShopCards(_viewModel.Shops);
            }
        }

        private void RenderShopCards(IList<ShopModel> shops)
        {
            ShopListContainer.Children.Clear();
            _cardControls.Clear();

            if (shops == null || shops.Count == 0)
            {
                EmptyStateLabel.IsVisible = true;
                return;
            }

            EmptyStateLabel.IsVisible = false;

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
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = GridLength.Auto }
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
                Spacing = 4,
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
                FontSize = 13,
                TextColor = Color.FromArgb("#8C7B6B"),
                LineBreakMode = LineBreakMode.TailTruncation,
                MaxLines = 2
            };

            infoStack.Add(nameLabel);
            infoStack.Add(addressLabel);

            var chevron = new Label
            {
                Text = ">",
                FontSize = 24,
                TextColor = Color.FromArgb("#B5A899"),
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(8, 0, 0, 0)
            };

            Grid.SetColumn(imageBorder, 0);
            Grid.SetColumn(infoStack, 1);
            Grid.SetColumn(chevron, 2);

            mainGrid.Children.Add(imageBorder);
            mainGrid.Children.Add(infoStack);
            mainGrid.Children.Add(chevron);

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += async (s, e) =>
            {
                if (_viewModel != null)
                {
                    await _viewModel.PlayShopAudioCommand.ExecuteAsync(shop);
                }
            };
            mainGrid.GestureRecognizers.Add(tapGesture);

            var mediaBar = CreateMediaControlBar(shop);
            var contentStack = new VerticalStackLayout
            {
                Spacing = 8
            };
            contentStack.Add(mainGrid);
            contentStack.Add(mediaBar);

            card.Content = contentStack;

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
                Margin = new Thickness(0, 4, 0, 0),
                BackgroundColor = Color.FromArgb("#F5F5F5"),
                Padding = new Thickness(8, 6, 8, 6)
            };

            var playBtn = new ImageButton
            {
                WidthRequest = 36,
                HeightRequest = 36,
                Source = ImageSource.FromFile("play.png"),
                BackgroundColor = Color.FromArgb("#E8672A"),
                CornerRadius = 18,
                Padding = 6,
                VerticalOptions = LayoutOptions.Center
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
                MinimumTrackColor = Color.FromArgb("#E8672A"),
                MaximumTrackColor = Color.FromArgb("#D0D0D0"),
                ThumbColor = Color.FromArgb("#E8672A"),
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(8, 0, 8, 0)
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
                FontSize = 11,
                TextColor = Color.FromArgb("#8C7B6B"),
                VerticalOptions = LayoutOptions.Center,
                WidthRequest = 70
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
    }
}