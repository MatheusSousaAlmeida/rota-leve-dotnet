using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using RotaLeve.Api.Domain.Entities;
using RotaLeve.Api.Domain.Enums;
using RotaLeve.Api.Infrastructure.Security;

namespace RotaLeve.Tests.Unit.Security;

public class JwtTokenServiceTests
{
    [Fact]
    [Trait("Security", "JWT")]
    public void GerarToken_Cidadao_DeveConterClaimsEsperadas()
    {
        // Arrange
        var service = CriarService();
        var usuario = new Usuario
        {
            IdUsuario = 12,
            Nome = "Maria Teste",
            Email = "maria@teste.com",
            TipoUsuario = TipoUsuario.Cidadao
        };

        // Act
        var (token, expires) = service.GerarToken(usuario);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        // Assert
        Assert.Contains(jwt.Claims, c => c.Value == "12");
        Assert.Contains(jwt.Claims, c => c.Value == "Maria Teste");
        Assert.Contains(jwt.Claims, c => c.Value == "maria@teste.com");
        Assert.Contains(jwt.Claims, c => c.Value == "cidadao");
        Assert.True(expires > DateTime.UtcNow.AddHours(7));
    }

    [Fact]
    [Trait("Security", "JWT")]
    public void GerarToken_Admin_DeveConterRoleAdmin()
    {
        // Arrange
        var service = CriarService();
        var usuario = new Usuario
        {
            IdUsuario = 20,
            Nome = "Admin",
            Email = "admin@teste.com",
            TipoUsuario = TipoUsuario.Admin
        };

        // Act
        var (token, _) = service.GerarToken(usuario);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        // Assert
        Assert.Contains(jwt.Claims, c => c.Value == "admin");
    }

    [Fact]
    [Trait("Security", "JWT")]
    public void GerarToken_SecretkeyDe30Caracteres_DeveGerarTokenComSucesso()
    {
        // Arrange
        var service = CriarService();
        var usuario = new Usuario
        {
            IdUsuario = 1,
            Nome = "Teste",
            Email = "teste@teste.com",
            TipoUsuario = TipoUsuario.Cidadao
        };

        // Act
        var (token, _) = service.GerarToken(usuario);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    [Trait("Security", "JWT")]
    public void GerarToken_SemSecretkey_DeveLancarExcecao()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        var service = new JwtTokenService(config);
        var usuario = new Usuario { IdUsuario = 1, Nome = "Teste", Email = "teste@teste.com" };

        // Act
        var exception = Record.Exception(() => service.GerarToken(usuario));

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
    }

    private static JwtTokenService CriarService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Secretkey"] = "t7w0soRP8sIIHWl2P0rbZoZEX7f2rm"
            })
            .Build();

        return new JwtTokenService(config);
    }
}
