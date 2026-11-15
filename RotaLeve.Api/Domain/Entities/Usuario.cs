using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using RotaLeve.Api.Domain.Enums;

namespace RotaLeve.Api.Domain.Entities;

[Table("USUARIO")]
public class Usuario
{
    [Key, Column("ID_USUARIO"), DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdUsuario { get; set; }

    [Required, StringLength(120), Column("NOME")]
    public string Nome { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(160), Column("EMAIL")]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(500), Column("SENHA_HASH"), JsonIgnore]
    public string SenhaHash { get; set; } = string.Empty;

    [Required, Column("TIPO_USUARIO")]
    public TipoUsuario TipoUsuario { get; set; } = TipoUsuario.Cidadao;

    [Column("ATIVO")]
    public bool Ativo { get; set; } = true;

    [Column("DATA_CRIACAO")]
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    [JsonIgnore] public ICollection<Relato> Relatos { get; set; } = new List<Relato>();
    [JsonIgnore] public ICollection<ConfirmacaoRelato> Confirmacoes { get; set; } = new List<ConfirmacaoRelato>();
}
