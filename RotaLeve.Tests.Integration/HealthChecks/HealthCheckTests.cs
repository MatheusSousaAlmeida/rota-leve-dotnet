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
    public async Task Live_SemJwt_DeveRetornarOk()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/live");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }


}
