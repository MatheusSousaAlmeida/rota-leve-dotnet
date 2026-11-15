using System.Net;
using Moq;
using RotaLeve.Api.Application.Dtos;
using RotaLeve.Tests.Integration.Fixtures;

namespace RotaLeve.Tests.Integration.RateLimit;

public class RateLimitListagensTests
{
    [Theory]
    [InlineData("/api/usuarios")]
    [InlineData("/api/relatos")]
    [InlineData("/api/pontos-criticos")]
    [Trait("RateLimit", "Listagens")]
    public async Task Listar_AposCincoRequisicoes_DeveRetornar429(string rota)
    {
        // Arrange
        // Factory isolada para que testes de outros endpoints não consumam a mesma janela de rate limit.
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", "admin");
        client.DefaultRequestHeaders.Add("X-Test-UserId", "8301");
        factory.UsuarioServiceMock.Setup(x => x.ListarAsync())
            .ReturnsAsync(new List<UsuarioDetalheResponseDto>());
        factory.RelatoServiceMock.Setup(x => x.ListarAsync())
            .ReturnsAsync(new List<RelatoResponseDto>());
        factory.PontoCriticoServiceMock.Setup(x => x.ListarAsync())
            .ReturnsAsync(new List<PontoCriticoResponseDto>());

        // Act
        var respostas = new List<HttpResponseMessage>();
        for (var i = 0; i < 6; i++)
            respostas.Add(await client.GetAsync(rota));

        // Assert
        Assert.All(respostas.Take(5), resposta => Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode));
        Assert.Equal(HttpStatusCode.TooManyRequests, respostas[5].StatusCode);
    }
}
