using RotaLeve.Api.Application.Dtos;
using RotaLeve.Api.Domain.Models;
namespace RotaLeve.Api.Application.Interfaces;
public interface ILocalidadeService
{
    Task<List<LocalidadeResponseDto>> ListarAsync();
    Task<OperationResult<LocalidadeResponseDto>> ObterAsync(int id);
    Task<OperationResult<LocalidadeResponseDto>> CriarAsync(LocalidadeRequestDto dto);
    Task<OperationResult<LocalidadeResponseDto>> AtualizarAsync(int id, LocalidadeRequestDto dto);
    Task<OperationResult<bool>> ExcluirAsync(int id);
}
