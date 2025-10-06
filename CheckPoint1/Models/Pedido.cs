using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CheckPoint1.Models;

public class Pedido
{
    [Key]
    public int Id { get; set; }
        
    [Required]
    public string NumeroPedido { get; set; } = string.Empty;
        
    public DateTime DataPedido { get; set; }
        
    public StatusPedido Status { get; set; } = StatusPedido.Pendente;
        
    [Column(TypeName = "decimal(10,2)")]
    public decimal ValorTotal { get; set; }
        
    [Column(TypeName = "decimal(10,2)")]
    public decimal? Desconto { get; set; }
        
    [MaxLength(500)]
    public string? Observacoes { get; set; }
        
    // Foreign Key
    public int ClienteId { get; set; }
        
    // Navigation Properties
    public Cliente Cliente { get; set; } = null!;
    public ICollection<PedidoItem> Itens { get; set; } = new List<PedidoItem>();
}