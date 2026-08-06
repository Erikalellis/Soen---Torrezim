# Banco de Dados

## Motor escolhido: SQLite (arquivo local, sem servidor)

- Fácil: um único arquivo `.db` na pasta do app (`soen.db`).
- Zero configuração de servidor — perfeito para uso em uma oficina (1 PC).
- Criado automaticamente no primeiro uso pelo `Database.cs`.

> Futuro: quando o sistema crescer, pode migrar para SQL Server/PostgreSQL.
> O padrão DAO que usaremos facilita essa mudança.

---

## Diagrama de tabelas (Fase 1 — núcleo)

```
clientes ──1:N── veiculos
   │                 │
   │                 └──1:N── manutencoes
   │
   └──1:N── vendas ──1:N── vendas_itens ──N:1── produtos (estoque)

produtos ──1:N── movimentacao_estoque
```

---

## DDL — SQLite

```sql
-- ============ CADASTROS ============

CREATE TABLE clientes (
    id            INTEGER PRIMARY KEY AUTOINCREMENT,
    tipo          TEXT    NOT NULL DEFAULT 'cliente',  -- 'cliente' | 'fornecedor'
    cpf_cnpj      TEXT,
    nome_razao    TEXT    NOT NULL,
    sexo          TEXT,
    nascimento    TEXT,               -- ISO 8601 (yyyy-MM-dd)
    cep           TEXT,
    endereco      TEXT,
    complemento   TEXT,
    bairro        TEXT,
    cidade        TEXT,
    estado        TEXT,               -- UF
    fone1         TEXT,
    fone2         TEXT,
    email1        TEXT,
    email2        TEXT,
    responsavel   TEXT,
    funcao        TEXT,
    criado_em     TEXT    DEFAULT (datetime('now','localtime'))
);

CREATE TABLE veiculos (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    cliente_id  INTEGER NOT NULL REFERENCES clientes(id),
    placa       TEXT,
    marca       TEXT,
    modelo      TEXT,
    cor         TEXT,
    observacoes TEXT,
    criado_em   TEXT DEFAULT (datetime('now','localtime'))
);

CREATE TABLE manutencoes (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    veiculo_id  INTEGER NOT NULL REFERENCES veiculos(id),
    cliente_id  INTEGER REFERENCES clientes(id),
    data        TEXT,
    descricao   TEXT,
    valor       REAL,
    status      TEXT DEFAULT 'aberta'
);

-- ============ ESTOQUE / SERVIÇOS ============

CREATE TABLE produtos (
    id        INTEGER PRIMARY KEY AUTOINCREMENT,
    codigo    TEXT,
    nome      TEXT NOT NULL,
    categoria TEXT,
    unidade   TEXT,
    qtd_atual REAL DEFAULT 0,
    custo     REAL,
    preco     REAL
);

CREATE TABLE movimentacao_estoque (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    produto_id  INTEGER NOT NULL REFERENCES produtos(id),
    tipo        TEXT NOT NULL,        -- 'entrada' | 'saida'
    quantidade  REAL NOT NULL,
    data        TEXT DEFAULT (datetime('now','localtime')),
    documento   TEXT
);

-- ============ AGENDAS E SERVIÇOS ============

CREATE TABLE servicos (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    nome        TEXT NOT NULL,
    descricao   TEXT,
    preco       REAL
);

CREATE TABLE agendamentos (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    cliente_id  INTEGER REFERENCES clientes(id),
    veiculo_id  INTEGER REFERENCES veiculos(id),
    servico_id  INTEGER REFERENCES servicos(id),
    data_hora   TEXT,
    status      TEXT DEFAULT 'agendado', -- 'agendado'|'confirmado'|'concluido'|'cancelado'
    observacoes TEXT
);

-- ============ VENDAS E CAIXA ============

CREATE TABLE vendas (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    data        TEXT DEFAULT (datetime('now','localtime')),
    cliente_id  INTEGER REFERENCES clientes(id),
    valor_total REAL NOT NULL,
    forma_pagamento TEXT,
    status      TEXT DEFAULT 'finalizada'
);

CREATE TABLE vendas_itens (
    id         INTEGER PRIMARY KEY AUTOINCREMENT,
    venda_id   INTEGER NOT NULL REFERENCES vendas(id),
    produto_id INTEGER REFERENCES produtos(id),
    descricao  TEXT,
    quantidade REAL NOT NULL DEFAULT 1,
    valor_unit REAL NOT NULL
);

CREATE TABLE caixa (
    id        INTEGER PRIMARY KEY AUTOINCREMENT,
    data      TEXT DEFAULT (datetime('now','localtime')),
    tipo      TEXT NOT NULL,        -- 'entrada' | 'saida'
    descricao TEXT,
    valor     REAL NOT NULL
);

-- ============ FINANCEIRO ============

CREATE TABLE contas_pagar (
    id           INTEGER PRIMARY KEY AUTOINCREMENT,
    fornecedor_id INTEGER REFERENCES clientes(id),
    descricao    TEXT,
    valor        REAL NOT NULL,
    vencimento   TEXT,
    pago         INTEGER DEFAULT 0
);

CREATE TABLE contas_receber (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    cliente_id  INTEGER REFERENCES clientes(id),
    descricao   TEXT,
    valor       REAL NOT NULL,
    vencimento  TEXT,
    recebido    INTEGER DEFAULT 0
);

-- ============ ORÇAMENTOS ============

CREATE TABLE orcamentos (
    id         INTEGER PRIMARY KEY AUTOINCREMENT,
    data       TEXT DEFAULT (datetime('now','localtime')),
    cliente_id INTEGER REFERENCES clientes(id),
    veiculo_id INTEGER REFERENCES veiculos(id),
    servico    TEXT,
    valor      REAL,
    status     TEXT DEFAULT 'em_aberto' -- 'em_aberto'|'aprovado'|'recusado'|'convertido'
);

-- ============ NOTIFICAÇÕES E LEMBRETES ============

CREATE TABLE lembretes (
    id        INTEGER PRIMARY KEY AUTOINCREMENT,
    titulo    TEXT NOT NULL,
    mensagem  TEXT,
    data_hora TEXT,
    tipo      TEXT,       -- 'tarefa' | 'vencimento' | 'personalizado'
    concluido INTEGER DEFAULT 0
);

-- ============ SEGURANÇA (Fase 10) ============

CREATE TABLE usuarios (
    id       INTEGER PRIMARY KEY AUTOINCREMENT,
    usuario  TEXT UNIQUE NOT NULL,
    senha    TEXT NOT NULL,          -- hash SHA256
    nome     TEXT
);
```

> Nota: tabelas de fases futuras (contas, orçamentos, agendamentos…) já estão
> descritas aqui para referência, mas serão criadas conforme a fase avança para
> não complicar o início.

---

## Convenções
- Chave primária: `id INTEGER PRIMARY KEY AUTOINCREMENT`.
- Datas no formato ISO `yyyy-mm-dd` (ordenação alfabética = cronológica).
- Moedas: `REAL` (guardar só números; formatar no visual).
- Datas/hora: `datetime('now','localtime')` usa fuso local da máquina.