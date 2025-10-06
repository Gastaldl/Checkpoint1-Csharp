# **Sistema de Gestão de Loja Online — Entity Framework Core + ADO.NET (SQLite)**

> Console app que demonstra CRUDs e consultas avançadas com **EF Core** e com **ADO.NET puro** sobre o mesmo banco `SQLite`.

---

## Integrantes

* Márcio Gastaldi - RM98811
* Davi Desenzi - RM550849

---

## 🔧 Tecnologias & Pacotes

* .NET 7/8 (recomendado .NET 8)
* **Entity Framework Core** (Microsoft.EntityFrameworkCore, Microsoft.EntityFrameworkCore.Sqlite, Tools)
* **ADO.NET** com **System.Data.SQLite**
* SQLite (arquivo local `loja.db`)

Instale (se necessário) com o CLI do .NET, na pasta do projeto:

```bash
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package System.Data.SQLite.Core
dotnet tool install --global dotnet-ef
```

---

## 📁 Estrutura (resumo)

```
CheckPoint1/
├─ Program.cs
├─ CheckpointContext.cs
├─ Models/
│  ├─ Categoria.cs
│  ├─ Produto.cs
│  ├─ Cliente.cs
│  ├─ Pedido.cs
│  ├─ PedidoItem.cs
│  └─ StatusPedido.cs
├─ Services/
│  ├─ EntityFrameworkService.cs
│  └─ AdoNetService.cs
└─ loja.db  (gerado em runtime dentro de bin/…)
```

> Observação: há uma imagem de referência da estrutura no repositório do projeto (quando aplicável).

---

## 🚀 Como rodar

1. **Restaurar & Compilar**

```bash
dotnet restore
dotnet build
```

2. **Executar**

```bash
dotnet run
```

3. **Inicialização do banco**

* O `Program.cs` chama `InicializarBanco()` no start:

  * Se houver **migrations**, aplica `MigrateAsync()`.
  * Caso contrário, cria o schema com `EnsureCreatedAsync()`.
* O arquivo `loja.db` é resolvido via **caminho absoluto**: `Path.Combine(AppContext.BaseDirectory, "loja.db")`, garantindo que EF e ADO.NET usem **o mesmo arquivo**.

### (Opcional) Criar Migrations para aplicar o Seed do `OnModelCreating`

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

> Com migrations, os dados de **seed** (`HasData`) são inseridos automaticamente (3 categorias, 6 produtos, 2 clientes, 2 pedidos, 4 itens).

---

## 🧭 Menus & Funcionalidades

### Menu Principal

* **1 - Entity Framework**
* **2 - ADO.NET Direto**
* **0 - Sair**

### Entity Framework

CADASTROS

* **Categorias**: criar/listar (com contagem de produtos)
* **Produtos**: criar/listar (Include categoria)/atualizar
* **Clientes**: criar (valida e-mail único e saneia CPF)/listar (contagem de pedidos)/atualizar
* **Pedidos**: criar (gera número, valida estoque, baixa estoque, calcula total)/listar (Include Cliente/Itens/Produto)/atualizar status (valida transições)/cancelar (devolve estoque)

CONSULTAS LINQ

* Produtos mais vendidos
* Clientes com mais pedidos
* Faturamento por categoria (ticket médio)
* Pedidos por período
* Produtos com estoque baixo
* Análise de vendas mensal (Δ% vs mês anterior)
* Top 10 clientes por valor

RELATÓRIOS

* Dashboard executivo (KPIs, faturamento últimos 6 meses)
* Relatório de estoque (valor por categoria, zerados, baixo estoque)
* Análise de clientes (por UF, valor médio por cliente)

### ADO.NET Direto

CONSULTAS COMPLEXAS

* **Relatório Vendas Completo** (JOIN Pedidos/Clientes/PedidoItens/Produtos)
* **Faturamento por Cliente** (GROUP BY, SUM, ticket médio)
* **Produtos sem Vendas** (LEFT JOIN + `IS NULL`)

OPERAÇÕES

* **Atualizar Estoque em Lote** (por categoria, produto a produto)
* **Inserir Pedido Completo** (transação: pedido + itens + baixa de estoque)
* **Excluir Dados Antigos** (DELETE cancelados > 6 meses)

PROCESSOS

* **Processar Devolução** (devolve estoque e cancela pedido)

ANÁLISES

* **Análise Performance** (vendas mensais e Δ%)

UTILITÁRIOS

* **Testar Conexão** (versão SQLite, PRAGMA FK, contagem e lista de tabelas)

---

## 🗄️ Modelo de Dados (resumo)

* **Tabelas (pluralizadas)**: `Categorias`, `Produtos`, `Clientes`, `Pedidos`, `PedidoItens`
* **Relacionamentos & DeleteBehavior**

  * Categoria (1)–(N) Produto → **Cascade**
  * Cliente (1)–(N) Pedido → **Cascade**
  * Pedido (1)–(N) PedidoItem → **Cascade**
  * Produto (1)–(N) PedidoItem → **Restrict** (não excluir produto vendido)
* **Índices únicos**

  * `Clientes.Email`
  * `Pedidos.NumeroPedido`
* **Observação sobre `decimal` no SQLite**
  O SQLite não aplica `precision/scale` rigidamente. Para cenários financeiros reais, considere armazenar valores em **centavos (inteiros)**.

---

## 🔒 Boas práticas aplicadas

* **Parametrização** de todas as queries ADO.NET (evita SQL Injection)
* **Transações** em operações críticas (pedido completo, devolução, exclusão em lote)
* **Validações** de domínio (estoque, e-mail único, transições de status, CPF saneado)
* **Caminho absoluto** do DB para evitar “bancos diferentes” em EF vs ADO.NET
* **PRAGMA foreign_keys=ON** via connection string (`Foreign Keys=True`)

---

## 🧪 Troubleshooting

* **“no such table: …”**

  * Confirme se o app listou 5+ tabelas no “Testar Conexão”.
  * Garanta que EF e ADO.NET apontam para **o mesmo arquivo** (caminho absoluto já configurado).
  * Se começou sem migrations, rode `dotnet ef migrations add InitialCreate && dotnet ef database update`.

* **Seed não apareceu**

  * `HasData` roda via **migrations**. Sem migrations, use `EnsureCreated` (cria schema) e insira dados manualmente ou gere as migrations.

* **`DllNotFoundException`/`BadImageFormatException` no System.Data.SQLite**

  * Certifique-se de usar `System.Data.SQLite.Core` compatível com sua arquitetura (x64/x86).
  * Em Linux/macOS, prefira rodar com .NET 7/8 e pacote Core recente.

---

## 🧱 Decisões de Projeto

* **Tabelas no plural** (compatibilidade natural com `DbSet<T>` do EF).
* **Delete Restrict** entre Produto e Itens de Pedido para preservar histórico de vendas.
* **ADO.NET** e **EF** trabalhando no **mesmo** banco, facilitando comparação entre abordagens.

---

## 🗺️ Próximos Passos / Extensões

* Implementar **desconto por item** e **cupons** no fluxo de pedido (EF e ADO.NET).
* Exportar relatórios em **CSV/JSON**.
* Paginar listagens no console.
* Adicionar **validação de CPF** (algoritmo) além do saneamento.

---

## 📝 Licença

Projeto acadêmico. Ajuste a licença conforme sua necessidade.

---

Qualquer coisa, me diga que eu adapto o README ao formato que você precisa (ex.: com GIFs, screenshots, badges ou seções extras de arquitetura).
