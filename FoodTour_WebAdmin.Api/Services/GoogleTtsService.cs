using System.Text;
using System.Text.Json;

namespace FoodTour_WebAdmin.Api.Services;

public class GoogleTtsService : ITtsService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    // Mapping from app language codes to Google TTS voice configs
    private static readonly Dictionary<string, (string languageCode, string voiceName)> VoiceMap = new()
    {
        { "vi", ("vi-VN", "vi-VN-Standard-A") },
        { "en", ("en-US", "en-US-Standard-C") },
        { "ja", ("ja-JP", "ja-JP-Standard-A") },
        { "zh", ("cmn-CN", "cmn-CN-Standard-A") },
        { "ru", ("ru-RU", "ru-RU-Standard-A") }
    };

    public GoogleTtsService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["GoogleTts:ApiKey"]
            ?? throw new ArgumentNullException("GoogleTts:ApiKey is missing in configuration.");
    }

    public async Task<byte[]> SynthesizeSpeechAsync(string text, string languageCode)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<byte>();

        var (googleLangCode, voiceName) = VoiceMap.TryGetValue(languageCode, out var voice)
            ? voice
            : ("en-US", "en-US-Standard-C"); // fallback to English

        var requestBody = new
        {
            input = new { text },
            voice = new
            {
                languageCode = googleLangCode,
                name = voiceName
            },
            audioConfig = new
            {
                audioEncoding = "MP3",
                speakingRate = 1.0,
                pitch = 0.0
            }
        };

        var url = $"https://texttospeech.googleapis.com/v1/text:synthesize?key={_apiKey}";
        var content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync(url, content);
        response.EnsureSuccessStatusCode();

        var responseString = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(responseString);

        var audioContentBase64 = document.RootElement
            .GetProperty("audioContent")
            .GetString();

        return Convert.FromBase64String(audioContentBase64 ?? string.Empty);
    }
}
