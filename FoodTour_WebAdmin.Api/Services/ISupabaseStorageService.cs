namespace FoodTour_WebAdmin.Api.Services;

public interface ISupabaseStorageService
{
    /// <summary>
    /// Upload an image to the "images" bucket on Supabase Storage.
    /// </summary>
    /// <param name="fileStream">The image file stream.</param>
    /// <param name="fileName">Target file name (e.g. "shops/guid.jpg").</param>
    /// <param name="contentType">MIME type (e.g. "image/jpeg").</param>
    /// <returns>Public URL of the uploaded image.</returns>
    Task<string> UploadImageAsync(Stream fileStream, string fileName, string contentType = "image/jpeg");

    /// <summary>
    /// Upload an audio file to the "audios" bucket on Supabase Storage.
    /// </summary>
    /// <param name="audioData">The MP3 byte array.</param>
    /// <param name="fileName">Target file name (e.g. "shops/vi/guid.mp3").</param>
    /// <returns>Public URL of the uploaded audio file.</returns>
    Task<string> UploadAudioAsync(byte[] audioData, string fileName);
}
