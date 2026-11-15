using Microsoft.EntityFrameworkCore;
using RotaLeve.Api.Domain.Entities;
using RotaLeve.Api.Domain.Interfaces;
using RotaLeve.Api.Infrastructure.Data;
namespace RotaLeve.Api.Infrastructure.Repositories;
public class PontoCriticoRepository(ApplicationContext db) : IPontoCriticoRepository
{
    public async Task<List<PontoCritico>> ListarAsync()
    {
        // O campo ATIVO e NUMBER(1) no Oracle. Na versao atual do provider,
        // traduzir uma expressao booleana sobre um campo com HasConversion<int>()
        // pode gerar InvalidCastException (Int32 -> Boolean) antes de executar o SQL.
        // O predicado usa o valor fisico do Oracle, sem interpolacao de entrada externa.
        var pontosAtivos = await db.PontosCriticos
            .FromSqlRaw("SELECT * FROM PONTO_CRITICO WHERE ATIVO = 1")
            .AsNoTracking()
            .ToListAsync();

        return pontosAtivos.OrderBy(x => x.Nome).ToList();
    }
    public Task<PontoCritico?> ObterAsync(int id) => db.PontosCriticos.FirstOrDefaultAsync(x => x.IdPontoCritico == id);
    public async Task<PontoCritico> AdicionarAsync(PontoCritico entity) { db.PontosCriticos.Add(entity); await db.SaveChangesAsync(); return entity; }
    public async Task<PontoCritico> AtualizarAsync(PontoCritico entity) { db.PontosCriticos.Update(entity); await db.SaveChangesAsync(); return entity; }
    public async Task<bool> ExcluirAsync(int id) { var e = await db.PontosCriticos.FindAsync(id); if (e is null) return false; e.Ativo = false; await db.SaveChangesAsync(); return true; }
}
