using System.ComponentModel.DataAnnotations;

namespace CheckPoint1.Models;

public class Cliente
{
    [Key]
    public int Id { get; set; }
        
    [Required]
    [MaxLength(200)]
    public string Nome { get; set; } = string.Empty;
        
    [Required]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;
        
    [MaxLength(20)]
    public string? Telefone { get; set; }
        
    [MaxLength(14)]
    public string? CPF { get; set; }
        
    [MaxLength(300)]
    public string? Endereco { get; set; }
        
    [MaxLength(50)]
    public string? Cidade { get; set; }
        
    [MaxLength(2)]
    public string? Estado { get; set; }
        
    [MaxLength(10)]
    public string? CEP { get; set; }
        
    public DateTime DataCadastro { get; set; }
        
    public bool Ativo { get; set; } = true;
        
    // Navigation Property - Um cliente tem muitos pedidos
    public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
}