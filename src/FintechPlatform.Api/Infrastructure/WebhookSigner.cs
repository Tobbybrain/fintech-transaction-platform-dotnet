using System.Security.Cryptography;
using System.Text;

namespace FintechPlatform.Api.Infrastructure;

public static class WebhookSigner
{
    public static string Sign(string payload, string secret)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var bytes = Encoding.UTF8.GetBytes(payload);
        using var hmac = new HMACSHA256(key);
        return Convert.ToHexString(hmac.ComputeHash(bytes)).ToLowerInvariant();
    }
}
