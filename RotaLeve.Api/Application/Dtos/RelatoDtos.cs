using System.ComponentModel.DataAnnotations;
using RotaLeve.Api.Domain.Enums;

namespace RotaLeve.Api.Application.Dtos;

public class RelatoRequestDto
{
    [Range(1, int.MaxValue)] public int LocalidadeId { get; set; }
    [Required, EnumDataType(typeof(TipoRelato))] public TipoRelato Tipo { get; set; }
    [Required, StringLength(600, MinimumLength = 10)] public string Descricao { get; set; } = string.Empty;
    [Range(-90, 90)] public decimal Latitude { get; set; }
    [Range(-180, 180)] public decimal Longitude { get; set; }
}

public class AlterarStatusRelatoDto
{
    [Required, EnumDataType(typeof(StatusRelato))] public StatusRelato Status { get; set; }
}

public record RelatoResponseDto(
    int Id,
    int UsuarioId,
    int LocalidadeId,
    TipoRelato Tipo,
    string Descricao,
    decimal Latitude,
    decimal Longitude,
    StatusRelato Status,
    int QuantidadeConfirmacoes,
    DateTime DataCriacao,
    DateTime? DataUltimaConfirmacao,
    DateTime? DataResolucao);

public record ConfirmacaoResponseDto(int Id, int RelatoId, int UsuarioId, DateTime DataConfirmacao);
