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

        try
        {
            var requestUrl = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={sourceLanguage}&tl={targetLanguage}&dt=t&q={Uri.EscapeDataString(text)}";
            var response = await _httpClient.GetAsync(requestUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                return text;
            }

            var responseString = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(responseString);
            
            var translatedText = "";
            if (document.RootElement.ValueKind == JsonValueKind.Array && document.RootElement.GetArrayLength() > 0)
            {
                var parts = document.RootElement[0];
                foreach (var part in parts.EnumerateArray())
                {
                    if (part.ValueKind == JsonValueKind.Array && part.GetArrayLength() > 0)
                    {
                        var str = part[0].GetString();
                        if (!string.IsNullOrEmpty(str))
                        {
                            translatedText += str;
                        }
                    }
                }
            }

            return !string.IsNullOrWhiteSpace(translatedText) ? translatedText : text;
        }
        catch
        {
            // Trong trường hợp lỗi mạng hoặc JSON parse
            return text;
        }
    }
}
