using Microsoft.EntityFrameworkCore;
using RotaLeve.Api.Domain.Entities;
using RotaLeve.Api.Domain.Interfaces;
using RotaLeve.Api.Infrastructure.Data;
namespace RotaLeve.Api.Infrastructure.Repositories;
public class LocalidadeRepository(ApplicationContext db) : ILocalidadeRepository
{
    public async Task<List<Localidade>> ListarAsync()
    {
        // O campo ATIVO e NUMBER(1) no Oracle. Na versao atual do provider,
        // traduzir uma expressao booleana sobre um campo com HasConversion<int>()
        // pode gerar InvalidCastException (Int32 -> Boolean) antes de executar o SQL.
        // O predicado usa o valor fisico do Oracle, sem interpolacao de entrada externa.
        var localidadesAtivas = await db.Localidades
            .FromSqlRaw("SELECT * FROM LOCALIDADE WHERE ATIVO = 1")
            .AsNoTracking()
            .ToListAsync();

        return localidadesAtivas.OrderBy(x => x.Nome).ToList();
    }
    public async Task<(List<Localidade> Itens, int Total)> ListarPaginadoAsync(int pagina, int tamanhoPagina)
    {
        // Filtrar pelo NUMBER(1) fisico evita a conversao booleana do provider Oracle.
        var consulta = db.Localidades.FromSqlRaw("SELECT * FROM LOCALIDADE WHERE ATIVO = 1").AsNoTracking();
        var total = await consulta.CountAsync();
        var itens = await consulta.OrderBy(x => x.Nome).ThenBy(x => x.IdLocalidade)
            .Skip((pagina - 1) * tamanhoPagina).Take(tamanhoPagina).ToListAsync();
        return (itens, total);
    }
    public Task<Localidade?> ObterAsync(int id) => db.Localidades.FirstOrDefaultAsync(x => x.IdLocalidade == id);
    public async Task<Localidade> AdicionarAsync(Localidade entity) { db.Localidades.Add(entity); await db.SaveChangesAsync(); return entity; }
    public async Task<Localidade> AtualizarAsync(Localidade entity) { db.Localidades.Update(entity); await db.SaveChangesAsync(); return entity; }
    public async Task<bool> ExcluirAsync(int id) { var e = await db.Localidades.FindAsync(id); if (e is null) return false; e.Ativo = false; await db.SaveChangesAsync(); return true; }
}
