using RotaLeve.Api.Domain.Entities;
namespace RotaLeve.Api.Domain.Interfaces;
public interface ILocalidadeRepository
{
    Task<List<Localidade>> ListarAsync();
    Task<Localidade?> ObterAsync(int id);
    Task<Localidade> AdicionarAsync(Localidade entity);
    Task<Localidade> AtualizarAsync(Localidade entity);
    Task<bool> ExcluirAsync(int id);
}
