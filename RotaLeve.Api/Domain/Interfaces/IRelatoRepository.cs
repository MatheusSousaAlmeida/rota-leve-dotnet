using RotaLeve.Api.Domain.Entities;
namespace RotaLeve.Api.Domain.Interfaces;
public interface IRelatoRepository
{
    Task<List<Relato>> ListarAsync();
    Task<Relato?> ObterAsync(int id, bool rastrear = false);
    Task<Relato> AdicionarAsync(Relato entity);
    Task<Relato> AtualizarAsync(Relato entity);
    Task<bool> ExcluirAsync(int id);
    Task<int> ExpirarAntigosAsync(DateTime limiteUtc);
}
