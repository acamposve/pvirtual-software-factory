using System.Security.Cryptography;
using System.Text;

namespace Orchestrator.Services;

/// <summary>
/// Validación HMAC-SHA256 de webhooks de GitHub (specs/orchestrator/architecture.md, Seguridad).
/// </summary>
public static class WebhookVerifier
{
    public static bool IsValidSignature(string payloadBody, string? signatureHeader, string webhookSecret)
    {
        if (string.IsNullOrEmpty(signatureHeader) ||
            !signatureHeader.StartsWith("sha256=", StringComparison.Ordinal) ||
            signatureHeader.Length != "sha256=".Length + 64)
        {
            return false;
        }

        var expected = ComputeHmac(payloadBody, webhookSecret);
        try
        {
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(expected),
                Convert.FromHexString(signatureHeader["sha256=".Length..]));
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string ComputeHmac(string payloadBody, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadBody));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
