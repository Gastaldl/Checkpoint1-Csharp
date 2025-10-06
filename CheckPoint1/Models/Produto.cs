using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CheckPoint1.Models;

public class Produto
{
    [Key]
    public int Id { get; set; }
        
    [Required]
    [MaxLength(200)]
    public string Nome { get; set; } = string.Empty;
        
    [MaxLength(1000)]
    public string? Descricao { get; set; }
        
    [Column(TypeName = "decimal(10,2)")]
    public decimal Preco { get; set; }
        
    public int Estoque { get; set; }
        
    public DateTime DataCriacao { get; set; }
        
    public bool Ativo { get; set; } = true;
        
    // Foreign Key
    public int CategoriaId { get; set; }
        
    // Navigation Properties
    public Categoria Categoria { get; set; } = null!;
    public ICollection<PedidoItem> PedidoItens { get; set; } = new List<PedidoItem>();
}