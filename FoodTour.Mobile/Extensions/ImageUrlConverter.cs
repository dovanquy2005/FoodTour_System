using System.Globalization;
using FoodTour.Mobile.Helpers;

namespace FoodTour.Mobile.Extensions;

/// <summary>
/// Converter XAML để giải quyết đường dẫn ảnh cho binding.
/// Chuyển đổi đường dẫn tương đối từ server thành đường dẫn hiển thị:
/// - Nếu file cache cục bộ tồn tại → dùng file cục bộ (nhanh, hoạt động offline).
/// - Nếu chưa cache → trả về URL API đầy đủ (tải qua mạng).
/// </summary>
public class ImageUrlConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Sử dụng ImagePathHelper để giải quyết đường dẫn
        var relativeUrl = value as string;
        return ImagePathHelper.ResolveImageUrl(relativeUrl);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Không cần chuyển đổi ngược cho hiển thị ảnh
        throw new NotImplementedException();
    }
}
