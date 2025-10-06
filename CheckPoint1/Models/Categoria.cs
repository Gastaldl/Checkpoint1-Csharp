using System.ComponentModel.DataAnnotations;

namespace CheckPoint1.Models;

public class Categoria
{
    [Key]
    public int Id { get; set; }
        
    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;
        
    [MaxLength(500)]
    public string? Descricao { get; set; }
        
    public DateTime DataCriacao { get; set; }
        
    // Navigation Property - Uma categoria tem muitos produtos
    public ICollection<Produto> Produtos { get; set; } = new List<Produto>();
}