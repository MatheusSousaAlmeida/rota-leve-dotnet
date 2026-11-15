using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using RotaLeve.Api.Domain.Enums;

namespace RotaLeve.Api.Domain.Entities;

[Table("PONTO_CRITICO")]
public class PontoCritico
{
    [Key, Column("ID_PONTO_CRITICO"), DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdPontoCritico { get; set; }

    [Required, StringLength(140), Column("NOME")]
    public string Nome { get; set; } = string.Empty;

    [Required, Column("TIPO")]
    public TipoPontoCritico Tipo { get; set; }

    [StringLength(400), Column("DESCRICAO")]
    public string? Descricao { get; set; }

    [StringLength(250), Column("ENDERECO")]
    public string? Endereco { get; set; }

    [Column("LATITUDE", TypeName = "NUMBER(10,7)")]
    public decimal Latitude { get; set; }

    [Column("LONGITUDE", TypeName = "NUMBER(10,7)")]
    public decimal Longitude { get; set; }

    [Column("ATIVO")]
    public bool Ativo { get; set; } = true;

    [Column("DATA_CRIACAO")]
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    [JsonIgnore] public ICollection<Relato> Relatos { get; set; } = new List<Relato>();
}
