using System.Net;
using System.Net.Http.Json;
using Moq;
using RotaLeve.Api.Application.Dtos;
using RotaLeve.Api.Domain.Models;
using RotaLeve.Tests.Integration.Fixtures;

namespace RotaLeve.Tests.Integration.Controllers;

[Collection("API Integration")]
public class AuthControllerTests
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthControllerTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    [Trait("Controller", "Auth")]
    public async Task Login_CredenciaisValidas_DeveRetornarOk()
    {
        // Arrange
        var request = new LoginRequestDto { Email = "maria@teste.com", Senha = "Senha@123" };
        var usuario = new UsuarioResponseDto(1, "Maria Silva", request.Email, "cidadao");
        _factory.AuthServiceMock.Reset();
        _factory.AuthServiceMock.Setup(x => x.LoginAsync(It.IsAny<LoginRequestDto>()))
            .ReturnsAsync(OperationResult<AuthResponseDto>.Success(
                new AuthResponseDto("token-valido", DateTime.UtcNow.AddHours(8), usuario)));
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(body);
        Assert.Equal("token-valido", body!.AccessToken);
    }




}
