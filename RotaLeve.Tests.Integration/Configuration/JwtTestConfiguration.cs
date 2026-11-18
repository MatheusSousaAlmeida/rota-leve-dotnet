using Microsoft.Extensions.Configuration;

namespace RotaLeve.Tests.Integration.Configuration;

/// <summary>
/// Le a Secretkey da configuracao da API, com fallback ficticio exclusivo dos testes.
/// O arquivo de configuracao e copiado do projeto RotaLeve.Api durante o build.
/// Variaveis de ambiente tem precedencia sobre o appsettings.json.
/// </summary>
public static class JwtTestConfiguration
{
    private const string ApiSettingsFile = "appsettings.RotaLeveApi.json";
    // Exclusivamente para testes automatizados. Nunca deve ser utilizada pela API real.
    private const string TestOnlySecretKey = "RotaLeve-Testes-Automatizados-JWT-Chave-Ficticia-2026-001";

    public static string GetSecretKey()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile(ApiSettingsFile, optional: false, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        var secret = configuration["Secretkey"];
        return string.IsNullOrWhiteSpace(secret) ? TestOnlySecretKey : secret;
    }
}
