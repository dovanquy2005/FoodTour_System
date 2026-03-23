namespace FoodTour_WebAdmin.Api.Services;

public interface IQrCodeService
{
    /// <summary>
    /// Generate a QR code PNG image for the given shop.
    /// The QR URL format: {baseUrl}/foodtour/{shopId}
    /// </summary>
    /// <param name="shopId">The shop ID.</param>
    /// <param name="baseUrl">The base URL of the application.</param>
    /// <param name="pixelsPerModule">Size of each QR module in pixels (default: 10).</param>
    /// <returns>PNG image as byte array.</returns>
    byte[] GenerateQrCodePng(string shopId, string baseUrl, int pixelsPerModule = 10);
}
