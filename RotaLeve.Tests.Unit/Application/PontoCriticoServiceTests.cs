using Moq;
using RotaLeve.Api.Application.Dtos;
using RotaLeve.Api.Application.Services;
using RotaLeve.Api.Domain.Entities;
using RotaLeve.Api.Domain.Enums;
using RotaLeve.Api.Domain.Interfaces;

namespace RotaLeve.Tests.Unit.Application;

public class PontoCriticoServiceTests
{
    private readonly Mock<IPontoCriticoRepository> _repository = new();

    [Fact]
    [Trait("Application", "PontosCriticos")]
    public async Task Obter_Inexistente_DeveRetornar404()
    {
        // Arrange
        _repository.Setup(x => x.ObterAsync(99)).ReturnsAsync((PontoCritico?)null);
        // Act
        var result = await new PontoCriticoService(_repository.Object).ObterAsync(99);
        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    [Trait("Application", "PontosCriticos")]
    public async Task Criar_DeveNormalizarTextosERetornar201()
    {
        // Arrange
        _repository.Setup(x => x.AdicionarAsync(It.IsAny<PontoCritico>()))
            .ReturnsAsync((PontoCritico p) => { p.IdPontoCritico = 5; return p; });

        // Act
        var result = await new PontoCriticoService(_repository.Object).CriarAsync(new PontoCriticoRequestDto
        {
            Nome = "  Estacao Se  ",
            Tipo = TipoPontoCritico.EstacaoMetro,
            Descricao = "  Acesso principal  ",
            Endereco = "  Praca da Se  ",
            Latitude = -23.5505m,
            Longitude = -46.6333m
        });

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(201, result.StatusCode);
        Assert.Equal("Estacao Se", result.Value!.Nome);
        _repository.Verify(x => x.AdicionarAsync(It.Is<PontoCritico>(p =>
            p.Nome == "Estacao Se" && p.Descricao == "Acesso principal" && p.Endereco == "Praca da Se")), Times.Once);
    }

    [Fact]
    [Trait("Application", "PontosCriticos")]
    public async Task Atualizar_Inexistente_DeveRetornar404()
    {
        // Arrange
        _repository.Setup(x => x.ObterAsync(10)).ReturnsAsync((PontoCritico?)null);
        // Act
        var result = await new PontoCriticoService(_repository.Object).AtualizarAsync(10, DtoValido());
        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
        _repository.Verify(x => x.AtualizarAsync(It.IsAny<PontoCritico>()), Times.Never);
    }

    [Fact]
    [Trait("Application", "PontosCriticos")]
    public async Task Excluir_Existente_DeveRetornar204()
    {
        // Arrange
        _repository.Setup(x => x.ExcluirAsync(7)).ReturnsAsync(true);
        // Act
        var result = await new PontoCriticoService(_repository.Object).ExcluirAsync(7);
        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(204, result.StatusCode);
    }

    [Fact]
    [Trait("Application", "PontosCriticos")]
    public async Task Excluir_Inexistente_DeveRetornar404()
    {
        // Arrange
        _repository.Setup(x => x.ExcluirAsync(7)).ReturnsAsync(false);
        // Act
        var result = await new PontoCriticoService(_repository.Object).ExcluirAsync(7);
        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    private static PontoCriticoRequestDto DtoValido() => new()
    {
        Nome = "Terminal",
        Tipo = TipoPontoCritico.Terminal,
        Latitude = -23.5m,
        Longitude = -46.6m
    };
}
