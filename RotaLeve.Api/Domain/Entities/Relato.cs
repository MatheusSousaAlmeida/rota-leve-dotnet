using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using RotaLeve.Api.Domain.Enums;

namespace RotaLeve.Api.Domain.Entities;

[Table("RELATO")]
public class Relato
{
    [Key, Column("ID_RELATO"), DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdRelato { get; set; }

    [Column("ID_USUARIO")]
    public int IdUsuario { get; set; }

    [Column("ID_PONTO_CRITICO")]
    public int IdPontoCritico { get; set; }

    [Required, Column("TIPO")]
    public TipoRelato Tipo { get; set; }

    [Required, StringLength(600), Column("DESCRICAO")]
    public string Descricao { get; set; } = string.Empty;

    [Column("LATITUDE", TypeName = "NUMBER(10,7)")]
    public decimal Latitude { get; set; }

    [Column("LONGITUDE", TypeName = "NUMBER(10,7)")]
    public decimal Longitude { get; set; }

    [Required, Column("STATUS")]
    public StatusRelato Status { get; set; } = StatusRelato.Novo;

    [Column("DATA_CRIACAO")]
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    [Column("DATA_ULTIMA_CONFIRMACAO")]
    public DateTime? DataUltimaConfirmacao { get; set; }

    [Column("DATA_RESOLUCAO")]
    public DateTime? DataResolucao { get; set; }

    [JsonIgnore] public Usuario? Usuario { get; set; }
    [JsonIgnore] public PontoCritico? PontoCritico { get; set; }
    [JsonIgnore] public ICollection<ConfirmacaoRelato> Confirmacoes { get; set; } = new List<ConfirmacaoRelato>();
}
