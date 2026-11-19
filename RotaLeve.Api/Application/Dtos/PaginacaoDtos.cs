using System.ComponentModel.DataAnnotations;

namespace RotaLeve.Api.Application.Dtos;

/// <summary>Parâmetros de paginação de listagens.</summary>
public class PaginacaoRequestDto
{
    /// <summary>Número da página, iniciado em 1 (máximo 1000000).</summary>
    [Range(1, 1000000, ErrorMessage = "A página deve estar entre 1 e 1000000.")]
    public int Pagina { get; set; } = 1;

    /// <summary>Quantidade de registros por página (de 1 a 100).</summary>
    [Range(1, 100, ErrorMessage = "O tamanho da página deve estar entre 1 e 100.")]
    public int TamanhoPagina { get; set; } = 10;
}

/// <summary>Resultado de uma consulta paginada.</summary>
public record PaginacaoResponseDto<T>(
    int Pagina,
    int TamanhoPagina,
    int TotalRegistros,
    int TotalPaginas,
    IReadOnlyList<T> Dados);
