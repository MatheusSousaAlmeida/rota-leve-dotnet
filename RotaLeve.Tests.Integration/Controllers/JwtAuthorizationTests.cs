using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Moq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RotaLeve.Api.Application.Dtos;
using RotaLeve.Api.Application.Interfaces;
using RotaLeve.Api.Domain.Entities;
using RotaLeve.Api.Domain.Enums;
using RotaLeve.Api.Domain.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RotaLeve.Api.Infrastructure.Security;
using RotaLeve.Tests.Integration.Configuration;

namespace RotaLeve.Tests.Integration.Controllers;

public class JwtAuthorizationTests
{
    private static string Secret => JwtTestConfiguration.GetSecretKey();

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
    public async Task GetRelatos_TokenInvalido_DeveRetornarUnauthorized()
    {
        // Arrange
        await using var factory = CriarFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "token-invalido");

        // Act
        var response = await client.GetAsync("/api/relatos");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    [Trait("Security", "JWT")]
    public async Task PostLocalidade_TokenCidadao_DeveRetornarForbidden()
    {
        // Arrange
        await using var factory = CriarFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GerarToken("cidadao"));

        // Act
        var response = await client.PostAsync("/api/localidades", null);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }




    [Fact]
    [Trait("Security", "JWT")]
    public async Task Login_TokenEmitido_DeveAcessarRotaProtegida()
    {
        // Arrange: login simulado, mas geração e validação JWT reais (sem banco Oracle).
        var usuario = CriarUsuario(TipoUsuario.Admin);
        var tokenService = new JwtTokenService(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Secretkey"] = Secret }).Build());
        var jwt = tokenService.GerarToken(usuario);
        var auth = new Mock<IAuthService>();
        auth.Setup(s => s.LoginAsync(It.IsAny<LoginRequestDto>()))
            .ReturnsAsync(OperationResult<AuthResponseDto>.Success(
                new AuthResponseDto(jwt.Token, jwt.ExpiresAtUtc,
                    new UsuarioResponseDto(usuario.IdUsuario, usuario.Nome, usuario.Email, "admin"))));
        var relatos = new Mock<IRelatoService>();
        relatos.Setup(s => s.ListarPaginadoAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(new PaginacaoResponseDto<RelatoResponseDto>(1, 10, 0, 0, new List<RelatoResponseDto>()));
        await using var factory = CriarFactoryComServicos(auth.Object, relatos.Object);
        using var client = factory.CreateClient();

        // Act: exatamente o fluxo utilizado no Swagger.
        var login = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequestDto { Email = usuario.Email, Senha = "Senha@123" });
        var payload = await login.Content.ReadFromJsonAsync<AuthResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload!.AccessToken);
        var response = await client.GetAsync("/api/relatos");

        // Assert
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.NotEmpty(payload.AccessToken);
        // O mock retorna uma lista vazia; após autenticar, o controller responde 204.
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }









    private static Usuario CriarUsuario(TipoUsuario role) => new()
    {
        IdUsuario = 42,
        Nome = "Administrador Teste",
        Email = "admin@rotaleve.local",
        TipoUsuario = role,
        Ativo = true
    };

    private static WebApplicationFactory<Program> CriarFactoryComServicos(
        IAuthService auth, IRelatoService relatos)
        => CriarFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IAuthService>();
                services.RemoveAll<IRelatoService>();
                services.AddSingleton(auth);
                services.AddSingleton(relatos);
            });
        });

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
