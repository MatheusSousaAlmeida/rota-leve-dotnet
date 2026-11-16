using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RotaLeve.Api.Infrastructure.Data;

namespace RotaLeve.Api.Infrastructure.Health;

/// <summary>
/// Verifica conexão com Oracle mesmo quando a connection string está incompleta.
/// Uma falha vira Unhealthy/HTTP 503 em vez de impedir a inicialização da API.
/// </summary>
public sealed class OracleConnectivityHealthCheck(
    ApplicationContext context,
    ILogger<OracleConnectivityHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext checkContext,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var conectado = await context.Database.CanConnectAsync(cancellationToken);
            return conectado
                ? HealthCheckResult.Healthy("Conexão com Oracle disponível.")
                : HealthCheckResult.Unhealthy("Não foi possível conectar ao Oracle. Verifique a connection string e a disponibilidade do banco.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Health Check do Oracle falhou.");
            // Não expor detalhes da connection string ou de credenciais na API.
            return HealthCheckResult.Unhealthy("Oracle indisponível ou connection string inválida.");
        }
    }
}
