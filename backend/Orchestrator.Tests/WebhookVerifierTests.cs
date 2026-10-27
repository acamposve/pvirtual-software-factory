using System.Security.Cryptography;
using System.Text;
using Orchestrator.Services;
using Xunit;

namespace Orchestrator.Tests;

/// <summary>Tests de validación HMAC de webhooks (acceptance: webhook sin firma válida → 401).</summary>
public class WebhookVerifierTests
{
    private const string Secret = "test-secret";
    private const string Body = """{"action":"labeled"}""";

    private static string Sign(string body, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return "sha256=" + Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
    }

    [Fact]
    public void Firma_valida_pasa()
    {
        Assert.True(WebhookVerifier.IsValidSignature(Body, Sign(Body, Secret), Secret));
    }

    [Fact]
    public void Firma_invalida_falla()
    {
        Assert.False(WebhookVerifier.IsValidSignature(Body, Sign(Body, "otro-secret"), Secret));
    }

    [Fact]
    public void Sin_firma_falla()
    {
        Assert.False(WebhookVerifier.IsValidSignature(Body, null, Secret));
    }

    [Fact]
    public void Firma_con_formato_incorrecto_falla()
    {
        Assert.False(WebhookVerifier.IsValidSignature(Body, "sha1=abcdef", Secret));
        Assert.False(WebhookVerifier.IsValidSignature(Body, "garbage", Secret));
    }

    [Fact]
    public void Body_alterado_falla()
    {
        var signature = Sign(Body, Secret);
        Assert.False(WebhookVerifier.IsValidSignature(Body + " ", signature, Secret));
    }
}
