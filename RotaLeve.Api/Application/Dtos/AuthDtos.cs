using System.ComponentModel.DataAnnotations;

namespace RotaLeve.Api.Application.Dtos;

public class LoginRequestDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Senha { get; set; } = string.Empty;
}

public record UsuarioResponseDto(int Id, string Nome, string Email, string Perfil);
public record AuthResponseDto(string AccessToken, DateTime ExpiresAtUtc, UsuarioResponseDto Usuario);
