using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Swashbuckle.AspNetCore.Annotations;

namespace RotaLeve.Api.Presentation.Controllers;

[Route("health")]
[ApiController]
[AllowAnonymous]
public class HealthController(HealthCheckService healthService) : ControllerBase
{
    [HttpGet("live")]
    [SwaggerOperation(
        Summary = "Verificar liveness da API",
        Description = """
        Verifica se o processo da API Rota Leve está em execução.

        ### Fluxo de processamento
        1. Executa somente os Health Checks registrados com a tag **live**.
        2. Consolida o resultado das verificações executadas.
        3. Retorna o status de saúde da API.

        ### Observação
        * Este check é autocontido e não consulta o banco de dados.
        """)]
    [SwaggerResponse(StatusCodes.Status200OK, "API em execução")]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "API indisponível")]
    public async Task<IActionResult> Live(CancellationToken cancellationToken)
    {
        var report = await healthService.CheckHealthAsync(
            registration => registration.Tags.Contains("live"),
            cancellationToken);

        return BuildResponse(report);
    }

    [HttpGet("ready")]
    [SwaggerOperation(
        Summary = "Verificar readiness da API e Oracle",
        Description = """
        Verifica se a API está pronta para atender requisições e se consegue acessar as dependências necessárias, incluindo o banco Oracle.

        ### Fluxo de processamento
        1. Executa os Health Checks registrados para a aplicação.
        2. Verifica a disponibilidade da conexão configurada com o Oracle.
        3. Consolida e retorna o resultado das verificações.

        ### Observação
        * Caso alguma dependência necessária esteja indisponível, o endpoint retorna **503 Service Unavailable**.
        """)]
    [SwaggerResponse(StatusCodes.Status200OK, "API pronta e Oracle disponível")]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "API ou Oracle indisponível")]
    public async Task<IActionResult> Ready(CancellationToken cancellationToken)
    {
        var report = await healthService.CheckHealthAsync(
            _ => true,
            cancellationToken);

        return BuildResponse(report);
    }

    private IActionResult BuildResponse(HealthReport report)
    {
        var result = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                error = entry.Value.Exception?.Message,
                durationMs = Math.Round(entry.Value.Duration.TotalMilliseconds, 2)
            })
        };

        return report.Status == HealthStatus.Healthy
            ? Ok(result)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, result);
    }
}
