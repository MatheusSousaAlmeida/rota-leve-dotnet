using Moq;
using RotaLeve.Api.Application.Dtos;
using RotaLeve.Api.Application.Services;
using RotaLeve.Api.Domain.Entities;
using RotaLeve.Api.Domain.Enums;
using RotaLeve.Api.Domain.Interfaces;

namespace RotaLeve.Tests.Unit.Application;

public class LocalidadeServiceTests
{
    private readonly Mock<ILocalidadeRepository> _repository = new();



    [Fact]
    [Trait("Application", "Localidades")]
    public async Task Criar_DeveNormalizarTextosERetornar201()
    {
        // Arrange
        _repository.Setup(x => x.AdicionarAsync(It.IsAny<Localidade>()))
            .ReturnsAsync((Localidade p) => { p.IdLocalidade = 5; return p; });

        // Act
        var result = await new LocalidadeService(_repository.Object).CriarAsync(new LocalidadeRequestDto
        {
            Nome = "  Estacao Se  ",
            Tipo = TipoLocalidade.EstacaoMetro,
            Descricao = "  Acesso principal  ",
            Endereco = "  Praca da Se  ",
            Latitude = -23.5505m,
            Longitude = -46.6333m
        });

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(201, result.StatusCode);
        Assert.Equal("Estacao Se", result.Value!.Nome);
        _repository.Verify(x => x.AdicionarAsync(It.Is<Localidade>(p =>
            p.Nome == "Estacao Se" && p.Descricao == "Acesso principal" && p.Endereco == "Praca da Se")), Times.Once);
    }



    [Fact]
    [Trait("Application", "Localidades")]
    public async Task Excluir_Existente_DeveRetornar200()
    {
        // Arrange
        _repository.Setup(x => x.ExcluirAsync(7)).ReturnsAsync(true);
        // Act
        var result = await new LocalidadeService(_repository.Object).ExcluirAsync(7);
        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.StatusCode);
    }



    private static LocalidadeRequestDto DtoValido() => new()
    {
        Nome = "Terminal",
        Tipo = TipoLocalidade.Terminal,
        Latitude = -23.5m,
        Longitude = -46.6m
    };
}
