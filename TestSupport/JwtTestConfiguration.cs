using Microsoft.Extensions.Configuration;

namespace RotaLeve.TestSupport;

/// <summary>
/// Obtem a chave JWT usada pela API, sem manter segredos duplicados nos testes.
/// O arquivo de configuracao e copiado do projeto RotaLeve.Api durante o build.
/// Variaveis de ambiente tem precedencia sobre o appsettings.json.
/// </summary>
public static class JwtTestConfiguration
{
    private const string ApiSettingsFile = "appsettings.RotaLeveApi.json";

    public static string GetSecretKey()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile(ApiSettingsFile, optional: false, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        var secret = configuration["Secretkey"];
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException(
                "Secretkey nao configurada. Preencha 'Secretkey' no " +
                "RotaLeve.Api/appsettings.json antes de executar os testes, " +
                "ou configure a variavel de ambiente Secretkey.");

        return secret;
    }
}
