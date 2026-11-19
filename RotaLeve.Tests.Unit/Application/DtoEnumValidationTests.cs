using System.ComponentModel.DataAnnotations;
using RotaLeve.Api.Application.Dtos;
using RotaLeve.Api.Domain.Enums;

namespace RotaLeve.Tests.Unit.Application;

public class DtoEnumValidationTests
{
    [Fact]
    public void LocalidadeRequest_TipoNaoDefinido_DeveSerInvalido()
    {
        // Arrange
        var dto = new LocalidadeRequestDto { Nome = "Local de teste", Tipo = (TipoLocalidade)99 };

        // Act
        var erros = Validar(dto);

        // Assert
        Assert.Contains(erros, x => x.MemberNames.Contains(nameof(LocalidadeRequestDto.Tipo)));
    }

    [Fact]
    public void RelatoRequest_TipoNaoDefinido_DeveSerInvalido()
    {
        // Arrange
        var dto = new RelatoRequestDto
        {
            LocalidadeId = 1,
            Tipo = (TipoRelato)99,
            Descricao = "Descricao valida de um relato"
        };

        // Act
        var erros = Validar(dto);

        // Assert
        Assert.Contains(erros, x => x.MemberNames.Contains(nameof(RelatoRequestDto.Tipo)));
    }

    [Fact]
    public void AlterarStatusRelato_StatusNaoDefinido_DeveSerInvalido()
    {
        // Arrange
        var dto = new AlterarStatusRelatoDto { Status = (StatusRelato)99 };

        // Act
        var erros = Validar(dto);

        // Assert
        Assert.Contains(erros, x => x.MemberNames.Contains(nameof(AlterarStatusRelatoDto.Status)));
    }

    private static List<ValidationResult> Validar(object dto)
    {
        var erros = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), erros, validateAllProperties: true);
        return erros;
    }
}
