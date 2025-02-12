using OtpNet;
using QRCoder.Core;

namespace MFAServer.Logic;


public class TwoFactorAuthManager
{
    public string GenerateSecretKey()
    {
        // Generate a secret key for the user
        var key = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(key);
    }

    public string GenerateQrCodeUri(string secretKey, string userEmail, string issuer = AppConstants.AppName)
    {
        var mIssuer = Uri.EscapeDataString(issuer);
        var mEmail = Uri.EscapeDataString(userEmail); 
        // Generate the QR code URI for Google Authenticator
        var totp = new Totp(Base32Encoding.ToBytes(secretKey));
       // return $"otpauth://totp/{mIssuer}:{mEmail}?secret={secretKey}&issuer={mIssuer}&digits={totp.TotpSize}";
        return $"otpauth://totp/{mIssuer}:{mEmail}?secret={secretKey}&issuer={issuer}&algorithm=SHA1&digits={totp.TotpSize}&period=30";
    }

    public string GenerateQrCodeImage(string qrCodeUri)
    {
        using var generator = new QRCodeGenerator();
        using var codeData = generator.CreateQrCode(qrCodeUri, QRCodeGenerator.ECCLevel.Q);
        using var pngCodeGenerator = new PngByteQRCode(codeData);
        var qrCode = pngCodeGenerator.GetGraphic(20);
        return Convert.ToBase64String(qrCode);
    }

    public bool ValidateOtpCode(string secretKey, string code)
    {
        // Validate the provided OTP code
        var totp = new Totp(Base32Encoding.ToBytes(secretKey));
        // return totp.VerifyTotp(code, out _, VerificationWindow.RfcSpecifiedNetworkDelay); 
        return totp.VerifyTotp(code, out _); // The out parameter will capture the matched timestamp.
    }
}
