using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CheckPoint1.Models;

public class PedidoItem
{
    [Key]
    public int Id { get; set; }
        
    public int Quantidade { get; set; }
        
    [Column(TypeName = "decimal(10,2)")]
    public decimal PrecoUnitario { get; set; }
        
    [Column(TypeName = "decimal(10,2)")]
    public decimal? Desconto { get; set; }
        
    // Calculated property
    [NotMapped]
    public decimal Subtotal => Quantidade * PrecoUnitario - (Desconto ?? 0);
        
    // Foreign Keys
    public int PedidoId { get; set; }
    public int ProdutoId { get; set; }
        
    // Navigation Properties
    public Pedido Pedido { get; set; } = null!;
    public Produto Produto { get; set; } = null!;
}