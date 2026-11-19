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
    public async Task Get_PaginacaoValida_DeveRetornarMetadadosEParametrosCorretos()
    {
        // Arrange
        _factory.LocalidadeServiceMock.Reset();
        var item = CriarResponse(12, "Praça Central");
        _factory.LocalidadeServiceMock
            .Setup(x => x.ListarPaginadoAsync(2, 5))
            .ReturnsAsync(new PaginacaoResponseDto<LocalidadeResponseDto>(
                2, 5, 12, 3, new List<LocalidadeResponseDto> { item }));
        using var client = CriarClient("admin");

        // Act
        var response = await client.GetAsync("/api/localidades?pagina=2&tamanhoPagina=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PaginacaoResponseDto<LocalidadeResponseDto>>();
        Assert.NotNull(body);
        Assert.Equal(2, body.Pagina);
        Assert.Equal(5, body.TamanhoPagina);
        Assert.Equal(12, body.TotalRegistros);
        Assert.Equal(3, body.TotalPaginas);
        Assert.Single(body.Dados);
        _factory.LocalidadeServiceMock.Verify(x => x.ListarPaginadoAsync(2, 5), Times.Once);
    }

    [Fact]
    [Trait("Controller", "Localidades")]
    public async Task Get_ListaVazia_DeveRetornarNoContent()
    {
        // Arrange
        _factory.LocalidadeServiceMock.Reset();
        _factory.LocalidadeServiceMock.Setup(x => x.ListarPaginadoAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PaginacaoResponseDto<LocalidadeResponseDto>(1, 10, 0, 0, new List<LocalidadeResponseDto>()));
        using var client = CriarClient("cidadao");

        // Act
        var response = await client.GetAsync("/api/localidades");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());
        _factory.LocalidadeServiceMock.Verify(x => x.ListarPaginadoAsync(1, 10), Times.Once);
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
    public async Task Delete_AdminPontoExistente_DeveRetornarOk()
    {
        // Arrange
        _factory.LocalidadeServiceMock.Reset();
        _factory.LocalidadeServiceMock.Setup(x => x.ExcluirAsync(1))
            .ReturnsAsync(OperationResult<bool>.Success(true, 200));
        using var client = CriarClient("admin");

        // Act
        var response = await client.DeleteAsync("/api/localidades/1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, System.Text.Json.JsonElement>>();
        Assert.NotNull(body);
        Assert.Equal(1, body["localidadeId"].GetInt32());
        Assert.Equal("Localidade desativada com sucesso.", body["mensagem"].GetString());
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
