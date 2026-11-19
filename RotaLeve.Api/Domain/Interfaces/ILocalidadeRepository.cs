using RotaLeve.Api.Domain.Entities;
namespace RotaLeve.Api.Domain.Interfaces;
public interface ILocalidadeRepository
{
    Task<List<Localidade>> ListarAsync();
    Task<(List<Localidade> Itens, int Total)> ListarPaginadoAsync(int pagina, int tamanhoPagina);

    Task<Localidade?> ObterAsync(int id);
    Task<Localidade> AdicionarAsync(Localidade entity);
    Task<Localidade> AtualizarAsync(Localidade entity);
    Task<bool> ExcluirAsync(int id);
}
