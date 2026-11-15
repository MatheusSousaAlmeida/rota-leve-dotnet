using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace RotaLeve.Api.Application.Dtos;

public class UsuarioCreateRequestDto
{
    [Required, StringLength(120, MinimumLength = 3)]
    public string Nome { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(160)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    public string Senha { get; set; } = string.Empty;

    [DefaultValue("cidadao")]
    [RegularExpression("^(?i:cidadao|admin)$", ErrorMessage = "O perfil deve ser 'cidadao' ou 'admin'.")]
    public string Perfil { get; set; } = "cidadao";
}

public class UsuarioUpdateRequestDto
{
    [Required, StringLength(120, MinimumLength = 3)]
    public string Nome { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(160)]
    public string Email { get; set; } = string.Empty;

    [StringLength(100, MinimumLength = 8)]
    public string? Senha { get; set; }

    [Required]
    [RegularExpression("^(?i:cidadao|admin)$", ErrorMessage = "O perfil deve ser 'cidadao' ou 'admin'.")]
    public string Perfil { get; set; } = "cidadao";

    public bool Ativo { get; set; } = true;
}

public record UsuarioDetalheResponseDto(
    int Id,
    string Nome,
    string Email,
    string Perfil,
    bool Ativo,
    DateTime DataCriacao);
