using RotaLeve.Api.Application.Dtos;
using RotaLeve.Api.Application.Interfaces;
using RotaLeve.Api.Domain.Entities;
using RotaLeve.Api.Domain.Interfaces;
using RotaLeve.Api.Domain.Models;
namespace RotaLeve.Api.Application.Services;
public class LocalidadeService(ILocalidadeRepository repository) : ILocalidadeService
{
    public async Task<List<LocalidadeResponseDto>> ListarAsync() => (await repository.ListarAsync()).Select(ToDto).ToList();
    public async Task<OperationResult<LocalidadeResponseDto>> ObterAsync(int id)
    {
        var e = await repository.ObterAsync(id); return e is null ? OperationResult<LocalidadeResponseDto>.Failure("Localidade não encontrado.", 404) : OperationResult<LocalidadeResponseDto>.Success(ToDto(e));
    }
    public async Task<OperationResult<LocalidadeResponseDto>> CriarAsync(LocalidadeRequestDto dto)
    {
        var e = await repository.AdicionarAsync(new Localidade { Nome = dto.Nome.Trim(), Tipo = dto.Tipo, Descricao = dto.Descricao?.Trim(), Endereco = dto.Endereco?.Trim(), Latitude = dto.Latitude, Longitude = dto.Longitude });
        return OperationResult<LocalidadeResponseDto>.Success(ToDto(e), 201);
    }
    public async Task<OperationResult<LocalidadeResponseDto>> AtualizarAsync(int id, LocalidadeRequestDto dto)
    {
        var e = await repository.ObterAsync(id); if (e is null) return OperationResult<LocalidadeResponseDto>.Failure("Localidade não encontrado.", 404);
        e.Nome = dto.Nome.Trim(); e.Tipo = dto.Tipo; e.Descricao = dto.Descricao?.Trim(); e.Endereco = dto.Endereco?.Trim(); e.Latitude = dto.Latitude; e.Longitude = dto.Longitude;
        await repository.AtualizarAsync(e); return OperationResult<LocalidadeResponseDto>.Success(ToDto(e));
    }
    public async Task<OperationResult<bool>> ExcluirAsync(int id) => await repository.ExcluirAsync(id) ? OperationResult<bool>.Success(true, 204) : OperationResult<bool>.Failure("Localidade não encontrado.", 404);
    private static LocalidadeResponseDto ToDto(Localidade e) => new(e.IdLocalidade, e.Nome, e.Tipo, e.Descricao, e.Endereco, e.Latitude, e.Longitude, e.Ativo, e.DataCriacao);
}
