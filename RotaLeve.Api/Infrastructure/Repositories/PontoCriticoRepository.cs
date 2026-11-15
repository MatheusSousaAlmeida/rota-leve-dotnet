using Microsoft.EntityFrameworkCore;
using RotaLeve.Api.Domain.Entities;
using RotaLeve.Api.Domain.Interfaces;
using RotaLeve.Api.Infrastructure.Data;
namespace RotaLeve.Api.Infrastructure.Repositories;
public class PontoCriticoRepository(ApplicationContext db) : IPontoCriticoRepository
{
    public Task<List<PontoCritico>> ListarAsync() => db.PontosCriticos.AsNoTracking().Where(x => x.Ativo).OrderBy(x => x.Nome).ToListAsync();
    public Task<PontoCritico?> ObterAsync(int id) => db.PontosCriticos.FirstOrDefaultAsync(x => x.IdPontoCritico == id);
    public async Task<PontoCritico> AdicionarAsync(PontoCritico entity) { db.PontosCriticos.Add(entity); await db.SaveChangesAsync(); return entity; }
    public async Task<PontoCritico> AtualizarAsync(PontoCritico entity) { db.PontosCriticos.Update(entity); await db.SaveChangesAsync(); return entity; }
    public async Task<bool> ExcluirAsync(int id) { var e = await db.PontosCriticos.FindAsync(id); if (e is null) return false; e.Ativo = false; await db.SaveChangesAsync(); return true; }
}
