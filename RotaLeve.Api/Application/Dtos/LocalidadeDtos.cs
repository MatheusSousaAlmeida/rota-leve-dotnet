using System.ComponentModel.DataAnnotations;
using RotaLeve.Api.Domain.Enums;

namespace RotaLeve.Api.Application.Dtos;

public class LocalidadeRequestDto
{
    [Required, StringLength(140)] public string Nome { get; set; } = string.Empty;
    [Required, EnumDataType(typeof(TipoLocalidade))] public TipoLocalidade Tipo { get; set; }
    [StringLength(400)] public string? Descricao { get; set; }
    [StringLength(250)] public string? Endereco { get; set; }
    [Range(-90, 90)] public decimal Latitude { get; set; }
    [Range(-180, 180)] public decimal Longitude { get; set; }
}

public record LocalidadeResponseDto(int Id, string Nome, TipoLocalidade Tipo, string? Descricao, string? Endereco, decimal Latitude, decimal Longitude, bool Ativo, DateTime DataCriacao);
