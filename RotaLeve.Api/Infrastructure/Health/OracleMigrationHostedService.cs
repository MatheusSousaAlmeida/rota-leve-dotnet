using Microsoft.EntityFrameworkCore;
using RotaLeve.Api.Infrastructure.Data;

namespace RotaLeve.Api.Infrastructure.Health;

/// <summary>
/// Aplica migrations automaticamente após a inicialização da aplicação, sem
/// impedir o Swagger e os Health Checks de responderem quando o Oracle falhar.
/// </summary>
public sealed class OracleMigrationHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<OracleMigrationHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Faz com que o Host disponibilize os endpoints antes da primeira conexão.
        await Task.Yield();

        const int maxRetries = 6;
        var retryDelay = TimeSpan.FromSeconds(20);

        for (var attempt = 1; attempt <= maxRetries && !stoppingToken.IsCancellationRequested; attempt++)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
                await context.Database.MigrateAsync(stoppingToken);
                logger.LogInformation("Migrations do Oracle aplicadas/verificadas com sucesso.");
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                if (attempt == maxRetries)
                {
                    logger.LogError(exception,
                        "Falha ao aplicar migrations Oracle após {MaxRetries} tentativas. API permanece disponível para diagnóstico via Health Checks.",
                        maxRetries);
                    return;
                }

                logger.LogWarning(exception,
                    "Falha na migration Oracle, tentativa {Attempt}/{MaxRetries}. Nova tentativa em {DelaySeconds} segundos.",
                    attempt, maxRetries, retryDelay.TotalSeconds);
                try
                {
                    await Task.Delay(retryDelay, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
            }
        }
    }
}
