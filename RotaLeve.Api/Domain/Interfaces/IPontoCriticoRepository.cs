using RotaLeve.Api.Domain.Entities;
namespace RotaLeve.Api.Domain.Interfaces;
public interface IPontoCriticoRepository
{
    Task<List<PontoCritico>> ListarAsync();
    Task<PontoCritico?> ObterAsync(int id);
    Task<PontoCritico> AdicionarAsync(PontoCritico entity);
    Task<PontoCritico> AtualizarAsync(PontoCritico entity);
    Task<bool> ExcluirAsync(int id);
}
