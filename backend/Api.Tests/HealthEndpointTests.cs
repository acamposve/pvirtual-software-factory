using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Api.Tests;

// Cubre specs/health-check/acceptance.md
public class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _client;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetHealth_ReturnsHealthyStatusAndTimestamp()
    {
        var body = await _client.GetFromJsonAsync<HealthResponseDto>("/health", JsonOptions);

        Assert.NotNull(body);
        Assert.Equal("healthy", body!.Status);
        // timestampUtc deberia ser un DateTime valido, cercano a "ahora".
        Assert.True(DateTime.UtcNow - body.TimestampUtc < TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task GetHealth_DoesNotRequireAuthentication()
    {
        // El HttpClient de la factory no manda ningun header de auth por
        // default. Si el endpoint exigiera auth, esto fallaria con 401/403.
        var response = await _client.GetAsync("/health");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // DTO propio del test (no referencia el record interno de Program.cs)
    // para no depender de su visibilidad/accesibilidad entre proyectos.
    private sealed record HealthResponseDto(string Status, DateTime TimestampUtc);
}
