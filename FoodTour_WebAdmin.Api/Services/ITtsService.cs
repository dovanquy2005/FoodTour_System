namespace FoodTour_WebAdmin.Api.Services;

public interface ITtsService
{
    /// <summary>
    /// Convert text to speech and return the audio as MP3 bytes.
    /// </summary>
    /// <param name="text">The text to synthesize.</param>
    /// <param name="languageCode">Language code (vi, en, ja, zh, ru).</param>
    /// <returns>MP3 audio data as byte array.</returns>
    Task<byte[]> SynthesizeSpeechAsync(string text, string languageCode);
}
