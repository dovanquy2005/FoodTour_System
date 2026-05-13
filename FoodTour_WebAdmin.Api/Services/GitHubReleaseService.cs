using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace FoodTour_WebAdmin.Api.Services;

public class GitHubReleaseService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private const string CacheKey = "LatestAppVersion";

    public GitHubReleaseService(HttpClient httpClient, IMemoryCache cache)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.github.com/");
        // GitHub API returns 403 if User-Agent is missing
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "FoodTour-WebAdmin"); 
        _cache = cache;
    }

    public async Task<string> GetLatestVersionAsync()
    {
        if (_cache.TryGetValue(CacheKey, out string? cachedVersion) && !string.IsNullOrEmpty(cachedVersion))
        {
            return cachedVersion;
        }

        try
        {
            // 1. Thử gọi qua GitHub API trước
            var response = await _httpClient.GetAsync("repos/dovanquy2005/FoodTour_System/releases/latest");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                using var json = JsonDocument.Parse(content);
                var version = json.RootElement.GetProperty("tag_name").GetString() ?? "v1.0";
                
                _cache.Set(CacheKey, version, TimeSpan.FromMinutes(10));
                return version;
            }
            
            // 2. Nếu API bị chặn (403 Forbidden - do Render dùng chung IP nên dễ bị dính Rate Limit)
            // -> Chuyển sang đọc Location Header từ HTTP 302 Redirect của Web GitHub
            using var handler = new HttpClientHandler { AllowAutoRedirect = false };
            using var webClient = new HttpClient(handler);
            var webResponse = await webClient.GetAsync("https://github.com/dovanquy2005/FoodTour_System/releases/latest");
            
            if (webResponse.StatusCode == System.Net.HttpStatusCode.Found) // HTTP 302
            {
                var location = webResponse.Headers.Location?.ToString();
                if (!string.IsNullOrEmpty(location))
                {
                    // location thường có dạng: https://github.com/dovanquy2005/FoodTour_System/releases/tag/v1.5
                    var version = location.Split('/').Last();
                    
                    _cache.Set(CacheKey, version, TimeSpan.FromMinutes(10));
                    return version;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FoodTour] Lỗi khi lấy phiên bản từ GitHub: {ex.Message}");
        }

        return "v1.0"; // Fallback default
    }
}
