using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RotaLeve.Api.Application.Dtos;

namespace RotaLeve.Api.Application.Services;

public static class IdempotenciaRelato
{
    public static bool ChaveValida(string? chave) =>
        !string.IsNullOrWhiteSpace(chave) &&
        chave.Length <= 200 &&
        chave.All(c => c >= 33 && c <= 126);

    public static string GerarHashChave(int usuarioId, string chave) =>
        Sha256($"{usuarioId}:{chave}");

    public static string GerarHashRequisicao(RelatoRequestDto dto)
    {
        var conteudo = JsonSerializer.Serialize(new
        {
            localidadeId = dto.LocalidadeId,
            tipo = (int)dto.Tipo,
            descricao = dto.Descricao.Trim(),
            latitude = dto.Latitude.ToString("0.#######", CultureInfo.InvariantCulture),
            longitude = dto.Longitude.ToString("0.#######", CultureInfo.InvariantCulture)
        });
        return Sha256(conteudo);
    }

    private static string Sha256(string texto) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(texto))).ToLowerInvariant();
}
