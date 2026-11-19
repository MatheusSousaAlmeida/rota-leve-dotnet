using RotaLeve.Api.Domain.Entities;

namespace RotaLeve.Api.Domain.Interfaces;

public interface IUsuarioRepository
{
    Task<IEnumerable<Usuario>> ListarAsync();
    Task<(List<Usuario> Itens, int Total)> ListarPaginadoAsync(int pagina, int tamanhoPagina);

    Task<Usuario?> ObterPorEmailAsync(string email);
    Task<Usuario?> ObterPorIdAsync(int id);
    Task<Usuario> AdicionarAsync(Usuario usuario);
    Task AtualizarAsync(Usuario usuario);
    Task ExcluirDefinitivamenteAsync(Usuario usuario);
}
