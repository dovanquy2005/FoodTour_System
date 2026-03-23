using Microsoft.Extensions.Configuration;
using Supabase;

namespace FoodTour_WebAdmin.Api.Services;

public class SupabaseStorageService : ISupabaseStorageService
{
    private readonly Supabase.Client _supabaseClient;
    private readonly string _supabaseUrl;
    private const string ImagesBucket = "images";
    private const string AudiosBucket = "audios";

    public SupabaseStorageService(IConfiguration configuration)
    {
        _supabaseUrl = configuration["Supabase:Url"]
            ?? throw new ArgumentNullException("Supabase:Url is missing in configuration.");
        var serviceRoleKey = configuration["Supabase:ServiceRoleKey"]
            ?? throw new ArgumentNullException("Supabase:ServiceRoleKey is missing in configuration.");

        var options = new SupabaseOptions { AutoRefreshToken = false, AutoConnectRealtime = false };
        _supabaseClient = new Supabase.Client(_supabaseUrl, serviceRoleKey, options);
    }

    public async Task<string> UploadImageAsync(Stream fileStream, string fileName, string contentType = "image/jpeg")
    {
        using var ms = new MemoryStream();
        await fileStream.CopyToAsync(ms);
        var fileBytes = ms.ToArray();

        await _supabaseClient.Storage
            .From(ImagesBucket)
            .Upload(fileBytes, fileName, new Supabase.Storage.FileOptions
            {
                ContentType = contentType,
                Upsert = true
            });

        return GetPublicUrl(ImagesBucket, fileName);
    }

    public async Task<string> UploadAudioAsync(byte[] audioData, string fileName)
    {
        await _supabaseClient.Storage
            .From(AudiosBucket)
            .Upload(audioData, fileName, new Supabase.Storage.FileOptions
            {
                ContentType = "audio/mpeg",
                Upsert = true
            });

        return GetPublicUrl(AudiosBucket, fileName);
    }

    private string GetPublicUrl(string bucket, string fileName)
    {
        return $"{_supabaseUrl}/storage/v1/object/public/{bucket}/{fileName}";
    }
}
