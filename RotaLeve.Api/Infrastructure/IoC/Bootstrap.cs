using Microsoft.EntityFrameworkCore;
using Oracle.EntityFrameworkCore;
using RotaLeve.Api.Application.Interfaces;
using RotaLeve.Api.Application.Services;
using RotaLeve.Api.Domain.Interfaces;
using RotaLeve.Api.Infrastructure.Data;
using RotaLeve.Api.Infrastructure.Repositories;
using RotaLeve.Api.Infrastructure.Security;

namespace RotaLeve.Api.Infrastructure.IoC;
public static class Bootstrap
{
    public static IServiceCollection AddRotaLeveInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("OracleDbConnection") ?? string.Empty;
        services.AddDbContext<ApplicationContext>(o => o.UseOracle(connection, x => x.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion21)));
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<ILocalidadeRepository, LocalidadeRepository>();
        services.AddScoped<IRelatoRepository, RelatoRepository>();
        services.AddScoped<IConfirmacaoRepository, ConfirmacaoRepository>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<ILocalidadeService, LocalidadeService>();
        services.AddScoped<IRelatoService, RelatoService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        return services;
    }
}
