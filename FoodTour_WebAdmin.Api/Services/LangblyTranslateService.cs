using System.Text;
using System.Text.Json;

namespace FoodTour_WebAdmin.Api.Services;

public class LangblyTranslateService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public LangblyTranslateService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Langbly:ApiKey"] 
                  ?? throw new ArgumentNullException("Langbly API Key is missing");
    }

    public async Task<string> TranslateTextAsync(string text, string targetLanguage, string sourceLanguage = "vi")
    {
        if (string.IsNullOrWhiteSpace(text)) return text;

        var requestBody = new
        {
            q = text,
            target = targetLanguage,
            source = sourceLanguage,
            format = "text"
        };

        var requestUrl = $"https://api.langbly.com/language/translate/v2?key={_apiKey}";
        var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        };
        
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseString = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(responseString);
        
        var translatedText = document.RootElement
            .GetProperty("data")
            .GetProperty("translations")[0]
            .GetProperty("translatedText")
            .GetString();

        return translatedText ?? string.Empty;
    }
}
