using RotaLeve.Api.Application.Dtos;
using RotaLeve.Api.Domain.Models;
namespace RotaLeve.Api.Application.Interfaces;
public interface IPontoCriticoService
{
    Task<List<PontoCriticoResponseDto>> ListarAsync();
    Task<OperationResult<PontoCriticoResponseDto>> ObterAsync(int id);
    Task<OperationResult<PontoCriticoResponseDto>> CriarAsync(PontoCriticoRequestDto dto);
    Task<OperationResult<PontoCriticoResponseDto>> AtualizarAsync(int id, PontoCriticoRequestDto dto);
    Task<OperationResult<bool>> ExcluirAsync(int id);
}
