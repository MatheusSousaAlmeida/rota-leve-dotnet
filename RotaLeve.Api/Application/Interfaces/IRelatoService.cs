using RotaLeve.Api.Application.Dtos;
using RotaLeve.Api.Domain.Enums;
using RotaLeve.Api.Domain.Models;
namespace RotaLeve.Api.Application.Interfaces;
public interface IRelatoService
{
    Task<PaginacaoResponseDto<RelatoResponseDto>> ListarPaginadoAsync(int pagina, int tamanhoPagina);
    Task<List<RelatoResponseDto>> ListarAsync();
    Task<OperationResult<RelatoResponseDto>> ObterAsync(int id);
    Task<OperationResult<RelatoResponseDto>> CriarAsync(int usuarioId, RelatoRequestDto dto, string chaveIdempotencia);
    Task<OperationResult<RelatoResponseDto>> AtualizarAsync(int id, int usuarioId, bool admin, RelatoRequestDto dto);
    Task<OperationResult<bool>> ExcluirAsync(int id, int usuarioId, bool admin);
    Task<OperationResult<ConfirmacaoResponseDto>> ConfirmarAsync(int relatoId, int usuarioId);
    Task<OperationResult<bool>> RemoverConfirmacaoAsync(int relatoId, int usuarioId);
    Task<OperationResult<RelatoResponseDto>> AlterarStatusAsync(int relatoId, StatusRelato status);
}
