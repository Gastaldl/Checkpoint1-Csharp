using System;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace CheckPoint1.Services;

public class AdoNetService : IDisposable
{
    private readonly string _connectionString;

    public AdoNetService()
    {
        // Mesmo arquivo usado pelo EF (caminho absoluto em bin/...)
        var dbPath = Path.Combine(AppContext.BaseDirectory, "loja.db");
        _connectionString = $"Data Source={dbPath};Version=3;Foreign Keys=True;";
    }

    // ========== CONSULTAS COMPLEXAS ==========

    public void RelatorioVendasCompleto()
    {
        Console.WriteLine("=== RELATÓRIO VENDAS COMPLETO (ADO.NET) ===");
        const string sql = @"
SELECT
    p.Id               AS PedidoId,
    p.NumeroPedido     AS NumeroPedido,
    p.DataPedido       AS DataPedido,
    c.Nome             AS NomeCliente,
    pr.Nome            AS NomeProduto,
    pi.Quantidade      AS Quantidade,
    pi.PrecoUnitario   AS PrecoUnitario,
    COALESCE(pi.Desconto,0) AS Desconto,
    (pi.Quantidade * pi.PrecoUnitario) - COALESCE(pi.Desconto,0) AS Subtotal
FROM Pedidos p
JOIN Clientes c     ON c.Id = p.ClienteId
JOIN PedidoItens pi ON pi.PedidoId = p.Id
JOIN Produtos pr    ON pr.Id = pi.ProdutoId
ORDER BY p.DataPedido, p.Id, pi.Id;";

        using var conn = GetConnection();
        conn.Open();
        using var cmd = new SQLiteCommand(sql, conn);
        using var rd = cmd.ExecuteReader();

        var culture = new CultureInfo("pt-BR");
        int? lastPedido = null;

        int linhas = 0;
        while (rd.Read())
        {
            linhas++;
            var pedidoId = Convert.ToInt32(rd["PedidoId"]);
            if (lastPedido != pedidoId)
            {
                Console.WriteLine();
                Console.WriteLine(new string('─', 72));
                Console.WriteLine($"Pedido: {rd["NumeroPedido"]}  |  Cliente: {rd["NomeCliente"]}  |  Data: {Convert.ToDateTime(rd["DataPedido"]):yyyy-MM-dd HH:mm}");
                Console.WriteLine(new string('-', 72));
                Console.WriteLine($"{"Produto",-36} {"Qtd",3} {"Preço",12} {"Desc.",8} {"Subtotal",12}");
                Console.WriteLine(new string('-', 72));
                lastPedido = pedidoId;
            }

            var nomeProduto = Convert.ToString(rd["NomeProduto"]);
            var quantidade = Convert.ToInt32(rd["Quantidade"]);
            var precoUnitario = Convert.ToDecimal(rd["PrecoUnitario"]);
            var desconto = Convert.ToDecimal(rd["Desconto"]);
            var subtotal = (quantidade * precoUnitario) - desconto;

            Console.WriteLine($"{Trunc(nomeProduto, 36),-36} {quantidade,3} {precoUnitario.ToString("C", culture),12} {desconto.ToString("C", culture),8} {subtotal.ToString("C", culture),12}");
        }

        if (linhas == 0) Console.WriteLine("Nenhuma venda encontrada.");
    }

    public void FaturamentoPorCliente()
    {
        Console.WriteLine("=== FATURAMENTO POR CLIENTE ===");
        const int CANCELADO = 5; // StatusPedido.Cancelado
        const string sql = @"
SELECT
    c.Id                 AS ClienteId,
    c.Nome               AS NomeCliente,
    COUNT(p.Id)          AS QtdePedidos,
    COALESCE(SUM(p.ValorTotal - COALESCE(p.Desconto,0)),0) AS Faturamento
FROM Clientes c
LEFT JOIN Pedidos p ON p.ClienteId = c.Id AND p.Status <> @statusCancelado
GROUP BY c.Id, c.Nome
ORDER BY Faturamento DESC;";

        using var conn = GetConnection();
        conn.Open();
        using var cmd = new SQLiteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@statusCancelado", CANCELADO);

        using var rd = cmd.ExecuteReader();
        var culture = new CultureInfo("pt-BR");

        Console.WriteLine($"{"Cliente",-32} {"Pedidos",7} {"Faturamento",15} {"Ticket Médio",15}");
        Console.WriteLine(new string('-', 73));

        int linhas = 0;
        while (rd.Read())
        {
            linhas++;
            var nome = Convert.ToString(rd["NomeCliente"]);
            var qtd = Convert.ToInt32(rd["QtdePedidos"]);
            var fat = Convert.ToDecimal(rd["Faturamento"]);
            var ticket = qtd > 0 ? fat / qtd : 0m;

            Console.WriteLine($"{Trunc(nome, 32),-32} {qtd,7} {fat.ToString("C", culture),15} {ticket.ToString("C", culture),15}");
        }

        if (linhas == 0) Console.WriteLine("Sem dados de faturamento.");
    }

    public void ProdutosSemVenda()
    {
        Console.WriteLine("=== PRODUTOS SEM VENDAS ===");
        const string sql = @"
SELECT
    pr.Id,
    cat.Nome    AS Categoria,
    pr.Nome     AS Produto,
    pr.Preco,
    pr.Estoque,
    (pr.Preco * pr.Estoque) AS ValorParado
FROM Produtos pr
JOIN Categorias cat ON cat.Id = pr.CategoriaId
LEFT JOIN PedidoItens pi ON pi.ProdutoId = pr.Id
WHERE pi.Id IS NULL
ORDER BY cat.Nome, pr.Nome;";

        using var conn = GetConnection();
        conn.Open();
        using var cmd = new SQLiteCommand(sql, conn);
        using var rd = cmd.ExecuteReader();

        var culture = new CultureInfo("pt-BR");
        Console.WriteLine($"{"Categoria",-18} {"Produto",-28} {"Preço",12} {"Estoque",8} {"Valor Parado",14}");
        Console.WriteLine(new string('-', 86));

        int linhas = 0;
        decimal totalParado = 0m;

        while (rd.Read())
        {
            linhas++;
            var cat = Convert.ToString(rd["Categoria"]);
            var prod = Convert.ToString(rd["Produto"]);
            var preco = Convert.ToDecimal(rd["Preco"]);
            var estq = Convert.ToInt32(rd["Estoque"]);
            var parado = preco * estq;

            totalParado += parado;
            Console.WriteLine($"{Trunc(cat, 18),-18} {Trunc(prod, 28),-28} {preco.ToString("C", culture),12} {estq,8} {parado.ToString("C", culture),14}");
        }

        if (linhas == 0)
            Console.WriteLine("Nenhum produto parado no estoque.");
        else
        {
            Console.WriteLine(new string('-', 86));
            Console.WriteLine($"Total em estoque parado: {totalParado.ToString("C", culture)}");
        }
    }

    // ========== OPERAÇÕES DE DADOS ==========

    public void AtualizarEstoqueLote()
    {
        Console.WriteLine("=== ATUALIZAR ESTOQUE EM LOTE ===");

        using var conn = GetConnection();
        conn.Open();

        // Listar categorias
        using (var cmdCats = new SQLiteCommand("SELECT Id, Nome FROM Categorias ORDER BY Nome;", conn))
        using (var rd = cmdCats.ExecuteReader())
        {
            Console.WriteLine("Categorias:");
            while (rd.Read())
            {
                Console.WriteLine($"  {rd["Id"],2} - {rd["Nome"]}");
            }
        }

        Console.Write("Informe o ID da categoria: ");
        if (!int.TryParse(Console.ReadLine(), out var categoriaId))
        {
            Console.WriteLine("ID inválido.");
            return;
        }

        // Buscar produtos da categoria
        var produtos = new List<(int Id, string Nome, int Estoque)>();
        using (var cmdProd = new SQLiteCommand("SELECT Id, Nome, Estoque FROM Produtos WHERE CategoriaId = @cat ORDER BY Nome;", conn))
        {
            cmdProd.Parameters.AddWithValue("@cat", categoriaId);
            using var rd = cmdProd.ExecuteReader();
            while (rd.Read())
            {
                produtos.Add((Convert.ToInt32(rd["Id"]), Convert.ToString(rd["Nome"])!, Convert.ToInt32(rd["Estoque"])));
            }
        }

        if (produtos.Count == 0)
        {
            Console.WriteLine("Nenhum produto encontrado para a categoria.");
            return;
        }

        Console.WriteLine("\nPara cada produto, digite a nova quantidade (ou deixe em branco para manter):");

        using var tx = conn.BeginTransaction();
        int atualizados = 0;

        foreach (var p in produtos)
        {
            Console.Write($"[{p.Id}] {p.Nome} (estoque atual: {p.Estoque}) => novo estoque: ");
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input)) continue;
            if (!int.TryParse(input, out var novo)) { Console.WriteLine("Valor inválido, ignorado."); continue; }
            if (novo < 0) { Console.WriteLine("Estoque não pode ser negativo, ignorado."); continue; }

            using var cmdUp = new SQLiteCommand("UPDATE Produtos SET Estoque = @novo WHERE Id = @id;", conn, tx);
            cmdUp.Parameters.AddWithValue("@novo", novo);
            cmdUp.Parameters.AddWithValue("@id", p.Id);
            var rows = cmdUp.ExecuteNonQuery();
            if (rows > 0) atualizados += rows;
        }

        tx.Commit();
        Console.WriteLine($"\nRegistros atualizados: {atualizados}");
    }

    public void InserirPedidoCompleto()
    {
        Console.WriteLine("=== INSERIR PEDIDO COMPLETO ===");

        using var conn = GetConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        try
        {
            // Selecionar cliente
            Console.Write("Informe o e-mail do cliente (ou ID): ");
            var ident = (Console.ReadLine() ?? "").Trim();

            int clienteId = 0;
            string clienteNome = "";

            if (int.TryParse(ident, out var idDigitado))
            {
                (clienteId, clienteNome) = BuscarClientePorId(conn, tx, idDigitado) ?? (0, "");
            }
            else
            {
                (clienteId, clienteNome) = BuscarClientePorEmail(conn, tx, ident) ?? (0, "");
            }

            if (clienteId == 0)
            {
                Console.WriteLine("Cliente não encontrado.");
                tx.Rollback();
                return;
            }

            var numeroPedido = "PED-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var data = DateTime.UtcNow;
            const int STATUS_CONFIRMADO = 2;

            // Inserir pedido (valor inicial 0)
            long pedidoId;
            using (var cmdPed = new SQLiteCommand(@"
INSERT INTO Pedidos (NumeroPedido, DataPedido, Status, ValorTotal, Desconto, Observacoes, ClienteId)
VALUES (@num, @data, @status, 0, 0, @obs, @cli);
SELECT last_insert_rowid();", conn, tx))
            {
                cmdPed.Parameters.AddWithValue("@num", numeroPedido);
                cmdPed.Parameters.AddWithValue("@data", data);
                cmdPed.Parameters.AddWithValue("@status", STATUS_CONFIRMADO);
                cmdPed.Parameters.AddWithValue("@obs", $"Pedido inserido via ADO.NET em {DateTime.Now:dd/MM/yyyy HH:mm}");
                cmdPed.Parameters.AddWithValue("@cli", clienteId);

                pedidoId = (long)(cmdPed.ExecuteScalar() ?? 0L);
            }

            if (pedidoId == 0)
            {
                Console.WriteLine("Falha ao inserir pedido.");
                tx.Rollback();
                return;
            }

            Console.WriteLine($"Novo pedido criado: {numeroPedido} (Cliente: {clienteNome})");
            Console.WriteLine("Adicione itens (produtoId e quantidade). Enter vazio para finalizar.");

            decimal total = 0m;
            var culture = new CultureInfo("pt-BR");

            while (true)
            {
                Console.Write("\nProduto ID (vazio = finalizar): ");
                var pInput = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(pInput)) break;
                if (!int.TryParse(pInput, out var prodId)) { Console.WriteLine("ID inválido."); continue; }

                Console.Write("Quantidade: ");
                if (!int.TryParse(Console.ReadLine(), out var qtd) || qtd <= 0) { Console.WriteLine("Qtd inválida."); continue; }

                // Busca produto
                (int Id, string Nome, decimal Preco, int Estoque)? prod = BuscarProdutoPorId(conn, tx, prodId);
                if (prod is null)
                {
                    Console.WriteLine("Produto não encontrado.");
                    continue;
                }

                if (prod.Value.Estoque < qtd)
                {
                    Console.WriteLine($"Estoque insuficiente de '{prod.Value.Nome}'. Disponível: {prod.Value.Estoque}.");
                    continue;
                }

                // Inserir item (sem desconto, ou poderia perguntar)
                using (var cmdItem = new SQLiteCommand(@"
INSERT INTO PedidoItens (PedidoId, ProdutoId, Quantidade, PrecoUnitario, Desconto)
VALUES (@ped, @prod, @qtd, @preco, @desc);", conn, tx))
                {
                    cmdItem.Parameters.AddWithValue("@ped", pedidoId);
                    cmdItem.Parameters.AddWithValue("@prod", prod.Value.Id);
                    cmdItem.Parameters.AddWithValue("@qtd", qtd);
                    cmdItem.Parameters.AddWithValue("@preco", prod.Value.Preco);
                    cmdItem.Parameters.AddWithValue("@desc", 0m);
                    cmdItem.ExecuteNonQuery();
                }

                // Atualiza estoque
                using (var cmdEst = new SQLiteCommand("UPDATE Produtos SET Estoque = Estoque - @qtd WHERE Id = @id;", conn, tx))
                {
                    cmdEst.Parameters.AddWithValue("@qtd", qtd);
                    cmdEst.Parameters.AddWithValue("@id", prod.Value.Id);
                    cmdEst.ExecuteNonQuery();
                }

                var subtotal = (prod.Value.Preco * qtd);
                total += subtotal;

                Console.WriteLine($"➕ {qtd}x {prod.Value.Nome} @ {prod.Value.Preco.ToString("C", culture)}  =  {subtotal.ToString("C", culture)}");
            }

            // Atualiza total do pedido
            using (var cmdUp = new SQLiteCommand("UPDATE Pedidos SET ValorTotal = @total WHERE Id = @id;", conn, tx))
            {
                cmdUp.Parameters.AddWithValue("@total", total);
                cmdUp.Parameters.AddWithValue("@id", pedidoId);
                cmdUp.ExecuteNonQuery();
            }

            tx.Commit();
            Console.WriteLine($"\nPedido salvo com sucesso! Total: {total.ToString("C", new CultureInfo("pt-BR"))}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao inserir pedido: {ex.Message}");
            try { tx.Rollback(); } catch { /* ignore */ }
        }
    }

    public void ExcluirDadosAntigos()
    {
        Console.WriteLine("=== EXCLUIR DADOS ANTIGOS ===");
        const int CANCELADO = 5;

        using var conn = GetConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        try
        {
            // Contar para informar antes
            using (var cmdCount = new SQLiteCommand(@"
SELECT COUNT(1) FROM Pedidos
WHERE Status = @st
  AND DATE(DataPedido) < DATE('now','-6 months');", conn, tx))
            {
                cmdCount.Parameters.AddWithValue("@st", CANCELADO);
                var qtd = Convert.ToInt32(cmdCount.ExecuteScalar() ?? 0);
                Console.WriteLine($"Pedidos cancelados há mais de 6 meses: {qtd}");
            }

            using (var cmdDel = new SQLiteCommand(@"
DELETE FROM Pedidos
WHERE Status = @st
  AND DATE(DataPedido) < DATE('now','-6 months');", conn, tx))
            {
                cmdDel.Parameters.AddWithValue("@st", CANCELADO);
                var apagados = cmdDel.ExecuteNonQuery();
                tx.Commit();
                Console.WriteLine($"Registros excluídos: {apagados}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao excluir: {ex.Message}");
            try { tx.Rollback(); } catch { /* ignore */ }
        }
    }

    public void ProcessarDevolucao()
    {
        Console.WriteLine("=== PROCESSAR DEVOLUÇÃO ===");
        Console.Write("Informe o Número do Pedido (ex.: PED-2025-0001): ");
        var numero = (Console.ReadLine() ?? "").Trim();
        if (string.IsNullOrWhiteSpace(numero))
        {
            Console.WriteLine("Número inválido.");
            return;
        }

        using var conn = GetConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        try
        {
            long pedidoId = 0;
            int status = 0;

            using (var cmdPed = new SQLiteCommand("SELECT Id, Status FROM Pedidos WHERE NumeroPedido = @num;", conn, tx))
            {
                cmdPed.Parameters.AddWithValue("@num", numero);
                using var rd = cmdPed.ExecuteReader();
                if (rd.Read())
                {
                    pedidoId = Convert.ToInt64(rd["Id"]);
                    status = Convert.ToInt32(rd["Status"]);
                }
            }

            if (pedidoId == 0)
            {
                Console.WriteLine("Pedido não encontrado.");
                tx.Rollback();
                return;
            }

            const int CANCELADO = 5;
            if (status == CANCELADO)
            {
                Console.WriteLine("Pedido já cancelado; não é possível devolver novamente.");
                tx.Rollback();
                return;
            }

            // Buscar itens e devolver estoque
            using (var cmdItens = new SQLiteCommand(@"
SELECT ProdutoId, Quantidade FROM PedidoItens WHERE PedidoId = @id;", conn, tx))
            {
                cmdItens.Parameters.AddWithValue("@id", pedidoId);
                using var rd = cmdItens.ExecuteReader();
                while (rd.Read())
                {
                    var prodId = Convert.ToInt32(rd["ProdutoId"]);
                    var qtd = Convert.ToInt32(rd["Quantidade"]);

                    using var cmdEst = new SQLiteCommand("UPDATE Produtos SET Estoque = Estoque + @qtd WHERE Id = @id;", conn, tx);
                    cmdEst.Parameters.AddWithValue("@qtd", qtd);
                    cmdEst.Parameters.AddWithValue("@id", prodId);
                    cmdEst.ExecuteNonQuery();
                }
            }

            // Atualiza status do pedido para Cancelado
            using (var cmdUp = new SQLiteCommand(@"
UPDATE Pedidos
SET Status = @st,
    Observacoes = COALESCE(Observacoes,'') || ' | Devolvido em ' || @dt
WHERE Id = @id;", conn, tx))
            {
                cmdUp.Parameters.AddWithValue("@st", CANCELADO);
                cmdUp.Parameters.AddWithValue("@dt", DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                cmdUp.Parameters.AddWithValue("@id", pedidoId);
                cmdUp.ExecuteNonQuery();
            }

            tx.Commit();
            Console.WriteLine("Devolução processada e estoque ajustado.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro na devolução: {ex.Message}");
            try { tx.Rollback(); } catch { /* ignore */ }
        }
    }

    // ========== ANÁLISES PERFORMANCE ==========

    public void AnalisarPerformanceVendas()
    {
        Console.WriteLine("=== ANÁLISE PERFORMANCE VENDAS ===");
        const int CANCELADO = 5;

        const string sql = @"
SELECT
    strftime('%Y-%m', DataPedido) AS Mes,
    COALESCE(SUM(ValorTotal - COALESCE(Desconto,0)),0) AS Total
FROM Pedidos
WHERE Status <> @st
  AND DATE(DataPedido) >= DATE('now','-12 months')
GROUP BY strftime('%Y-%m', DataPedido)
ORDER BY Mes;";

        using var conn = GetConnection();
        conn.Open();
        using var cmd = new SQLiteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@st", CANCELADO);
        using var rd = cmd.ExecuteReader();

        var linhas = new List<(string Mes, decimal Total)>();
        while (rd.Read())
        {
            linhas.Add((Convert.ToString(rd["Mes"])!, Convert.ToDecimal(rd["Total"])));
        }

        if (linhas.Count == 0)
        {
            Console.WriteLine("Sem dados de vendas no período.");
            return;
        }

        var culture = new CultureInfo("pt-BR");
        Console.WriteLine($"{"Mês",-8} {"Vendas",14} {"Δ% vs mês ant.",15}");
        Console.WriteLine(new string('-', 42));

        decimal? anterior = null;
        foreach (var row in linhas)
        {
            string deltaStr = "—";
            if (anterior is not null && anterior.Value != 0)
            {
                var delta = ((row.Total - anterior.Value) / anterior.Value) * 100m;
                deltaStr = $"{delta:0.##}%";
            }
            else if (anterior is not null && anterior.Value == 0 && row.Total > 0)
            {
                deltaStr = "+∞";
            }

            Console.WriteLine($"{row.Mes,-8} {row.Total.ToString("C", culture),14} {deltaStr,15}");
            anterior = row.Total;
        }
    }

    // ========== UTILIDADES ==========

    private SQLiteConnection GetConnection()
    {
        return new SQLiteConnection(_connectionString);
    }

    public void TestarConexao()
    {
        Console.WriteLine("=== TESTE DE CONEXÃO ===");
        using var conn = GetConnection();
        try
        {
            conn.Open();

            // Ver informações
            using (var cmd = new SQLiteCommand("SELECT sqlite_version();", conn))
            {
                var version = cmd.ExecuteScalar()?.ToString();
                Console.WriteLine($"SQLite: {version}");
            }

            using (var cmd = new SQLiteCommand("PRAGMA foreign_keys;", conn))
            {
                var fk = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
                Console.WriteLine($"Foreign Keys: {(fk == 1 ? "ON" : "OFF")}");
            }

            using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM sqlite_master WHERE type='table';", conn))
            {
                var tables = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
                Console.WriteLine($"Tabelas no banco: {tables}");
            }

            // Listar nomes de tabelas
            using (var cmd = new SQLiteCommand("SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;", conn))
            using (var rd = cmd.ExecuteReader())
            {
                Console.WriteLine("Tabelas:");
                while (rd.Read()) Console.WriteLine(" - " + rd.GetString(0));
            }

            Console.WriteLine("Conexão OK e operações básicas funcionando.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Falha ao conectar: {ex.Message}");
        }
    }

    // ===== Helpers =====

    private static string Trunc(string? s, int max)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return s.Length <= max ? s : s[..(max - 1)] + "…";
    }

    private (int Id, string Nome)? BuscarClientePorEmail(SQLiteConnection conn, SQLiteTransaction tx, string email)
    {
        using var cmd = new SQLiteCommand("SELECT Id, Nome FROM Clientes WHERE Email = @e LIMIT 1;", conn, tx);
        cmd.Parameters.AddWithValue("@e", email);
        using var rd = cmd.ExecuteReader();
        if (rd.Read())
            return (Convert.ToInt32(rd["Id"]), Convert.ToString(rd["Nome"])!);
        return null;
    }

    private (int Id, string Nome)? BuscarClientePorId(SQLiteConnection conn, SQLiteTransaction tx, int id)
    {
        using var cmd = new SQLiteCommand("SELECT Id, Nome FROM Clientes WHERE Id = @i LIMIT 1;", conn, tx);
        cmd.Parameters.AddWithValue("@i", id);
        using var rd = cmd.ExecuteReader();
        if (rd.Read())
            return (Convert.ToInt32(rd["Id"]), Convert.ToString(rd["Nome"])!);
        return null;
    }

    private (int Id, string Nome, decimal Preco, int Estoque)? BuscarProdutoPorId(SQLiteConnection conn, SQLiteTransaction tx, int id)
    {
        using var cmd = new SQLiteCommand("SELECT Id, Nome, Preco, Estoque FROM Produtos WHERE Id = @i LIMIT 1;", conn, tx);
        cmd.Parameters.AddWithValue("@i", id);
        using var rd = cmd.ExecuteReader();
        if (rd.Read())
            return (Convert.ToInt32(rd["Id"]), Convert.ToString(rd["Nome"])!, Convert.ToDecimal(rd["Preco"]), Convert.ToInt32(rd["Estoque"]));
        return null;
    }

    public void Dispose()
    {

    }
}
