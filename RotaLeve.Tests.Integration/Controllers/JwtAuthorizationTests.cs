using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RotaLeve.Api.Infrastructure.Security;

namespace RotaLeve.Tests.Integration.Controllers;

public class JwtAuthorizationTests
{
    private const string Secret = "t7w0soRP8sIIHWl2P0rbZoZEX7f2rm";

    [Fact]
    [Trait("Security", "JWT")]
    public async Task GetRelatos_SemToken_DeveRetornarUnauthorized()
    {
        // Arrange
        await using var factory = CriarFactory();
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/relatos");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    [Trait("Security", "JWT")]
    public async Task PostPontoCritico_TokenCidadao_DeveRetornarForbidden()
    {
        // Arrange
        await using var factory = CriarFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GerarToken("cidadao"));

        // Act
        var response = await client.PostAsync("/api/pontos-criticos", null);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    [Trait("Security", "JWT")]
    public async Task PatchStatus_TokenCidadao_DeveRetornarForbidden()
    {
        // Arrange
        await using var factory = CriarFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GerarToken("cidadao"));

        // Act
        var response = await client.PatchAsync("/api/relatos/1/status", null);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CriarFactory()
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Secretkey"] = Secret
                    });
                });
            });
    }

    private static string GerarToken(string role)
    {
        var token = new JwtSecurityToken(
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "teste@rotaleve.local"),
                new Claim(ClaimTypes.Role, role)
            ],
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(JwtKeyHelper.Create(Secret), SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
