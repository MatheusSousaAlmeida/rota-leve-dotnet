using Microsoft.EntityFrameworkCore;
using RotaLeve.Api.Domain.Entities;
using RotaLeve.Api.Domain.Enums;
using RotaLeve.Api.Domain.Interfaces;
using RotaLeve.Api.Infrastructure.Data;
namespace RotaLeve.Api.Infrastructure.Repositories;
public class RelatoRepository(ApplicationContext db) : IRelatoRepository
{
    public Task<List<Relato>> ListarAsync() => db.Relatos.AsNoTracking().Include(x => x.Confirmacoes).OrderByDescending(x => x.DataCriacao).ToListAsync();
    public Task<Relato?> ObterAsync(int id, bool rastrear = false)
    {
        var q = db.Relatos.Include(x => x.Confirmacoes).AsQueryable();
        if (!rastrear) q = q.AsNoTracking();
        return q.FirstOrDefaultAsync(x => x.IdRelato == id);
    }
    public async Task<Relato> AdicionarAsync(Relato entity) { db.Relatos.Add(entity); await db.SaveChangesAsync(); return entity; }
    public async Task<Relato> AtualizarAsync(Relato entity) { db.Relatos.Update(entity); await db.SaveChangesAsync(); return entity; }
    public async Task<bool> ExcluirAsync(int id) { var e = await db.Relatos.FindAsync(id); if (e is null) return false; db.Relatos.Remove(e); await db.SaveChangesAsync(); return true; }
    public async Task<int> ExpirarAntigosAsync(DateTime limiteUtc)
    {
        var itens = await db.Relatos.Where(x => (x.Status == StatusRelato.Novo || x.Status == StatusRelato.Ativo) && (x.DataUltimaConfirmacao ?? x.DataCriacao) < limiteUtc).ToListAsync();
        foreach (var item in itens) item.Status = StatusRelato.Expirado;
        if (itens.Count > 0) await db.SaveChangesAsync();
        return itens.Count;
    }
}
