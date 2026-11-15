using Microsoft.EntityFrameworkCore;
using RotaLeve.Api.Domain.Entities;

namespace RotaLeve.Api.Infrastructure.Data;

public class ApplicationContext(DbContextOptions<ApplicationContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<PontoCritico> PontosCriticos => Set<PontoCritico>();
    public DbSet<Relato> Relatos => Set<Relato>();
    public DbSet<ConfirmacaoRelato> ConfirmacoesRelato => Set<ConfirmacaoRelato>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(e =>
        {
            e.Property(x => x.IdUsuario).ValueGeneratedOnAdd();
            e.Property(x => x.Ativo).HasConversion<int>().HasColumnType("NUMBER(1)");
            e.Property(x => x.Nome).HasColumnType("VARCHAR2(120)");
            e.Property(x => x.Email).HasColumnType("VARCHAR2(160)");
            e.Property(x => x.SenhaHash).HasColumnType("VARCHAR2(500)");
            e.HasIndex(x => x.Email).IsUnique().HasDatabaseName("UX_USUARIO_EMAIL");
        });
        modelBuilder.Entity<PontoCritico>(e =>
        {
            e.Property(x => x.IdPontoCritico).ValueGeneratedOnAdd();
            e.Property(x => x.Ativo).HasConversion<int>().HasColumnType("NUMBER(1)");
            e.Property(x => x.Nome).HasColumnType("VARCHAR2(140)");
            e.Property(x => x.Descricao).HasColumnType("VARCHAR2(400)");
            e.Property(x => x.Endereco).HasColumnType("VARCHAR2(250)");
        });
        modelBuilder.Entity<Relato>(e =>
        {
            e.Property(x => x.IdRelato).ValueGeneratedOnAdd();
            e.Property(x => x.Descricao).HasColumnType("VARCHAR2(600)");
            e.HasOne(x => x.Usuario).WithMany(x => x.Relatos).HasForeignKey(x => x.IdUsuario).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_RELATO_USUARIO");
            e.HasOne(x => x.PontoCritico).WithMany(x => x.Relatos).HasForeignKey(x => x.IdPontoCritico).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_RELATO_PONTO");
        });
        modelBuilder.Entity<ConfirmacaoRelato>(e =>
        {
            e.Property(x => x.IdConfirmacao).ValueGeneratedOnAdd();
            e.HasIndex(x => new { x.IdRelato, x.IdUsuario }).IsUnique().HasDatabaseName("UX_CONF_RELATO_USUARIO");
            e.HasOne(x => x.Relato).WithMany(x => x.Confirmacoes).HasForeignKey(x => x.IdRelato).OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_CONF_RELATO");
            e.HasOne(x => x.Usuario).WithMany(x => x.Confirmacoes).HasForeignKey(x => x.IdUsuario).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_CONF_USUARIO");
        });
    }
}
