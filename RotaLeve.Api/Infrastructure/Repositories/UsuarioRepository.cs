using Microsoft.EntityFrameworkCore;
using RotaLeve.Api.Domain.Entities;
using RotaLeve.Api.Domain.Interfaces;
using RotaLeve.Api.Infrastructure.Data;

namespace RotaLeve.Api.Infrastructure.Repositories;

public class UsuarioRepository(ApplicationContext db) : IUsuarioRepository
{
    public async Task<IEnumerable<Usuario>> ListarAsync()
        => await db.Usuarios.AsNoTracking().OrderBy(x => x.IdUsuario).ToListAsync();

    public async Task<(List<Usuario> Itens, int Total)> ListarPaginadoAsync(int pagina, int tamanhoPagina)
    {
        var consulta = db.Usuarios.AsNoTracking();
        var total = await consulta.CountAsync();
        var itens = await consulta.OrderBy(x => x.IdUsuario)
            .Skip((pagina - 1) * tamanhoPagina).Take(tamanhoPagina).ToListAsync();
        return (itens, total);
    }

    public Task<Usuario?> ObterPorEmailAsync(string email)
        => db.Usuarios.FirstOrDefaultAsync(x => x.Email.ToLower() == email.ToLower());

    public Task<Usuario?> ObterPorIdAsync(int id)
        => db.Usuarios.FirstOrDefaultAsync(x => x.IdUsuario == id);

    public async Task<Usuario> AdicionarAsync(Usuario usuario)
    {
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        return usuario;
    }

    public async Task AtualizarAsync(Usuario usuario)
    {
        db.Usuarios.Update(usuario);
        await db.SaveChangesAsync();
    }

    public async Task ExcluirDefinitivamenteAsync(Usuario usuario)
    {
        // As FKs Oracle para USUARIO nao possuem ON DELETE CASCADE.
        // Excluir as dependencias em ordem, em uma unica transacao, evita
        // violacoes de integridade e deixa a exclusao completamente atomica.
        await using var transacao = await db.Database.BeginTransactionAsync();

        await db.Database.ExecuteSqlInterpolatedAsync($@"
            DELETE FROM CONFIRMACAO_RELATO
             WHERE ID_USUARIO = {usuario.IdUsuario}
                OR ID_RELATO IN (
                    SELECT ID_RELATO FROM RELATO WHERE ID_USUARIO = {usuario.IdUsuario}
                )");

        await db.Database.ExecuteSqlInterpolatedAsync($@"
            DELETE FROM RELATO WHERE ID_USUARIO = {usuario.IdUsuario}");

        db.Usuarios.Remove(usuario);
        await db.SaveChangesAsync();
        await transacao.CommitAsync();
    }
}
