using System.Text;
using Microsoft.EntityFrameworkCore;
using CheckPoint1.Services;

namespace CheckPoint1;

public class Program
{
    private static readonly EntityFrameworkService EfService = new();
    private static readonly AdoNetService AdoService = new();

    static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        Console.WriteLine("╔══════════════════════════════════════╗");
        Console.WriteLine("║        CHECKPOINT 1 - C# FIAP        ║");
        Console.WriteLine("║   Sistema de Gestão de Loja Online   ║");
        Console.WriteLine("║    Entity Framework + ADO.NET        ║");
        Console.WriteLine("╚══════════════════════════════════════╝");

        await InicializarBanco();

        var continuar = true;
        while (continuar)
        {
            MostrarMenuPrincipal();
            var opcao = Console.ReadLine()?.Trim();

            switch (opcao)
            {
                case "1": MenuEntityFramework(); break;
                case "2": MenuAdoNet(); break;
                case "0": continuar = false; break;
                default:
                    Console.WriteLine("Opção inválida!");
                    Thread.Sleep(1000);
                    break;
            }

            if (continuar)
            {
                Console.WriteLine("\nPressione qualquer tecla para continuar...");
                Console.ReadKey(true);
            }
        }

        EfService?.Dispose();
        AdoService?.Dispose();
        Console.WriteLine("Sistema encerrado!");
    }

    static void MostrarMenuPrincipal()
    {
        Console.Clear();
        Console.WriteLine("╔══════════════════════════════════════╗");
        Console.WriteLine("║            MENU PRINCIPAL            ║");
        Console.WriteLine("╠══════════════════════════════════════╣");
        Console.WriteLine("║ 1 - Entity Framework                 ║");
        Console.WriteLine("║ 2 - ADO.NET Direto                   ║");
        Console.WriteLine("║ 0 - Sair                             ║");
        Console.WriteLine("╚══════════════════════════════════════╝");
        Console.Write("Escolha uma opção: ");
    }

    static void MenuEntityFramework()
    {
        bool voltar = false;
        while (!voltar)
        {
            Console.Clear();
            Console.WriteLine("╔══════════════════════════════════════╗");
            Console.WriteLine("║         ENTITY FRAMEWORK             ║");
            Console.WriteLine("╠══════════════════════════════════════╣");
            Console.WriteLine("║          CADASTROS                   ║");
            Console.WriteLine("║ 1 - Gerenciar Categorias             ║");
            Console.WriteLine("║ 2 - Gerenciar Produtos               ║");
            Console.WriteLine("║ 3 - Gerenciar Clientes               ║");
            Console.WriteLine("║ 4 - Gerenciar Pedidos                ║");
            Console.WriteLine("║                                      ║");
            Console.WriteLine("║          CONSULTAS                   ║");
            Console.WriteLine("║ 5 - Consultas LINQ Avançadas         ║");
            Console.WriteLine("║ 6 - Relatórios Gerais                ║");
            Console.WriteLine("║                                      ║");
            Console.WriteLine("║ 0 - Voltar ao Menu Principal         ║");
            Console.WriteLine("╚══════════════════════════════════════╝");
            Console.Write("Escolha uma opção: ");

            var opcao = Console.ReadLine()?.Trim();

            try
            {
                switch (opcao)
                {
                    case "1": MenuCategorias(); break;
                    case "2": MenuProdutos(); break;
                    case "3": MenuClientes(); break;
                    case "4": MenuPedidos(); break;
                    case "5": EfService.ConsultasAvancadas(); break;
                    case "6": EfService.RelatoriosGerais(); break;
                    case "0": voltar = true; break;
                    default:
                        Console.WriteLine("Opção inválida!");
                        Thread.Sleep(1000);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro: {ex.Message}");
                Console.WriteLine("Pressione qualquer tecla para continuar...");
                Console.ReadKey(true);
            }

            if (!voltar && opcao != "0")
            {
                Console.WriteLine("\nPressione qualquer tecla para continuar...");
                Console.ReadKey(true);
            }
        }
    }

    static void MenuCategorias()
    {
        Console.Clear();
        Console.WriteLine("╔══════════════════════════════════════╗");
        Console.WriteLine("║            CATEGORIAS                ║");
        Console.WriteLine("╠══════════════════════════════════════╣");
        Console.WriteLine("║ 1 - Criar Categoria                  ║");
        Console.WriteLine("║ 2 - Listar Categorias                ║");
        Console.WriteLine("║ 0 - Voltar                           ║");
        Console.WriteLine("╚══════════════════════════════════════╝");
        Console.Write("Escolha uma opção: ");

        var opcao = Console.ReadLine()?.Trim();
        switch (opcao)
        {
            case "1": EfService.CriarCategoria(); break;
            case "2": EfService.ListarCategorias(); break;
        }
    }

    static void MenuProdutos()
    {
        Console.Clear();
        Console.WriteLine("╔══════════════════════════════════════╗");
        Console.WriteLine("║             PRODUTOS                 ║");
        Console.WriteLine("╠══════════════════════════════════════╣");
        Console.WriteLine("║ 1 - Criar Produto                    ║");
        Console.WriteLine("║ 2 - Listar Produtos                  ║");
        Console.WriteLine("║ 3 - Atualizar Produto                ║");
        Console.WriteLine("║ 0 - Voltar                           ║");
        Console.WriteLine("╚══════════════════════════════════════╝");
        Console.Write("Escolha uma opção: ");

        var opcao = Console.ReadLine()?.Trim();
        switch (opcao)
        {
            case "1": EfService.CriarProduto(); break;
            case "2": EfService.ListarProdutos(); break;
            case "3": EfService.AtualizarProduto(); break;
        }
    }

    static void MenuClientes()
    {
        Console.Clear();
        Console.WriteLine("╔══════════════════════════════════════╗");
        Console.WriteLine("║             CLIENTES                 ║");
        Console.WriteLine("╠══════════════════════════════════════╣");
        Console.WriteLine("║ 1 - Criar Cliente                    ║");
        Console.WriteLine("║ 2 - Listar Clientes                  ║");
        Console.WriteLine("║ 3 - Atualizar Cliente                ║");
        Console.WriteLine("║ 0 - Voltar                           ║");
        Console.WriteLine("╚══════════════════════════════════════╝");
        Console.Write("Escolha uma opção: ");

        var opcao = Console.ReadLine()?.Trim();
        switch (opcao)
        {
            case "1": EfService.CriarCliente(); break;
            case "2": EfService.ListarClientes(); break;
            case "3": EfService.AtualizarCliente(); break;
        }
    }

    static void MenuPedidos()
    {
        Console.Clear();
        Console.WriteLine("╔══════════════════════════════════════╗");
        Console.WriteLine("║              PEDIDOS                 ║");
        Console.WriteLine("╠══════════════════════════════════════╣");
        Console.WriteLine("║ 1 - Criar Pedido                     ║");
        Console.WriteLine("║ 2 - Listar Pedidos                   ║");
        Console.WriteLine("║ 3 - Atualizar Status                 ║");
        Console.WriteLine("║ 4 - Cancelar Pedido                  ║");
        Console.WriteLine("║ 0 - Voltar                           ║");
        Console.WriteLine("╚══════════════════════════════════════╝");
        Console.Write("Escolha uma opção: ");

        var opcao = Console.ReadLine()?.Trim();
        switch (opcao)
        {
            case "1": EfService.CriarPedido(); break;
            case "2": EfService.ListarPedidos(); break;
            case "3": EfService.AtualizarStatusPedido(); break;
            case "4": EfService.CancelarPedido(); break;
        }
    }

    static void MenuAdoNet()
    {
        bool voltar = false;
        while (!voltar)
        {
            Console.Clear();
            Console.WriteLine("╔══════════════════════════════════════╗");
            Console.WriteLine("║            ADO.NET DIRETO            ║");
            Console.WriteLine("╠══════════════════════════════════════╣");
            Console.WriteLine("║          CONSULTAS COMPLEXAS         ║");
            Console.WriteLine("║ 1 - Relatório Vendas Completo        ║");
            Console.WriteLine("║ 2 - Faturamento por Cliente          ║");
            Console.WriteLine("║ 3 - Produtos sem Vendas              ║");
            Console.WriteLine("║                                      ║");
            Console.WriteLine("║          OPERAÇÕES AVANÇADAS         ║");
            Console.WriteLine("║ 4 - Atualizar Estoque em Lote        ║");
            Console.WriteLine("║ 5 - Inserir Pedido Completo          ║");
            Console.WriteLine("║ 6 - Excluir Dados Antigos            ║");
            Console.WriteLine("║                                      ║");
            Console.WriteLine("║          PROCESSOS                   ║");
            Console.WriteLine("║ 7 - Processar Devolução              ║");
            Console.WriteLine("║                                      ║");
            Console.WriteLine("║          ANÁLISES                    ║");
            Console.WriteLine("║ 8 - Análise Performance              ║");
            Console.WriteLine("║                                      ║");
            Console.WriteLine("║          UTILITÁRIOS                 ║");
            Console.WriteLine("║ 9 - Testar Conexão                   ║");
            Console.WriteLine("║                                      ║");
            Console.WriteLine("║ 0 - Voltar ao Menu Principal         ║");
            Console.WriteLine("╚══════════════════════════════════════╝");
            Console.Write("Escolha uma opção: ");

            var opcao = Console.ReadLine()?.Trim();

            try
            {
                switch (opcao)
                {
                    case "1": AdoService.RelatorioVendasCompleto(); break;
                    case "2": AdoService.FaturamentoPorCliente(); break;
                    case "3": AdoService.ProdutosSemVenda(); break;
                    case "4": AdoService.AtualizarEstoqueLote(); break;
                    case "5": AdoService.InserirPedidoCompleto(); break;
                    case "6": AdoService.ExcluirDadosAntigos(); break;
                    case "7": AdoService.ProcessarDevolucao(); break;
                    case "8": AdoService.AnalisarPerformanceVendas(); break;
                    case "9": AdoService.TestarConexao(); break;
                    case "0": voltar = true; break;
                    default:
                        Console.WriteLine("Opção inválida!");
                        Thread.Sleep(1000);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro: {ex.Message}");
                Console.WriteLine("Pressione qualquer tecla para continuar...");
                Console.ReadKey(true);
            }

            if (!voltar && opcao != "0")
            {
                Console.WriteLine("\nPressione qualquer tecla para continuar...");
                Console.ReadKey(true);
            }
        }
    }

    static async Task InicializarBanco()
    {
        try
        {
            Console.WriteLine("Inicializando banco de dados...");
            using var context = new CheckpointContext();

            // Se houver migrations no projeto, aplica; se não houver, cria o schema do modelo
            var temMigrations = context.Database.GetMigrations().Any();
            if (temMigrations)
                await context.Database.MigrateAsync();
            else
                await context.Database.EnsureCreatedAsync();

            Console.WriteLine("Banco inicializado com sucesso!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao inicializar banco: {ex.Message}");
            Console.WriteLine("Verifique a implementação do DbContext.");
            Console.WriteLine("Pressione qualquer tecla para continuar...");
            Console.ReadKey(true);
        }
    }
}