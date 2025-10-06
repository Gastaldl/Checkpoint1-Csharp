using Microsoft.EntityFrameworkCore;
using CheckPoint1.Models;

namespace CheckPoint1;

public class CheckpointContext : DbContext
{
    // DbSets
    public DbSet<Categoria> Categorias { get; set; } = null!;
    public DbSet<Produto> Produtos { get; set; } = null!;
    public DbSet<Cliente> Clientes { get; set; } = null!;
    public DbSet<Pedido> Pedidos { get; set; } = null!;
    public DbSet<PedidoItem> PedidoItens { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // SQLite em arquivo local
        if (!optionsBuilder.IsConfigured)
        {
            var dbPath = Path.Combine(AppContext.BaseDirectory, "loja.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Categoria (1) -> (N) Produto  [Cascade]
        modelBuilder.Entity<Produto>()
            .HasOne(p => p.Categoria)
            .WithMany(c => c.Produtos)
            .HasForeignKey(p => p.CategoriaId)
            .OnDelete(DeleteBehavior.Cascade);

        // Cliente (1) -> (N) Pedido  [Cascade]
        modelBuilder.Entity<Pedido>()
            .HasOne(p => p.Cliente)
            .WithMany(c => c.Pedidos)
            .HasForeignKey(p => p.ClienteId)
            .OnDelete(DeleteBehavior.Cascade);

        // Pedido (1) -> (N) PedidoItem  [Cascade]
        modelBuilder.Entity<PedidoItem>()
            .HasOne(i => i.Pedido)
            .WithMany(p => p.Itens)
            .HasForeignKey(i => i.PedidoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Produto (1) -> (N) PedidoItem  [Restrict]
        modelBuilder.Entity<PedidoItem>()
            .HasOne(i => i.Produto)
            .WithMany(p => p.PedidoItens)
            .HasForeignKey(i => i.ProdutoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índices únicos
        modelBuilder.Entity<Cliente>()
            .HasIndex(c => c.Email)
            .IsUnique();

        modelBuilder.Entity<Pedido>()
            .HasIndex(p => p.NumeroPedido)
            .IsUnique();

        // ===== Seed Data =====
        // Categorias
        modelBuilder.Entity<Categoria>().HasData(
            new Categoria { Id = 1, Nome = "Eletrônicos", Descricao = "Gadgets e dispositivos", DataCriacao = DateTime.UtcNow },
            new Categoria { Id = 2, Nome = "Livros", Descricao = "Ficção, não-ficção e técnicos", DataCriacao = DateTime.UtcNow },
            new Categoria { Id = 3, Nome = "Acessórios", Descricao = "Acessórios em geral", DataCriacao = DateTime.UtcNow }
        );

        // Produtos (inclui um com estoque 0)
        modelBuilder.Entity<Produto>().HasData(
            new Produto { Id = 1, Nome = "Fone Bluetooth", Descricao = "Fone sem fio com case", Preco = 199.90m, Estoque = 25, DataCriacao = DateTime.UtcNow, Ativo = true, CategoriaId = 1 },
            new Produto { Id = 2, Nome = "Teclado Mecânico", Descricao = "Switches Blue", Preco = 349.00m, Estoque = 0, DataCriacao = DateTime.UtcNow, Ativo = true, CategoriaId = 1 },
            new Produto { Id = 3, Nome = "Livro C# Essencial", Descricao = "Conceitos e práticas", Preco = 129.90m, Estoque = 40, DataCriacao = DateTime.UtcNow, Ativo = true, CategoriaId = 2 },
            new Produto { Id = 4, Nome = "Livro EF Core na Prática", Descricao = "Mapeamento e consultas", Preco = 149.90m, Estoque = 15, DataCriacao = DateTime.UtcNow, Ativo = true, CategoriaId = 2 },
            new Produto { Id = 5, Nome = "Mousepad XL", Descricao = "900x400mm", Preco = 59.90m, Estoque = 60, DataCriacao = DateTime.UtcNow, Ativo = true, CategoriaId = 3 },
            new Produto { Id = 6, Nome = "Cabo USB-C", Descricao = "1,5m nylon trançado", Preco = 39.90m, Estoque = 100, DataCriacao = DateTime.UtcNow, Ativo = true, CategoriaId = 3 }
        );

        // Clientes
        modelBuilder.Entity<Cliente>().HasData(
            new Cliente { Id = 1, Nome = "Ana Souza", Email = "ana.souza@example.com", Telefone = "11988887777", CPF = "11122233344", Endereco = "Rua A, 123", Cidade = "São Paulo", Estado = "SP", CEP = "01000-000", DataCadastro = DateTime.UtcNow, Ativo = true },
            new Cliente { Id = 2, Nome = "Bruno Silva", Email = "bruno.silva@example.com", Telefone = "21999996666", CPF = "55566677788", Endereco = "Av. B, 456", Cidade = "Rio de Janeiro", Estado = "RJ", CEP = "20000-000", DataCadastro = DateTime.UtcNow, Ativo = true }
        );

        // Pedidos
        modelBuilder.Entity<Pedido>().HasData(
            new Pedido
            {
                Id = 1,
                NumeroPedido = "PED-2025-0001",
                DataPedido = DateTime.UtcNow,
                Status = StatusPedido.Confirmado,
                ValorTotal = 199.90m + 59.90m, // Itens: 1x Fone + 1x Mousepad
                Desconto = 0m,
                Observacoes = "Entrega padrão",
                ClienteId = 1
            },
            new Pedido
            {
                Id = 2,
                NumeroPedido = "PED-2025-0002",
                DataPedido = DateTime.UtcNow,
                Status = StatusPedido.EmAndamento,
                ValorTotal = 129.90m + (39.90m * 2), // Itens: 1x Livro C# + 2x Cabo USB-C
                Desconto = 10.00m,
                Observacoes = "Cupom aplicado",
                ClienteId = 2
            }
        );

        // PedidoItens
        modelBuilder.Entity<PedidoItem>().HasData(
            // Pedido 1 (Ana): Fone Bluetooth (1x), Mousepad XL (1x)
            new PedidoItem { Id = 1, PedidoId = 1, ProdutoId = 1, Quantidade = 1, PrecoUnitario = 199.90m, Desconto = 0m },
            new PedidoItem { Id = 2, PedidoId = 1, ProdutoId = 5, Quantidade = 1, PrecoUnitario = 59.90m, Desconto = 0m },

            // Pedido 2 (Bruno): Livro C# (1x), Cabo USB-C (2x)
            new PedidoItem { Id = 3, PedidoId = 2, ProdutoId = 3, Quantidade = 1, PrecoUnitario = 129.90m, Desconto = 0m },
            new PedidoItem { Id = 4, PedidoId = 2, ProdutoId = 6, Quantidade = 2, PrecoUnitario = 39.90m, Desconto = 9.00m } // desconto total no item
        );
    }
}