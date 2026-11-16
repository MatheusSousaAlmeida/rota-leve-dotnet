using System.Net;
using System.Net.Http.Json;
using Moq;
using RotaLeve.Api.Application.Dtos;
using RotaLeve.Api.Domain.Enums;
using RotaLeve.Api.Domain.Models;
using RotaLeve.Tests.Integration.Fixtures;

namespace RotaLeve.Tests.Integration.Controllers;

[Collection("API Integration")]
public class LocalidadesControllerTests
{
    private readonly CustomWebApplicationFactory _factory;

    public LocalidadesControllerTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    [Trait("Controller", "Localidades")]
    public async Task Get_ListaVazia_DeveRetornarNoContent()
    {
        // Arrange
        _factory.LocalidadeServiceMock.Reset();
        _factory.LocalidadeServiceMock.Setup(x => x.ListarAsync())
            .ReturnsAsync(new List<LocalidadeResponseDto>());
        using var client = CriarClient("cidadao");

        // Act
        var response = await client.GetAsync("/api/localidades");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());
        _factory.LocalidadeServiceMock.Verify(x => x.ListarAsync(), Times.Once);
    }

    [Fact]
    [Trait("Controller", "Localidades")]
    public async Task Get_PontosExistentes_DeveRetornarOk()
    {
        // Arrange
        _factory.LocalidadeServiceMock.Reset();
        _factory.LocalidadeServiceMock.Setup(x => x.ListarAsync())
            .ReturnsAsync([CriarResponse(1, "Estacao Se")]);
        using var client = CriarClient("cidadao");

        // Act
        var response = await client.GetAsync("/api/localidades");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    [Trait("Controller", "Localidades")]
    public async Task Get_PontoInexistente_DeveRetornarNotFound()
    {
        // Arrange
        _factory.LocalidadeServiceMock.Reset();
        _factory.LocalidadeServiceMock.Setup(x => x.ObterAsync(999))
            .ReturnsAsync(OperationResult<LocalidadeResponseDto>.Failure("Localidade não encontrado.", 404));
        using var client = CriarClient("cidadao");

        // Act
        var response = await client.GetAsync("/api/localidades/999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    [Trait("Controller", "Localidades")]
    public async Task Post_Admin_DeveRetornarCreated()
    {
        // Arrange
        var request = CriarRequest("Estacao Se");
        _factory.LocalidadeServiceMock.Reset();
        _factory.LocalidadeServiceMock.Setup(x => x.CriarAsync(It.IsAny<LocalidadeRequestDto>()))
            .ReturnsAsync(OperationResult<LocalidadeResponseDto>.Success(CriarResponse(1, request.Nome), 201));
        using var client = CriarClient("admin");

        // Act
        var response = await client.PostAsJsonAsync("/api/localidades", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    [Trait("Controller", "Localidades")]
    public async Task Post_Cidadao_DeveRetornarForbidden()
    {
        // Arrange
        using var client = CriarClient("cidadao");
        var request = CriarRequest("Estacao Se");

        // Act
        var response = await client.PostAsJsonAsync("/api/localidades", request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    [Trait("Controller", "Localidades")]
    public async Task Put_AdminPontoExistente_DeveRetornarOk()
    {
        // Arrange
        var request = CriarRequest("Estacao Atualizada");
        _factory.LocalidadeServiceMock.Reset();
        _factory.LocalidadeServiceMock.Setup(x => x.AtualizarAsync(1, It.IsAny<LocalidadeRequestDto>()))
            .ReturnsAsync(OperationResult<LocalidadeResponseDto>.Success(CriarResponse(1, request.Nome)));
        using var client = CriarClient("admin");

        // Act
        var response = await client.PutAsJsonAsync("/api/localidades/1", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    [Trait("Controller", "Localidades")]
    public async Task Delete_AdminPontoExistente_DeveRetornarNoContent()
    {
        // Arrange
        _factory.LocalidadeServiceMock.Reset();
        _factory.LocalidadeServiceMock.Setup(x => x.ExcluirAsync(1))
            .ReturnsAsync(OperationResult<bool>.Success(true, 204));
        using var client = CriarClient("admin");

        // Act
        var response = await client.DeleteAsync("/api/localidades/1");

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

    private static LocalidadeRequestDto CriarRequest(string nome) => new()
    {
        Nome = nome,
        Tipo = TipoLocalidade.EstacaoMetro,
        Descricao = "Acesso principal",
        Endereco = "Sao Paulo - SP",
        Latitude = -23.5505m,
        Longitude = -46.6333m
    };

    private static LocalidadeResponseDto CriarResponse(int id, string nome) =>
        new(id, nome, TipoLocalidade.EstacaoMetro, "Acesso principal", "Sao Paulo - SP", -23.5505m, -46.6333m, true, DateTime.UtcNow);
}
