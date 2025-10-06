using System.Globalization;
using System.Text.RegularExpressions;
using CheckPoint1.Models;
using Microsoft.EntityFrameworkCore;

namespace CheckPoint1.Services;

public class EntityFrameworkService : IDisposable
{
    private readonly CheckpointContext _context;

    public EntityFrameworkService()
    {
        _context = new CheckpointContext();
    }

    // ========== CRUD CATEGORIAS ==========

    public void CriarCategoria()
    {
        Console.WriteLine("=== CRIAR CATEGORIA ===");
        Console.Write("Nome: ");
        var nome = (Console.ReadLine() ?? "").Trim();

        if (string.IsNullOrWhiteSpace(nome))
        {
            Console.WriteLine("Nome é obrigatório.");
            return;
        }

        Console.Write("Descrição (opcional): ");
        var desc = Console.ReadLine();

        // evita duplicidade básica por nome
        var existe = _context.Categorias.Any(c => c.Nome.ToLower() == nome.ToLower());
        if (existe)
        {
            Console.WriteLine("Já existe uma categoria com esse nome.");
            return;
        }

        var cat = new Categoria
        {
            Nome = nome,
            Descricao = string.IsNullOrWhiteSpace(desc) ? null : desc,
            DataCriacao = DateTime.UtcNow
        };

        _context.Categorias.Add(cat);
        _context.SaveChanges();
        Console.WriteLine($"Categoria criada (Id={cat.Id}).");
    }

    public void ListarCategorias()
    {
        Console.WriteLine("=== CATEGORIAS ===");
        var lista = _context.Categorias
            .Select(c => new
            {
                c.Id,
                c.Nome,
                c.Descricao,
                QtdeProdutos = c.Produtos.Count
            })
            .OrderBy(c => c.Nome)
            .ToList();

        if (!lista.Any())
        {
            Console.WriteLine("Nenhuma categoria cadastrada.");
            return;
        }

        foreach (var c in lista)
        {
            Console.WriteLine($"[{c.Id}] {c.Nome}  • Produtos: {c.QtdeProdutos}  {(string.IsNullOrWhiteSpace(c.Descricao) ? "" : $"• {c.Descricao}")}");
        }
    }

    // ========== CRUD PRODUTOS ==========

    public void CriarProduto()
    {
        Console.WriteLine("=== CRIAR PRODUTO ===");

        // listar categorias para escolha
        var cats = _context.Categorias.OrderBy(c => c.Nome).ToList();
        if (!cats.Any())
        {
            Console.WriteLine("Nenhuma categoria existente. Crie uma categoria antes.");
            return;
        }

        Console.WriteLine("Categorias disponíveis:");
        foreach (var c in cats)
            Console.WriteLine($"  {c.Id,2} - {c.Nome}");

        Console.Write("Categoria Id: ");
        if (!int.TryParse(Console.ReadLine(), out var catId))
        {
            Console.WriteLine("Id inválido.");
            return;
        }

        var categoria = _context.Categorias.Find(catId);
        if (categoria == null)
        {
            Console.WriteLine("Categoria não encontrada.");
            return;
        }

        Console.Write("Nome: ");
        var nome = (Console.ReadLine() ?? "").Trim();
        if (string.IsNullOrWhiteSpace(nome))
        {
            Console.WriteLine("Nome é obrigatório.");
            return;
        }

        Console.Write("Descrição (opcional): ");
        var desc = Console.ReadLine();

        Console.Write("Preço (ex.: 199,90): ");
        if (!TryParseDecimal(Console.ReadLine(), out var preco) || preco < 0)
        {
            Console.WriteLine("Preço inválido.");
            return;
        }

        Console.Write("Estoque (ex.: 10): ");
        if (!int.TryParse(Console.ReadLine(), out var estoque) || estoque < 0)
        {
            Console.WriteLine("Estoque inválido.");
            return;
        }

        var p = new Produto
        {
            Nome = nome,
            Descricao = string.IsNullOrWhiteSpace(desc) ? null : desc,
            Preco = preco,
            Estoque = estoque,
            DataCriacao = DateTime.UtcNow,
            Ativo = true,
            CategoriaId = categoria.Id
        };

        _context.Produtos.Add(p);
        _context.SaveChanges();
        Console.WriteLine($"Produto criado (Id={p.Id}).");
    }

    public void ListarProdutos()
    {
        Console.WriteLine("=== PRODUTOS ===");
        var produtos = _context.Produtos
            .Include(p => p.Categoria)
            .OrderBy(p => p.Categoria.Nome)
            .ThenBy(p => p.Nome)
            .ToList();

        if (!produtos.Any())
        {
            Console.WriteLine("Nenhum produto cadastrado.");
            return;
        }

        var culture = new CultureInfo("pt-BR");
        foreach (var p in produtos)
        {
            Console.WriteLine(
                $"[{p.Id}] {p.Nome}  • Cat: {p.Categoria.Nome}  • Preço: {p.Preco.ToString("C", culture)}  • Estoque: {p.Estoque}  • Ativo: {(p.Ativo ? "Sim" : "Não")}");
        }
    }

    public void AtualizarProduto()
    {
        Console.WriteLine("=== ATUALIZAR PRODUTO ===");
        Console.Write("Informe o Id do produto: ");
        if (!int.TryParse(Console.ReadLine(), out var id))
        {
            Console.WriteLine("Id inválido.");
            return;
        }

        var p = _context.Produtos.Find(id);
        if (p == null)
        {
            Console.WriteLine("Produto não encontrado.");
            return;
        }

        Console.WriteLine($"Editando: {p.Nome} (CategoriaId={p.CategoriaId})");
        Console.Write($"Novo nome (Enter mantém '{p.Nome}'): ");
        var nome = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(nome)) p.Nome = nome.Trim();

        Console.Write($"Nova descrição (Enter mantém): ");
        var desc = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(desc)) p.Descricao = desc;

        Console.Write($"Novo preço (atual {p.Preco}): ");
        var precoStr = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(precoStr))
        {
            if (!TryParseDecimal(precoStr, out var preco) || preco < 0)
            {
                Console.WriteLine("Preço inválido.");
                return;
            }
            p.Preco = preco;
        }

        Console.Write($"Novo estoque (atual {p.Estoque}): ");
        var estqStr = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(estqStr))
        {
            if (!int.TryParse(estqStr, out var estq) || estq < 0)
            {
                Console.WriteLine("Estoque inválido.");
                return;
            }
            p.Estoque = estq;
        }

        Console.Write($"Alterar categoria? Informe Id (Enter mantém {p.CategoriaId}): ");
        var catStr = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(catStr))
        {
            if (int.TryParse(catStr, out var catId))
            {
                var cat = _context.Categorias.Find(catId);
                if (cat == null)
                {
                    Console.WriteLine("Categoria não encontrada.");
                    return;
                }
                p.CategoriaId = cat.Id;
            }
            else
            {
                Console.WriteLine("Id de categoria inválido.");
                return;
            }
        }

        Console.Write($"Ativo? (S/N, atual {(p.Ativo ? "S" : "N")}): ");
        var ativoStr = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(ativoStr))
        {
            p.Ativo = ativoStr.Trim().ToUpperInvariant().StartsWith("S");
        }

        _context.SaveChanges();
        Console.WriteLine("Produto atualizado.");
    }

    // ========== CRUD CLIENTES ==========

    public void CriarCliente()
    {
        Console.WriteLine("=== CRIAR CLIENTE ===");

        Console.Write("Nome: ");
        var nome = (Console.ReadLine() ?? "").Trim();
        if (string.IsNullOrWhiteSpace(nome))
        {
            Console.WriteLine("Nome é obrigatório.");
            return;
        }

        Console.Write("Email: ");
        var email = (Console.ReadLine() ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
        {
            Console.WriteLine("Email é obrigatório.");
            return;
        }

        var emailExiste = _context.Clientes.Any(c => c.Email.ToLower() == email);
        if (emailExiste)
        {
            Console.WriteLine("Já existe um cliente com esse e-mail.");
            return;
        }

        Console.Write("Telefone (opcional): ");
        var tel = (Console.ReadLine() ?? "").Trim();

        Console.Write("CPF (apenas números ou com pontos/traços, será saneado): ");
        var cpfRaw = Console.ReadLine();
        var cpf = SomenteNumeros(cpfRaw ?? "");
        if (!string.IsNullOrEmpty(cpf) && cpf.Length != 11)
        {
            Console.WriteLine("CPF deve ter 11 dígitos (após saneamento).");
            return;
        }

        Console.Write("Endereço (opcional): ");
        var end = Console.ReadLine();
        Console.Write("Cidade (opcional): ");
        var cid = Console.ReadLine();
        Console.Write("Estado (UF, opcional): ");
        var uf = Console.ReadLine();
        Console.Write("CEP (opcional): ");
        var cep = Console.ReadLine();

        var cli = new Cliente
        {
            Nome = nome,
            Email = email,
            Telefone = string.IsNullOrWhiteSpace(tel) ? null : tel,
            CPF = string.IsNullOrWhiteSpace(cpf) ? null : cpf,
            Endereco = string.IsNullOrWhiteSpace(end) ? null : end,
            Cidade = string.IsNullOrWhiteSpace(cid) ? null : cid,
            Estado = string.IsNullOrWhiteSpace(uf) ? null : uf,
            CEP = string.IsNullOrWhiteSpace(cep) ? null : cep,
            DataCadastro = DateTime.UtcNow,
            Ativo = true
        };

        _context.Clientes.Add(cli);
        _context.SaveChanges();
        Console.WriteLine($"Cliente criado (Id={cli.Id}).");
    }

    public void ListarClientes()
    {
        Console.WriteLine("=== CLIENTES ===");

        var dados = _context.Clientes
            .Select(c => new
            {
                c.Id,
                c.Nome,
                c.Email,
                c.Cidade,
                c.Estado,
                c.Ativo,
                QtdePedidos = c.Pedidos.Count
            })
            .OrderByDescending(c => c.QtdePedidos)
            .ThenBy(c => c.Nome)
            .ToList();

        if (!dados.Any())
        {
            Console.WriteLine("Nenhum cliente cadastrado.");
            return;
        }

        foreach (var c in dados)
        {
            Console.WriteLine($"[{c.Id}] {c.Nome} • {c.Email} • Pedidos: {c.QtdePedidos} • Local: {c.Cidade}/{c.Estado} • Ativo: {(c.Ativo ? "Sim" : "Não")}");
        }
    }

    public void AtualizarCliente()
    {
        Console.WriteLine("=== ATUALIZAR CLIENTE ===");
        Console.Write("Informe o Id do cliente: ");
        if (!int.TryParse(Console.ReadLine(), out var id))
        {
            Console.WriteLine("Id inválido.");
            return;
        }

        var cli = _context.Clientes.Find(id);
        if (cli == null)
        {
            Console.WriteLine("Cliente não encontrado.");
            return;
        }

        Console.WriteLine($"Editando: {cli.Nome} ({cli.Email})");

        Console.Write($"Novo nome (Enter mantém '{cli.Nome}'): ");
        var nome = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(nome)) cli.Nome = nome.Trim();

        Console.Write($"Novo e-mail (Enter mantém '{cli.Email}'): ");
        var email = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(email))
        {
            var novoEmail = email.Trim().ToLowerInvariant();
            var emailExiste = _context.Clientes.Any(c => c.Email.ToLower() == novoEmail && c.Id != cli.Id);
            if (emailExiste)
            {
                Console.WriteLine("Já existe outro cliente com esse e-mail.");
                return;
            }
            cli.Email = novoEmail;
        }

        Console.Write($"Novo telefone (Enter mantém): ");
        var tel = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(tel)) cli.Telefone = tel.Trim();

        Console.Write($"Novo CPF (Enter mantém): ");
        var cpfRaw = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(cpfRaw))
        {
            var cpf = SomenteNumeros(cpfRaw);
            if (cpf.Length != 11)
            {
                Console.WriteLine("CPF deve ter 11 dígitos (após saneamento).");
                return;
            }
            cli.CPF = cpf;
        }

        Console.Write($"Novo endereço (Enter mantém): ");
        var end = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(end)) cli.Endereco = end;

        Console.Write($"Nova cidade (Enter mantém): ");
        var cid = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(cid)) cli.Cidade = cid;

        Console.Write($"Novo estado (UF) (Enter mantém): ");
        var uf = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(uf)) cli.Estado = uf;

        Console.Write($"Novo CEP (Enter mantém): ");
        var cep = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(cep)) cli.CEP = cep;

        Console.Write($"Ativo? (S/N, atual {(cli.Ativo ? "S" : "N")}): ");
        var ativoStr = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(ativoStr)) cli.Ativo = ativoStr.Trim().ToUpperInvariant().StartsWith("S");

        _context.SaveChanges();
        Console.WriteLine("Cliente atualizado.");
    }

    // ========== CRUD PEDIDOS ==========

    public void CriarPedido()
    {
        Console.WriteLine("=== CRIAR PEDIDO ===");

        Console.Write("Id do Cliente: ");
        if (!int.TryParse(Console.ReadLine(), out var clienteId))
        {
            Console.WriteLine("Id inválido.");
            return;
        }

        var cliente = _context.Clientes.Find(clienteId);
        if (cliente == null)
        {
            Console.WriteLine("Cliente não encontrado.");
            return;
        }

        using var tx = _context.Database.BeginTransaction();

        try
        {
            var numero = "PED-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var pedido = new Pedido
            {
                NumeroPedido = numero,
                DataPedido = DateTime.UtcNow,
                Status = StatusPedido.Confirmado, // alinhado ao ADO.NET e Seed
                ValorTotal = 0m,
                Desconto = 0m,
                Observacoes = "Criado via EF",
                ClienteId = cliente.Id
            };

            _context.Pedidos.Add(pedido);
            _context.SaveChanges();

            Console.WriteLine($"Pedido {numero} criado. Adicione itens (ProdutoId, Quantidade). Enter vazio para finalizar.");

            decimal total = 0m;
            var culture = new CultureInfo("pt-BR");

            while (true)
            {
                Console.Write("Produto Id (vazio = finalizar): ");
                var idStr = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(idStr)) break;

                if (!int.TryParse(idStr, out var prodId))
                {
                    Console.WriteLine("Id inválido.");
                    continue;
                }

                var produto = _context.Produtos.Find(prodId);
                if (produto == null)
                {
                    Console.WriteLine("Produto não encontrado.");
                    continue;
                }

                Console.Write("Quantidade: ");
                if (!int.TryParse(Console.ReadLine(), out var qtd) || qtd <= 0)
                {
                    Console.WriteLine("Quantidade inválida.");
                    continue;
                }

                if (produto.Estoque < qtd)
                {
                    Console.WriteLine($"Estoque insuficiente. Disponível: {produto.Estoque}.");
                    continue;
                }

                var item = new PedidoItem
                {
                    PedidoId = pedido.Id,
                    ProdutoId = produto.Id,
                    Quantidade = qtd,
                    PrecoUnitario = produto.Preco,
                    Desconto = 0m
                };

                _context.PedidoItens.Add(item);
                // baixa estoque
                produto.Estoque -= qtd;

                var subtotal = (produto.Preco * qtd);
                total += subtotal;

                Console.WriteLine($"➕ {qtd}x {produto.Nome} @ {produto.Preco.ToString("C", culture)} = {subtotal.ToString("C", culture)}");
            }

            // atualiza total
            pedido.ValorTotal = total;
            _context.SaveChanges();
            tx.Commit();

            Console.WriteLine($"Pedido salvo! Total: {total.ToString("C", new CultureInfo("pt-BR"))}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao criar pedido: {ex.Message}");
            try { tx.Rollback(); } catch { /* ignore */ }
        }
    }

    public void ListarPedidos()
    {
        Console.WriteLine("=== PEDIDOS ===");

        var pedidos = _context.Pedidos
            .Include(p => p.Cliente)
            .Include(p => p.Itens)
                .ThenInclude(i => i.Produto)
            .OrderByDescending(p => p.DataPedido)
            .ToList();

        if (!pedidos.Any())
        {
            Console.WriteLine("Nenhum pedido encontrado.");
            return;
        }

        var culture = new CultureInfo("pt-BR");

        foreach (var p in pedidos)
        {
            Console.WriteLine(new string('─', 74));
            Console.WriteLine($"Pedido: {p.NumeroPedido}  •  Cliente: {p.Cliente.Nome}  •  Data: {p.DataPedido:yyyy-MM-dd HH:mm}  •  Status: {p.Status}");
            Console.WriteLine(new string('-', 74));
            Console.WriteLine($"{"Produto",-36} {"Qtd",3} {"Preço",12} {"Desc.",8} {"Subtotal",12}");

            decimal totalCalc = 0m;
            foreach (var it in p.Itens)
            {
                var desc = it.Desconto ?? 0m;
                var sub = (it.Quantidade * it.PrecoUnitario) - desc;
                totalCalc += sub;
                Console.WriteLine($"{Trunc(it.Produto.Nome, 36),-36} {it.Quantidade,3} {it.PrecoUnitario.ToString("C", culture),12} {desc.ToString("C", culture),8} {sub.ToString("C", culture),12}");
            }

            Console.WriteLine(new string('-', 74));
            Console.WriteLine($"Total (calculado): {totalCalc.ToString("C", culture)}  | Total (armazenado): {p.ValorTotal.ToString("C", culture)}");
        }
    }

    public void AtualizarStatusPedido()
    {
        Console.WriteLine("=== ATUALIZAR STATUS PEDIDO ===");
        Console.Write("Informe o Id do pedido: ");
        if (!int.TryParse(Console.ReadLine(), out var id))
        {
            Console.WriteLine("Id inválido.");
            return;
        }

        var p = _context.Pedidos.Find(id);
        if (p == null)
        {
            Console.WriteLine("Pedido não encontrado.");
            return;
        }

        Console.WriteLine($"Status atual: {p.Status}");
        Console.WriteLine("Status disponíveis:");
        foreach (var s in Enum.GetValues<StatusPedido>())
        {
            Console.WriteLine($"  {(int)s} - {s}");
        }

        Console.Write("Novo status (número): ");
        if (!int.TryParse(Console.ReadLine(), out var stNum) || !Enum.IsDefined(typeof(StatusPedido), stNum))
        {
            Console.WriteLine("Status inválido.");
            return;
        }

        var novo = (StatusPedido)stNum;

        // regras de transição (simples)
        bool valido = p.Status switch
        {
            StatusPedido.Pendente => novo is StatusPedido.Confirmado or StatusPedido.Cancelado,
            StatusPedido.Confirmado => novo is StatusPedido.EmAndamento or StatusPedido.Cancelado,
            StatusPedido.EmAndamento => novo is StatusPedido.Entregue or StatusPedido.Cancelado,
            StatusPedido.Entregue => false,
            StatusPedido.Cancelado => false,
            _ => false
        };

        if (!valido)
        {
            Console.WriteLine("Transição de status inválida.");
            return;
        }

        // orientação: para cancelar e devolver estoque, use o fluxo 'CancelarPedido'
        if (novo == StatusPedido.Cancelado)
        {
            Console.WriteLine("Para cancelar com devolução de estoque, use a opção 'Cancelar Pedido'.");
            return;
        }

        p.Status = novo;
        _context.SaveChanges();
        Console.WriteLine("Status atualizado.");
    }

    public void CancelarPedido()
    {
        Console.WriteLine("=== CANCELAR PEDIDO ===");
        Console.Write("Informe o Id do pedido: ");
        if (!int.TryParse(Console.ReadLine(), out var id))
        {
            Console.WriteLine("Id inválido.");
            return;
        }

        var pedido = _context.Pedidos
            .Include(p => p.Itens)
            .FirstOrDefault(p => p.Id == id);

        if (pedido == null)
        {
            Console.WriteLine("Pedido não encontrado.");
            return;
        }

        if (pedido.Status is not StatusPedido.Pendente and not StatusPedido.Confirmado)
        {
            Console.WriteLine("Só é permitido cancelar pedidos em status Pendente ou Confirmado.");
            return;
        }

        using var tx = _context.Database.BeginTransaction();
        try
        {
            // Devolver estoque
            foreach (var it in pedido.Itens)
            {
                var produto = _context.Produtos.Find(it.ProdutoId);
                if (produto != null)
                    produto.Estoque += it.Quantidade;
            }

            pedido.Status = StatusPedido.Cancelado;
            pedido.Observacoes = (pedido.Observacoes ?? "") + $" | Cancelado em {DateTime.Now:dd/MM/yyyy HH:mm}";
            _context.SaveChanges();
            tx.Commit();
            Console.WriteLine("Pedido cancelado e estoque devolvido.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao cancelar: {ex.Message}");
            try { tx.Rollback(); } catch { /* ignore */ }
        }
    }

    // ========== CONSULTAS LINQ AVANÇADAS ==========

    public void ConsultasAvancadas()
    {
        Console.WriteLine("=== CONSULTAS LINQ ===");
        Console.WriteLine("1. Produtos mais vendidos");
        Console.WriteLine("2. Clientes com mais pedidos");
        Console.WriteLine("3. Faturamento por categoria");
        Console.WriteLine("4. Pedidos por período");
        Console.WriteLine("5. Produtos em estoque baixo");
        Console.WriteLine("6. Vendas mensais");
        Console.WriteLine("7. Top 10 clientes por valor");

        var opcao = Console.ReadLine();

        switch (opcao)
        {
            case "1": ProdutosMaisVendidos(); break;
            case "2": ClientesComMaisPedidos(); break;
            case "3": FaturamentoPorCategoria(); break;
            case "4": PedidosPorPeriodo(); break;
            case "5": ProdutosEstoqueBaixo(); break;
            case "6": AnaliseVendasMensal(); break;
            case "7": TopClientesPorValor(); break;
            default: Console.WriteLine("Opção inválida."); break;
        }
    }

    private void ProdutosMaisVendidos()
    {
        const StatusPedido CANCELADO = StatusPedido.Cancelado;

        var query = _context.PedidoItens
            .Where(i => i.Pedido.Status != CANCELADO)
            .GroupBy(i => new { i.ProdutoId, i.Produto.Nome, Categoria = i.Produto.Categoria.Nome })
            .Select(g => new
            {
                g.Key.ProdutoId,
                Produto = g.Key.Nome,
                Categoria = g.Key.Categoria,
                Quantidade = g.Sum(x => x.Quantidade)
            })
            .OrderByDescending(x => x.Quantidade)
            .ThenBy(x => x.Produto)
            .ToList();

        if (!query.Any())
        {
            Console.WriteLine("Sem vendas.");
            return;
        }

        Console.WriteLine($"{"Produto",-36} {"Categoria",-18} {"Qtd",5}");
        Console.WriteLine(new string('-', 62));
        foreach (var x in query)
            Console.WriteLine($"{Trunc(x.Produto, 36),-36} {Trunc(x.Categoria, 18),-18} {x.Quantidade,5}");
    }

    private void ClientesComMaisPedidos()
    {
        const StatusPedido CANCELADO = StatusPedido.Cancelado;

        var query = _context.Pedidos
            .Where(p => p.Status != CANCELADO)
            .GroupBy(p => new { p.ClienteId, p.Cliente.Nome })
            .Select(g => new
            {
                g.Key.ClienteId,
                Cliente = g.Key.Nome,
                Quantidade = g.Count()
            })
            .OrderByDescending(x => x.Quantidade)
            .ThenBy(x => x.Cliente)
            .ToList();

        if (!query.Any())
        {
            Console.WriteLine("Sem pedidos.");
            return;
        }

        foreach (var x in query)
            Console.WriteLine($"{x.Cliente} • Pedidos: {x.Quantidade}");
    }

    private void FaturamentoPorCategoria()
    {
        const StatusPedido CANCELADO = StatusPedido.Cancelado;

        var itensValidos = _context.PedidoItens
            .Where(i => i.Pedido.Status != CANCELADO);

        var query = itensValidos
            .GroupBy(i => new { Categoria = i.Produto.Categoria.Nome })
            .Select(g => new
            {
                g.Key.Categoria,
                Faturamento = g.Sum(x => (x.Quantidade * x.PrecoUnitario) - (x.Desconto ?? 0m)),
                ProdutosVendidos = g.Select(x => x.ProdutoId).Distinct().Count(),
                Pedidos = g.Select(x => x.PedidoId).Distinct().Count()
            })
            .Select(x => new
            {
                x.Categoria,
                x.Faturamento,
                x.ProdutosVendidos,
                x.Pedidos,
                TicketMedio = x.Pedidos > 0 ? x.Faturamento / x.Pedidos : 0m
            })
            .OrderByDescending(x => x.Faturamento)
            .ToList();

        if (!query.Any())
        {
            Console.WriteLine("Sem faturamento.");
            return;
        }

        var culture = new CultureInfo("pt-BR");
        Console.WriteLine($"{"Categoria",-18} {"Fat.",14} {"Produtos",8} {"Pedidos",8} {"Ticket Médio",14}");
        Console.WriteLine(new string('-', 68));

        foreach (var x in query)
        {
            Console.WriteLine($"{Trunc(x.Categoria, 18),-18} {x.Faturamento.ToString("C", culture),14} {x.ProdutosVendidos,8} {x.Pedidos,8} {x.TicketMedio.ToString("C", culture),14}");
        }
    }

    private void PedidosPorPeriodo()
    {
        Console.Write("Data início (yyyy-MM-dd): ");
        if (!DateTime.TryParse(Console.ReadLine(), out var ini))
        {
            Console.WriteLine("Data inválida.");
            return;
        }

        Console.Write("Data fim (yyyy-MM-dd): ");
        if (!DateTime.TryParse(Console.ReadLine(), out var fim))
        {
            Console.WriteLine("Data inválida.");
            return;
        }

        fim = fim.Date.AddDays(1).AddTicks(-1); // incluir o dia final

        const StatusPedido CANCELADO = StatusPedido.Cancelado;

        var query = _context.Pedidos
            .Where(p => p.Status != CANCELADO && p.DataPedido >= ini && p.DataPedido <= fim)
            .GroupBy(p => p.DataPedido.Date)
            .Select(g => new
            {
                Data = g.Key,
                Qtde = g.Count(),
                Total = g.Sum(x => x.ValorTotal - (x.Desconto ?? 0m))
            })
            .OrderBy(x => x.Data)
            .ToList();

        if (!query.Any())
        {
            Console.WriteLine("Sem pedidos no período.");
            return;
        }

        var culture = new CultureInfo("pt-BR");
        Console.WriteLine($"{"Data",-12} {"Pedidos",8} {"Total",14}");
        Console.WriteLine(new string('-', 36));
        foreach (var x in query)
        {
            Console.WriteLine($"{x.Data:yyyy-MM-dd,-12} {x.Qtde,8} {x.Total.ToString("C", culture),14}");
        }
    }

    private void ProdutosEstoqueBaixo()
    {
        var lista = _context.Produtos
            .Include(p => p.Categoria)
            .Where(p => p.Estoque < 20)
            .OrderBy(p => p.Estoque)
            .ThenBy(p => p.Nome)
            .ToList();

        if (!lista.Any())
        {
            Console.WriteLine("Nenhum produto com estoque baixo (<20).");
            return;
        }

        Console.WriteLine($"{"Produto",-36} {"Categoria",-18} {"Estoque",8}");
        Console.WriteLine(new string('-', 64));
        foreach (var p in lista)
        {
            Console.WriteLine($"{Trunc(p.Nome, 36),-36} {Trunc(p.Categoria.Nome, 18),-18} {p.Estoque,8}");
        }
    }

    private void AnaliseVendasMensal()
    {
        const StatusPedido CANCELADO = StatusPedido.Cancelado;

        var dados = _context.Pedidos
            .Where(p => p.Status != CANCELADO && p.DataPedido >= DateTime.UtcNow.AddMonths(-12))
            .GroupBy(p => new { p.DataPedido.Year, p.DataPedido.Month })
            .Select(g => new
            {
                Ano = g.Key.Year,
                Mes = g.Key.Month,
                Qtde = g.Count(),
                Total = g.Sum(x => x.ValorTotal - (x.Desconto ?? 0m))
            })
            .OrderBy(x => x.Ano).ThenBy(x => x.Mes)
            .ToList();

        if (!dados.Any())
        {
            Console.WriteLine("Sem dados.");
            return;
        }

        var culture = new CultureInfo("pt-BR");
        Console.WriteLine($"{"Mês",-7} {"Pedidos",8} {"Total",14} {"Δ% vs ant.",12}");
        Console.WriteLine(new string('-', 48));

        decimal? anterior = null;
        foreach (var x in dados)
        {
            var label = $"{x.Ano}-{x.Mes:00}";
            string delta = "—";
            if (anterior is not null && anterior.Value != 0)
            {
                delta = $"{((x.Total - anterior.Value) / anterior.Value * 100m):0.##}%";
            }
            else if (anterior is not null && anterior.Value == 0 && x.Total > 0)
            {
                delta = "+∞";
            }

            Console.WriteLine($"{label,-7} {x.Qtde,8} {x.Total.ToString("C", culture),14} {delta,12}");
            anterior = x.Total;
        }
    }

    private void TopClientesPorValor()
    {
        const StatusPedido CANCELADO = StatusPedido.Cancelado;

        var query = _context.Pedidos
            .Where(p => p.Status != CANCELADO)
            .GroupBy(p => new { p.ClienteId, p.Cliente.Nome })
            .Select(g => new
            {
                g.Key.ClienteId,
                Cliente = g.Key.Nome,
                Valor = g.Sum(x => x.ValorTotal - (x.Desconto ?? 0m))
            })
            .OrderByDescending(x => x.Valor)
            .ThenBy(x => x.Cliente)
            .Take(10)
            .ToList();

        if (!query.Any())
        {
            Console.WriteLine("Sem dados.");
            return;
        }

        var culture = new CultureInfo("pt-BR");
        int pos = 1;
        foreach (var x in query)
        {
            Console.WriteLine($"{pos,2}. {x.Cliente,-30} {x.Valor.ToString("C", culture)}");
            pos++;
        }
    }

    // ========== RELATÓRIOS GERAIS ==========

    public void RelatoriosGerais()
    {
        Console.WriteLine("=== RELATÓRIOS GERAIS ===");
        Console.WriteLine("1. Dashboard executivo");
        Console.WriteLine("2. Relatório de estoque");
        Console.WriteLine("3. Análise de clientes");

        var opcao = Console.ReadLine();

        switch (opcao)
        {
            case "1": DashboardExecutivo(); break;
            case "2": RelatorioEstoque(); break;
            case "3": AnaliseClientes(); break;
            default: Console.WriteLine("Opção inválida."); break;
        }
    }

    private void DashboardExecutivo()
    {
        const StatusPedido CANCELADO = StatusPedido.Cancelado;

        var totalPedidos = _context.Pedidos.Count();
        var pedidosValidos = _context.Pedidos.Where(p => p.Status != CANCELADO).ToList();
        var totalValidos = pedidosValidos.Count;
        var faturamento = pedidosValidos.Sum(p => p.ValorTotal - (p.Desconto ?? 0m));
        var ticketMedio = totalValidos > 0 ? faturamento / totalValidos : 0m;

        var produtos = _context.Produtos.ToList();
        var produtosAtivos = produtos.Count(p => p.Ativo);
        var produtosEstoque = produtos.Sum(p => p.Estoque);

        var clientesAtivos = _context.Clientes.Count(c => c.Ativo);

        // últimos 6 meses
        var ultimos6 = _context.Pedidos
            .Where(p => p.Status != CANCELADO && p.DataPedido >= DateTime.UtcNow.AddMonths(-6))
            .GroupBy(p => new { p.DataPedido.Year, p.DataPedido.Month })
            .Select(g => new
            {
                Label = $"{g.Key.Year}-{g.Key.Month:00}",
                Total = g.Sum(x => x.ValorTotal - (x.Desconto ?? 0m))
            })
            .OrderBy(x => x.Label)
            .ToList();

        var culture = new CultureInfo("pt-BR");
        Console.WriteLine($"Pedidos (todos): {totalPedidos} • Pedidos válidos: {totalValidos}");
        Console.WriteLine($"Faturamento: {faturamento.ToString("C", culture)} • Ticket médio: {ticketMedio.ToString("C", culture)}");
        Console.WriteLine($"Produtos ativos: {produtosAtivos} • Itens em estoque (soma): {produtosEstoque}");
        Console.WriteLine($"Clientes ativos: {clientesAtivos}");
        Console.WriteLine("Faturamento (últimos 6 meses):");
        foreach (var m in ultimos6)
            Console.WriteLine($"  {m.Label}: {m.Total.ToString("C", culture)}");
    }

    private void RelatorioEstoque()
    {
        var porCategoria = _context.Produtos
            .GroupBy(p => p.Categoria.Nome)
            .Select(g => new
            {
                Categoria = g.Key,
                QtdeProdutos = g.Count(),
                ItensEstoque = g.Sum(x => x.Estoque),
                ValorTotal = g.Sum(x => x.Estoque * x.Preco)
            })
            .OrderBy(x => x.Categoria)
            .ToList();

        var zerados = _context.Produtos.Where(p => p.Estoque == 0).Select(p => p.Nome).OrderBy(x => x).ToList();
        var baixo = _context.Produtos.Where(p => p.Estoque > 0 && p.Estoque < 20).Select(p => p.Nome).OrderBy(x => x).ToList();

        var culture = new CultureInfo("pt-BR");
        Console.WriteLine("Produtos por categoria:");
        foreach (var x in porCategoria)
            Console.WriteLine($"- {x.Categoria}: {x.QtdeProdutos} produtos • Itens em estoque: {x.ItensEstoque} • Valor: {x.ValorTotal.ToString("C", culture)}");

        Console.WriteLine("\nProdutos com estoque zerado:");
        if (zerados.Any()) foreach (var n in zerados) Console.WriteLine($"  - {n}");
        else Console.WriteLine("  (nenhum)");

        Console.WriteLine("\nProdutos com estoque baixo (<20):");
        if (baixo.Any()) foreach (var n in baixo) Console.WriteLine($"  - {n}");
        else Console.WriteLine("  (nenhum)");
    }

    private void AnaliseClientes()
    {
        var porEstado = _context.Clientes
            .GroupBy(c => c.Estado)
            .Select(g => new { Estado = g.Key, Qtde = g.Count() })
            .OrderByDescending(x => x.Qtde)
            .ToList();

        const StatusPedido CANCELADO = StatusPedido.Cancelado;

        var valorMedioCliente = _context.Pedidos
            .Where(p => p.Status != CANCELADO)
            .GroupBy(p => p.ClienteId)
            .Select(g => new { ClienteId = g.Key, Valor = g.Sum(x => x.ValorTotal - (x.Desconto ?? 0m)) })
            .DefaultIfEmpty(new { ClienteId = 0, Valor = 0m })
            .Average(x => x.Valor);

        Console.WriteLine("Clientes por estado (UF):");
        foreach (var e in porEstado)
            Console.WriteLine($"  {e.Estado ?? "(sem UF)"}: {e.Qtde}");

        var culture = new CultureInfo("pt-BR");
        Console.WriteLine($"\nValor médio por cliente: {valorMedioCliente.ToString("C", culture)}");
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    // ========== HELPERS ==========

    private static bool TryParseDecimal(string? input, out decimal value)
    {
        // aceita 10,50 e 10.50
        return decimal.TryParse(input, NumberStyles.Number, new CultureInfo("pt-BR"), out value)
               || decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    private static string SomenteNumeros(string s) => Regex.Replace(s, "[^0-9]", "");

    private static string Trunc(string? s, int max)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return s.Length <= max ? s : s.Substring(0, max - 1) + "…";
    }
}