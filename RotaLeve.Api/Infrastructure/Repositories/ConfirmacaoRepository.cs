using Microsoft.EntityFrameworkCore;
using RotaLeve.Api.Domain.Entities;
using RotaLeve.Api.Domain.Interfaces;
using RotaLeve.Api.Infrastructure.Data;
namespace RotaLeve.Api.Infrastructure.Repositories;
public class ConfirmacaoRepository(ApplicationContext db) : IConfirmacaoRepository
{
    public Task<bool> ExisteAsync(int relatoId, int usuarioId) => db.ConfirmacoesRelato.AnyAsync(x => x.IdRelato == relatoId && x.IdUsuario == usuarioId);
    public Task<int> ContarAsync(int relatoId) => db.ConfirmacoesRelato.CountAsync(x => x.IdRelato == relatoId);
    public async Task<ConfirmacaoRelato> AdicionarAsync(ConfirmacaoRelato entity) { db.ConfirmacoesRelato.Add(entity); await db.SaveChangesAsync(); return entity; }
    public Task<ConfirmacaoRelato?> ObterAsync(int relatoId, int usuarioId) => db.ConfirmacoesRelato.FirstOrDefaultAsync(x => x.IdRelato == relatoId && x.IdUsuario == usuarioId);
    public async Task<bool> RemoverAsync(int relatoId, int usuarioId) { var e = await ObterAsync(relatoId, usuarioId); if (e is null) return false; db.ConfirmacoesRelato.Remove(e); await db.SaveChangesAsync(); return true; }
}
