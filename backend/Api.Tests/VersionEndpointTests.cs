using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Api.Tests;

// Cubre specs/version-endpoint/acceptance.md
public class VersionEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _client;

    public VersionEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetVersion_ReturnsOk()
    {
        var response = await _client.GetAsync("/version");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetVersion_ReturnsNonEmptyVersionAndEnvironment()
    {
        var body = await _client.GetFromJsonAsync<VersionResponseDto>("/version", JsonOptions);

        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.Version));
        Assert.False(string.IsNullOrWhiteSpace(body.Environment));
    }

    [Fact]
    public async Task GetVersion_VersionMatchesCsprojVersion()
    {
        var body = await _client.GetFromJsonAsync<VersionResponseDto>("/version", JsonOptions);

        Assert.NotNull(body);
        Assert.Equal("0.1.0", body!.Version);
    }

    [Fact]
    public async Task GetVersion_DoesNotRequireAuthentication()
    {
        // El HttpClient de la factory no manda ningun header de auth por
        // default. Si el endpoint exigiera auth, esto fallaria con 401/403.
        var response = await _client.GetAsync("/version");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // DTO propio del test (no referencia el record interno de Program.cs)
    // para no depender de su visibilidad/accesibilidad entre proyectos.
    private sealed record VersionResponseDto(string Version, string Environment);
}
