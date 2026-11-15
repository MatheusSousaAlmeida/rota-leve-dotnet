using Moq;
using RotaLeve.Api.Application.Dtos;
using RotaLeve.Api.Application.Services;
using RotaLeve.Api.Domain.Entities;
using RotaLeve.Api.Domain.Enums;
using RotaLeve.Api.Domain.Interfaces;
using RotaLeve.Api.Infrastructure.Security;

namespace RotaLeve.Tests.Unit.Application;

public class AuthServiceTests
{
    private readonly Mock<IUsuarioRepository> _usuarios = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IJwtTokenService> _token = new();

    [Fact]
    [Trait("Application", "Auth")]
    public async Task Login_UsuarioInexistente_DeveRetornar401()
    {
        // Arrange
        _usuarios.Setup(x => x.ObterPorEmailAsync("naoexiste@fiap.com.br")).ReturnsAsync((Usuario?)null);
        var service = CriarService();

        // Act
        var result = await service.LoginAsync(new LoginRequestDto
        {
            Email = "naoexiste@fiap.com.br",
            Senha = "Senha@123"
        });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(401, result.StatusCode);
        _token.Verify(x => x.GerarToken(It.IsAny<Usuario>()), Times.Never);
    }

    [Fact]
    [Trait("Application", "Auth")]
    public async Task Login_UsuarioInativo_DeveRetornar401()
    {
        // Arrange
        _usuarios.Setup(x => x.ObterPorEmailAsync("inativo@fiap.com.br"))
            .ReturnsAsync(new Usuario { Email = "inativo@fiap.com.br", Ativo = false, SenhaHash = "hash" });
        var service = CriarService();

        // Act
        var result = await service.LoginAsync(new LoginRequestDto
        {
            Email = "inativo@fiap.com.br",
            Senha = "Senha@123"
        });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(401, result.StatusCode);
        _hasher.Verify(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    [Trait("Application", "Auth")]
    public async Task Login_SenhaIncorreta_DeveRetornar401()
    {
        // Arrange
        var usuario = new Usuario
        {
            IdUsuario = 3,
            Nome = "Joao",
            Email = "joao@fiap.com.br",
            Ativo = true,
            SenhaHash = "hash"
        };
        _usuarios.Setup(x => x.ObterPorEmailAsync(usuario.Email)).ReturnsAsync(usuario);
        _hasher.Setup(x => x.Verify("errada", "hash")).Returns(false);
        var service = CriarService();

        // Act
        var result = await service.LoginAsync(new LoginRequestDto { Email = usuario.Email, Senha = "errada" });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(401, result.StatusCode);
        _token.Verify(x => x.GerarToken(It.IsAny<Usuario>()), Times.Never);
    }

    [Fact]
    [Trait("Application", "Auth")]
    public async Task Login_CidadaoValido_DeveRetornarTokenEPerfilCidadao()
    {
        // Arrange
        var usuario = new Usuario
        {
            IdUsuario = 4,
            Nome = "Maria",
            Email = "maria@fiap.com.br",
            Ativo = true,
            SenhaHash = "hash",
            TipoUsuario = TipoUsuario.Cidadao
        };
        var expiracao = DateTime.UtcNow.AddHours(8);
        _usuarios.Setup(x => x.ObterPorEmailAsync(usuario.Email)).ReturnsAsync(usuario);
        _hasher.Setup(x => x.Verify("Senha@123", "hash")).Returns(true);
        _token.Setup(x => x.GerarToken(usuario)).Returns(("jwt-valido", expiracao));
        var service = CriarService();

        // Act
        var result = await service.LoginAsync(new LoginRequestDto { Email = usuario.Email, Senha = "Senha@123" });

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("jwt-valido", result.Value!.AccessToken);
        Assert.Equal("cidadao", result.Value.Usuario.Perfil);
        Assert.Equal(expiracao, result.Value.ExpiresAtUtc);
    }

    [Fact]
    [Trait("Application", "Auth")]
    public async Task Login_AdminValido_DeveRetornarTokenEPerfilAdmin()
    {
        // Arrange
        var usuario = new Usuario
        {
            IdUsuario = 5,
            Nome = "Admin",
            Email = "admin@fiap.com.br",
            Ativo = true,
            SenhaHash = "hash",
            TipoUsuario = TipoUsuario.Admin
        };
        var expiracao = DateTime.UtcNow.AddHours(8);
        _usuarios.Setup(x => x.ObterPorEmailAsync(usuario.Email)).ReturnsAsync(usuario);
        _hasher.Setup(x => x.Verify("Senha@123", "hash")).Returns(true);
        _token.Setup(x => x.GerarToken(usuario)).Returns(("jwt-valido", expiracao));
        var service = CriarService();

        // Act
        var result = await service.LoginAsync(new LoginRequestDto { Email = usuario.Email, Senha = "Senha@123" });

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("jwt-valido", result.Value!.AccessToken);
        Assert.Equal("admin", result.Value.Usuario.Perfil);
        Assert.Equal(expiracao, result.Value.ExpiresAtUtc);
    }

    [Fact]
    [Trait("Application", "Auth")]
    public async Task Login_EmailComMaiusculasEEspacos_DeveNormalizarAntesDaConsulta()
    {
        // Arrange
        var usuario = new Usuario
        {
            IdUsuario = 6,
            Nome = "Maria",
            Email = "maria@fiap.com.br",
            Ativo = true,
            SenhaHash = "hash",
            TipoUsuario = TipoUsuario.Cidadao
        };
        _usuarios.Setup(x => x.ObterPorEmailAsync("maria@fiap.com.br")).ReturnsAsync(usuario);
        _hasher.Setup(x => x.Verify("Senha@123", "hash")).Returns(true);
        _token.Setup(x => x.GerarToken(usuario)).Returns(("jwt", DateTime.UtcNow.AddHours(8)));
        var service = CriarService();

        // Act
        var result = await service.LoginAsync(new LoginRequestDto
        {
            Email = "  MARIA@FIAP.COM.BR  ",
            Senha = "Senha@123"
        });

        // Assert
        Assert.True(result.IsSuccess);
        _usuarios.Verify(x => x.ObterPorEmailAsync("maria@fiap.com.br"), Times.Once);
    }

    private AuthService CriarService() => new(_usuarios.Object, _hasher.Object, _token.Object);
}
