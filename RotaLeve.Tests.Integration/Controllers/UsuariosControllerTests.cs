using System.Net;
using System.Net.Http.Json;
using Moq;
using RotaLeve.Api.Application.Dtos;
using RotaLeve.Api.Domain.Models;
using RotaLeve.Tests.Integration.Fixtures;

namespace RotaLeve.Tests.Integration.Controllers;

[Collection("API Integration")]
public class UsuariosControllerTests
{
    private readonly CustomWebApplicationFactory _factory;

    public UsuariosControllerTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    [Trait("Controller", "Usuarios")]
    public async Task Get_Admin_DeveRetornarOk()
    {
        // Arrange
        _factory.UsuarioServiceMock.Reset();
        _factory.UsuarioServiceMock.Setup(x => x.ListarAsync()).ReturnsAsync([CriarResponse(1, "cidadao")]);
        using var client = CriarClient("admin");

        // Act
        var response = await client.GetAsync("/api/usuarios");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    [Trait("Controller", "Usuarios")]
    public async Task Get_Cidadao_DeveRetornarForbidden()
    {
        // Arrange
        using var client = CriarClient("cidadao");

        // Act
        var response = await client.GetAsync("/api/usuarios");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    [Trait("Controller", "Usuarios")]
    public async Task GetPorId_UsuarioInexistente_DeveRetornarNotFound()
    {
        // Arrange
        _factory.UsuarioServiceMock.Reset();
        _factory.UsuarioServiceMock.Setup(x => x.ObterAsync(999))
            .ReturnsAsync(OperationResult<UsuarioDetalheResponseDto>.Failure("Usuário não encontrado.", 404));
        using var client = CriarClient("admin");

        // Act
        var response = await client.GetAsync("/api/usuarios/999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    [Trait("Controller", "Usuarios")]
    public async Task Post_SemTokenCidadao_DeveRetornarCreated()
    {
        // Arrange
        var request = CriarCreateRequest("cidadao");
        _factory.UsuarioServiceMock.Reset();
        _factory.UsuarioServiceMock.Setup(x => x.CriarAsync(It.IsAny<UsuarioCreateRequestDto>()))
            .ReturnsAsync(OperationResult<UsuarioDetalheResponseDto>.Success(CriarResponse(1, "cidadao"), 201));
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/usuarios", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UsuarioDetalheResponseDto>();
        Assert.NotNull(body);
        Assert.Equal("cidadao", body.Perfil);
        _factory.UsuarioServiceMock.Verify(x => x.CriarAsync(It.IsAny<UsuarioCreateRequestDto>()), Times.Once);
    }

    [Fact]
    [Trait("Controller", "Usuarios")]
    public async Task Post_SemTokenAdmin_DeveRetornarCreated()
    {
        // Arrange
        var request = CriarCreateRequest("admin");
        _factory.UsuarioServiceMock.Reset();
        _factory.UsuarioServiceMock.Setup(x => x.CriarAsync(It.IsAny<UsuarioCreateRequestDto>()))
            .ReturnsAsync(OperationResult<UsuarioDetalheResponseDto>.Success(CriarResponse(2, "admin"), 201));
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/usuarios", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UsuarioDetalheResponseDto>();
        Assert.NotNull(body);
        Assert.Equal("admin", body.Perfil);
    }

    [Fact]
    [Trait("Controller", "Usuarios")]
    public async Task Post_PerfilInvalido_DeveRetornarBadRequestSemChamarService()
    {
        // Arrange
        var request = CriarCreateRequest("administrador");
        _factory.UsuarioServiceMock.Reset();
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/usuarios", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        _factory.UsuarioServiceMock.Verify(
            x => x.CriarAsync(It.IsAny<UsuarioCreateRequestDto>()),
            Times.Never);
    }

    [Fact]
    [Trait("Controller", "Usuarios")]
    public async Task Post_EmailDuplicado_DeveRetornarConflict()
    {
        // Arrange
        var request = CriarCreateRequest("cidadao");
        _factory.UsuarioServiceMock.Reset();
        _factory.UsuarioServiceMock.Setup(x => x.CriarAsync(It.IsAny<UsuarioCreateRequestDto>()))
            .ReturnsAsync(OperationResult<UsuarioDetalheResponseDto>.Failure("E-mail já cadastrado.", 409));
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/usuarios", request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    [Trait("Controller", "Usuarios")]
    public async Task Put_AdminPodeAlterarPerfilParaAdmin_DeveRetornarOk()
    {
        // Arrange
        var request = new UsuarioUpdateRequestDto
        {
            Nome = "Usuario Atualizado",
            Email = "usuario@teste.com",
            Perfil = "admin",
            Ativo = true
        };
        _factory.UsuarioServiceMock.Reset();
        _factory.UsuarioServiceMock.Setup(x => x.AtualizarAsync(1, It.IsAny<UsuarioUpdateRequestDto>()))
            .ReturnsAsync(OperationResult<UsuarioDetalheResponseDto>.Success(CriarResponse(1, "admin")));
        using var client = CriarClient("admin");

        // Act
        var response = await client.PutAsJsonAsync("/api/usuarios/1", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    [Trait("Controller", "Usuarios")]
    public async Task Delete_AdminUsuarioExistente_DeveRetornarNoContent()
    {
        // Arrange
        _factory.UsuarioServiceMock.Reset();
        _factory.UsuarioServiceMock.Setup(x => x.ExcluirAsync(1))
            .ReturnsAsync(OperationResult<bool>.Success(true, 204));
        using var client = CriarClient("admin");

        // Act
        var response = await client.DeleteAsync("/api/usuarios/1");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private HttpClient CriarClient(string role)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        client.DefaultRequestHeaders.Add("X-Test-UserId", "1");
        return client;
    }

    private static UsuarioCreateRequestDto CriarCreateRequest(string perfil) => new()
    {
        Nome = "Usuario Teste",
        Email = "usuario@teste.com",
        Senha = "Senha@123",
        Perfil = perfil
    };

    private static UsuarioDetalheResponseDto CriarResponse(int id, string perfil) =>
        new(id, "Usuario Teste", "usuario@teste.com", perfil, true, DateTime.UtcNow);
}
