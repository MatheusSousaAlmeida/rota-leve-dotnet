using RotaLeve.Api.Application.Dtos;
using RotaLeve.Api.Domain.Models;

namespace RotaLeve.Api.Application.Interfaces;

public interface IUsuarioService
{
    Task<IEnumerable<UsuarioDetalheResponseDto>> ListarAsync();
    Task<OperationResult<UsuarioDetalheResponseDto>> ObterAsync(int id);
    Task<OperationResult<UsuarioDetalheResponseDto>> CriarAsync(UsuarioCreateRequestDto dto);
    Task<OperationResult<UsuarioDetalheResponseDto>> AtualizarAsync(int id, UsuarioUpdateRequestDto dto);
    Task<OperationResult<bool>> ExcluirAsync(int id);
}
