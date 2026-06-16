using QRCoder;

namespace Dsms.Web.Services;

public static class AuthenticatorQrCodeGenerator
{
    public static string? TryGenerateBase64Png(string otpauthUri, int pixelsPerModule = 8)
    {
        if (string.IsNullOrWhiteSpace(otpauthUri))
        {
            return null;
        }

        try
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(otpauthUri, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(data);
            var bytes = qrCode.GetGraphic(pixelsPerModule);
            return Convert.ToBase64String(bytes);
        }
        catch
        {
            return null;
        }
    }
}
