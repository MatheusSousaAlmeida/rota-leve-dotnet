using Microsoft.EntityFrameworkCore;
using RotaLeve.Api.Domain.Entities;
using RotaLeve.Api.Domain.Interfaces;
using RotaLeve.Api.Infrastructure.Data;

namespace RotaLeve.Api.Infrastructure.Repositories;

public class UsuarioRepository(ApplicationContext db) : IUsuarioRepository
{
    public async Task<IEnumerable<Usuario>> ListarAsync()
        => await db.Usuarios.AsNoTracking().OrderBy(x => x.IdUsuario).ToListAsync();

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
}
