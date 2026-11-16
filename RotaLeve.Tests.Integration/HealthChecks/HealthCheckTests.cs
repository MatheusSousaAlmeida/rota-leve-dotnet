using System.Net;
using RotaLeve.Tests.Integration.Fixtures;

namespace RotaLeve.Tests.Integration.HealthChecks;

[Collection("API Integration")]
public class HealthCheckTests
{
    private readonly CustomWebApplicationFactory _factory;

    public HealthCheckTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    [Trait("HealthCheck", "API")]
    public async Task Live_DeveRetornarOk()
    {
        // Arrange
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", "cidadao");

        // Act
        var response = await client.GetAsync("/health/live");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    [Trait("HealthCheck", "API")]
    public async Task Live_SemJwt_DeveRetornarOk()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/live");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    [Trait("OpenAPI", "Swagger")]
    public async Task SwaggerJson_DeveEstarDisponivel()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Rota Leve API", content);
    }
}
