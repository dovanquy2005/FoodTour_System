namespace FoodTour.Mobile.Helpers
{
    /// <summary>
    /// Lớp tiện ích tĩnh để giải quyết đường dẫn ảnh.
    /// Ưu tiên file cache cục bộ trên thiết bị, nếu không tồn tại thì trả về URL API đầy đủ.
    /// </summary>
    public static class ImagePathHelper
    {
        // Lấy base URL của API dựa trên nền tảng đang chạy
        private static string GetApiBaseUrl()
        {
            return "https://foodtour-admin-api.onrender.com";
        }

        /// <summary>
        /// Giải quyết đường dẫn ảnh từ đường dẫn tương đối của server.
        /// - Nếu file đã được cache cục bộ trong AppDataDirectory → trả về đường dẫn cục bộ.
        /// - Nếu chưa có cache → trả về URL đầy đủ từ API để tải qua mạng.
        /// - Nếu đường dẫn rỗng hoặc null → trả về chuỗi rỗng.
        /// </summary>
        public static string ResolveImageUrl(string? relativeUrl)
        {
            if (string.IsNullOrEmpty(relativeUrl))
                return string.Empty;

            // Nếu đường dẫn bắt đầu bằng "/" → đây là đường dẫn tương đối từ server
            if (relativeUrl.StartsWith("/"))
            {
                var fileName = Path.GetFileName(relativeUrl);
                var localPath = Path.Combine(FileSystem.AppDataDirectory, fileName);

                // Kiểm tra file đã tồn tại trong cache cục bộ chưa
                if (File.Exists(localPath))
                    return localPath;

                // Chưa có cache → trả về URL API đầy đủ để tải qua mạng
                return GetApiBaseUrl().TrimEnd('/') + relativeUrl;
            }

            // Trường hợp đường dẫn đã là đường dẫn đầy đủ (URL hoặc local) → giữ nguyên
            return relativeUrl;
        }
    }
}
