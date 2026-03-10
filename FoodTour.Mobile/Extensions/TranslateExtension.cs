using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using FoodTour.Mobile.Services;

namespace FoodTour.Mobile.Extensions
{
    /// <summary>
    /// Custom Markup Extension for XAML, establishing a OneWay dynamic binding to the OTA 
    /// localization dictionary using Clean Architecture and DI principles.
    /// </summary>
    [ContentProperty(nameof(Key))]
    [AcceptEmptyServiceProvider]
    public class TranslateExtension : IMarkupExtension<BindingBase>
    {
        public string Key { get; set; } = string.Empty;

        public BindingBase ProvideValue(IServiceProvider serviceProvider)
        {
            if (string.IsNullOrEmpty(Key))
            {
                return new Binding { Source = string.Empty };
            }

            // Architecture resolution: we decouple UI from explicit service instantiation
            // by relying directly on MAUI's registered DI container.
            var services = Application.Current?.Handler?.MauiContext?.Services 
                           ?? IPlatformApplication.Current?.Services;

            var localizationService = services?.GetService(typeof(ILocalizationService)) as ILocalizationService;

            if (localizationService == null)
            {
                // Fallback gracefully if service vanishes
                return new Binding { Source = $"[{Key}]" };
            }

            // Create a binding that connects specifically to the string indexer we exposed: 'Item[key]'
            return new Binding
            {
                Mode = BindingMode.OneWay,
                Path = $"[{Key}]",
                Source = localizationService
            };
        }

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
        {
            return (this as IMarkupExtension<BindingBase>).ProvideValue(serviceProvider);
        }
    }
}
