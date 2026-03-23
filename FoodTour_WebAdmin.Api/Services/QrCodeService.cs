using QRCoder;

namespace FoodTour_WebAdmin.Api.Services;

public class QrCodeService : IQrCodeService
{
    /// <summary>
    /// Generate a QR code PNG for a shop.
    /// URL format: {baseUrl}/foodtour/{shopId}
    /// The landing page will auto-detect browser language.
    /// </summary>
    public byte[] GenerateQrCodePng(string shopId, string baseUrl, int pixelsPerModule = 10)
    {
        var url = $"{baseUrl.TrimEnd('/')}/foodtour/{shopId}";
        
        using var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(qrCodeData);
        
        return qrCode.GetGraphic(pixelsPerModule);
    }
}
