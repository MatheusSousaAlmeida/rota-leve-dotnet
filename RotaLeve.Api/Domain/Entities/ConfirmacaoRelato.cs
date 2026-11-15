using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace RotaLeve.Api.Domain.Entities;

[Table("CONFIRMACAO_RELATO")]
public class ConfirmacaoRelato
{
    [Key, Column("ID_CONFIRMACAO"), DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdConfirmacao { get; set; }

    [Column("ID_RELATO")]
    public int IdRelato { get; set; }

    [Column("ID_USUARIO")]
    public int IdUsuario { get; set; }

    [Column("DATA_CONFIRMACAO")]
    public DateTime DataConfirmacao { get; set; } = DateTime.UtcNow;

    [JsonIgnore] public Relato? Relato { get; set; }
    [JsonIgnore] public Usuario? Usuario { get; set; }
}
