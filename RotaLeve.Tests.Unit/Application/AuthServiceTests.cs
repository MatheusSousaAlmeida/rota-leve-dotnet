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



    private AuthService CriarService() => new(_usuarios.Object, _hasher.Object, _token.Object);
}
