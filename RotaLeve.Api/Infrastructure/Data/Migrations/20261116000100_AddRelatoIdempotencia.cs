using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RotaLeve.Api.Infrastructure.Data;

#nullable disable
namespace RotaLeve.Api.Infrastructure.Data.Migrations;

[DbContext(typeof(ApplicationContext))]
[Migration("20261116000100_AddRelatoIdempotencia")]
public partial class AddRelatoIdempotencia : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE RELATO ADD (CHAVE_IDEMPOTENCIA VARCHAR2(64), HASH_REQUISICAO VARCHAR2(64))");
        migrationBuilder.Sql("CREATE UNIQUE INDEX UX_RELATO_CHAVE_IDEMP ON RELATO (CHAVE_IDEMPOTENCIA)");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX UX_RELATO_CHAVE_IDEMP");
        migrationBuilder.Sql("ALTER TABLE RELATO DROP (CHAVE_IDEMPOTENCIA, HASH_REQUISICAO)");
    }
}
