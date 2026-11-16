using System.Net;
using System.Net.Http.Json;
using Moq;
using RotaLeve.Api.Application.Dtos;
using RotaLeve.Api.Domain.Enums;
using RotaLeve.Api.Domain.Models;
using RotaLeve.Tests.Integration.Fixtures;

namespace RotaLeve.Tests.Integration.Controllers;

[Collection("API Integration")]
public class RelatosControllerTests
{
    private readonly CustomWebApplicationFactory _factory;

    public RelatosControllerTests(CustomWebApplicationFactory factory) => _factory = factory;



    [Fact]
    [Trait("Controller", "Relatos")]
    public async Task Get_ListaVazia_DeveRetornarNoContent()
    {
        // Arrange
        _factory.RelatoServiceMock.Reset();
        _factory.RelatoServiceMock.Setup(x => x.ListarAsync())
            .ReturnsAsync(new List<RelatoResponseDto>());
        using var client = CriarClient("cidadao", 1);

        // Act
        var response = await client.GetAsync("/api/relatos");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());
        _factory.RelatoServiceMock.Verify(x => x.ListarAsync(), Times.Once);
    }



    [Fact]
    [Trait("Controller", "Relatos")]
    public async Task Post_RelatoValido_DeveRetornarCreated()
    {
        // Arrange
        var request = CriarRequest();
        _factory.RelatoServiceMock.Reset();
        _factory.RelatoServiceMock.Setup(x => x.CriarAsync(1, It.IsAny<RelatoRequestDto>()))
            .ReturnsAsync(OperationResult<RelatoResponseDto>.Success(CriarResponse(), 201));
        using var client = CriarClient("cidadao", 1);

        // Act
        var response = await client.PostAsJsonAsync("/api/relatos", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        _factory.RelatoServiceMock.Verify(x => x.CriarAsync(1, It.IsAny<RelatoRequestDto>()), Times.Once);
    }

    [Fact]
    [Trait("Controller", "Relatos")]
    public async Task Post_DescricaoCurta_DeveRetornarBadRequest()
    {
        // Arrange
        var request = CriarRequest();
        request.Descricao = "curta";
        using var client = CriarClient("cidadao", 1);

        // Act
        var response = await client.PostAsJsonAsync("/api/relatos", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }







    [Fact]
    [Trait("Controller", "Relatos")]
    public async Task Confirmar_Duplicada_DeveRetornarConflict()
    {
        // Arrange
        _factory.RelatoServiceMock.Reset();
        _factory.RelatoServiceMock.Setup(x => x.ConfirmarAsync(10, 2))
            .ReturnsAsync(OperationResult<ConfirmacaoResponseDto>.Failure("Relato já confirmado por este usuário.", 409));
        using var client = CriarClient("cidadao", 2);

        // Act
        var response = await client.PostAsync("/api/relatos/10/confirmacoes", null);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }



    [Fact]
    [Trait("Controller", "Relatos")]
    public async Task PatchStatus_Admin_DeveRetornarOk()
    {
        // Arrange
        _factory.RelatoServiceMock.Reset();
        _factory.RelatoServiceMock.Setup(x => x.AlterarStatusAsync(10, StatusRelato.Resolvido))
            .ReturnsAsync(OperationResult<RelatoResponseDto>.Success(CriarResponse(10, StatusRelato.Resolvido)));
        using var client = CriarClient("admin", 99);
        var request = new AlterarStatusRelatoDto { Status = StatusRelato.Resolvido };

        // Act
        var response = await client.PatchAsJsonAsync("/api/relatos/10/status", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }



    private HttpClient CriarClient(string role, int userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
        return client;
    }

    private static RelatoRequestDto CriarRequest() => new()
    {
        LocalidadeId = 1,
        Tipo = TipoRelato.ElevadorIndisponivel,
        Descricao = "Elevador principal indisponivel",
        Latitude = -23.5505m,
        Longitude = -46.6333m
    };

    private static RelatoResponseDto CriarResponse(int id = 10, StatusRelato status = StatusRelato.Novo) =>
        new(id, 1, 1, TipoRelato.ElevadorIndisponivel, "Elevador principal indisponivel", -23.5505m, -46.6333m,
            status, 0, DateTime.UtcNow, null, status == StatusRelato.Resolvido ? DateTime.UtcNow : null);
}
