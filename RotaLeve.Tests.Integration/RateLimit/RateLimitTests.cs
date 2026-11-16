using System.Net;
using System.Net.Http.Json;
using Moq;
using RotaLeve.Api.Application.Dtos;
using RotaLeve.Api.Domain.Enums;
using RotaLeve.Api.Domain.Models;
using RotaLeve.Tests.Integration.Fixtures;

namespace RotaLeve.Tests.Integration.RateLimit;

[Collection("API Integration")]
public class RateLimitTests
{
    private readonly CustomWebApplicationFactory _factory;

    public RateLimitTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    [Trait("RateLimit", "Relatos")]
    public async Task CriarRelato_AposCincoRequisicoes_DeveRetornar429()
    {
        // Arrange
        _factory.RelatoServiceMock.Reset();
        _factory.RelatoServiceMock.Setup(x => x.CriarAsync(50, It.IsAny<RelatoRequestDto>()))
            .ReturnsAsync(OperationResult<RelatoResponseDto>.Success(CriarRelatoResponse(), 201));
        using var client = CriarClient(50);
        var request = CriarRelatoRequest();

        // Act
        var respostas = new List<HttpResponseMessage>();
        for (var i = 0; i < 6; i++)
            respostas.Add(await client.PostAsJsonAsync("/api/relatos", request));

        // Assert
        Assert.All(respostas.Take(5), response => Assert.Equal(HttpStatusCode.Created, response.StatusCode));
        Assert.Equal(HttpStatusCode.TooManyRequests, respostas[5].StatusCode);
    }

    [Fact]
    [Trait("RateLimit", "Confirmacoes")]
    public async Task ConfirmarRelato_AposVinteRequisicoes_DeveRetornar429()
    {
        // Arrange
        _factory.RelatoServiceMock.Reset();
        _factory.RelatoServiceMock.Setup(x => x.ConfirmarAsync(10, 60))
            .ReturnsAsync(OperationResult<ConfirmacaoResponseDto>.Failure("Já confirmado.", 409));
        using var client = CriarClient(60);

        // Act
        var respostas = new List<HttpResponseMessage>();
        for (var i = 0; i < 21; i++)
            respostas.Add(await client.PostAsync("/api/relatos/10/confirmacoes", null));

        // Assert
        Assert.All(respostas.Take(20), response => Assert.Equal(HttpStatusCode.Conflict, response.StatusCode));
        Assert.Equal(HttpStatusCode.TooManyRequests, respostas[20].StatusCode);
    }

    private HttpClient CriarClient(int userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", "cidadao");
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
        return client;
    }

    private static RelatoRequestDto CriarRelatoRequest() => new()
    {
        LocalidadeId = 1,
        Tipo = TipoRelato.ElevadorIndisponivel,
        Descricao = "Elevador indisponivel para teste de rate limit",
        Latitude = -23.5m,
        Longitude = -46.6m
    };

    private static RelatoResponseDto CriarRelatoResponse() =>
        new(10, 50, 1, TipoRelato.ElevadorIndisponivel, "Elevador indisponivel para teste de rate limit",
            -23.5m, -46.6m, StatusRelato.Novo, 0, DateTime.UtcNow, null, null);
}
