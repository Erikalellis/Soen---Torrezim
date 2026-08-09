# Banco de Dados

## Motor escolhido: SQLite (arquivo local, sem servidor)

- Fácil: um único arquivo `.db` na pasta do app (`soen.db`).
- Zero configuração de servidor — perfeito para uso em uma oficina (1 PC).
- Criado automaticamente no primeiro uso pelo `Database.cs` (`Database.Inicializar()`).
- O banco responde às tabelas/camadas **atuais** (versão 1.1.x). Migrações leves
  (`ALTER TABLE ...`) são aplicadas automaticamente ao abrir (`Database.Migrar`).

> Futuro: quando o sistema crescer, pode migrar para SQL Server/PostgreSQL.
> O padrão DAO que usaremos facilita essa mudança.

---

## Diagrama de tabelas (estado atual)

```
clientes ──1:N── veiculos
   │   │            │
   │   │            └──1:N── manutencoes
   │   │
   │   └──1:N── agendamentos ──N:1── servicos
   │
   ├──1:N── vendas ──1:N── vendas_itens
   │
   ├──1:N── orcamentos ──1:N── orcamento_itens ──N:1── produtos
   │                      (baixa de peça via produto_id)
   │            O orcamento referencia um tecnico (comissao %)
   │
   └──1:N── financeiro  (tipo 'pagar' | 'receber')

produtos ──1:N── movimentacao_estoque
tecnicos ──1:N── comissao_pagamentos
usuarios              (perfil admin/operador)
empresa               (dados da empresa + personalização de UI)
config                (chave/valor — preferências, backup, impressora)
```

---

## DDL — SQLite (conforme `Database.cs`)

```sql
-- ============ CADASTROS ============

CREATE TABLE IF NOT EXISTS clientes (
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

CREATE TABLE IF NOT EXISTS veiculos (
    id            INTEGER PRIMARY KEY AUTOINCREMENT,
    cliente_id    INTEGER REFERENCES clientes(id),
    placa         TEXT,
    marca         TEXT,
    modelo        TEXT,
    cor           TEXT,
    observacoes   TEXT,
    quilometragem REAL DEFAULT 0,
    proxima_revisao TEXT,
    garantia_fim  TEXT,               -- (migração)
    ipva_venc     TEXT,               -- (migração)
    licenciamento_venc TEXT,          -- (migração)
    criado_em     TEXT DEFAULT (datetime('now','localtime'))
);

CREATE TABLE IF NOT EXISTS manutencoes (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    veiculo_id  INTEGER REFERENCES veiculos(id),
    cliente_id  INTEGER REFERENCES clientes(id),
    data        TEXT,
    descricao   TEXT,
    valor       REAL,
    status      TEXT DEFAULT 'aberta'   -- aberta | concluida | cancelada
);

-- ============ VENDAS E CAIXA ============

CREATE TABLE IF NOT EXISTS vendas (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    data            TEXT DEFAULT (datetime('now','localtime')),
    cliente_id      INTEGER REFERENCES clientes(id),
    veiculo_id      INTEGER REFERENCES veiculos(id),
    valor_total     REAL NOT NULL DEFAULT 0,
    forma_pagamento TEXT,
    observacoes     TEXT
);

CREATE TABLE IF NOT EXISTS vendas_itens (
    id         INTEGER PRIMARY KEY AUTOINCREMENT,
    venda_id   INTEGER NOT NULL REFERENCES vendas(id),
    descricao  TEXT,
    quantidade REAL NOT NULL DEFAULT 1,
    valor_unit REAL NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS caixa (
    id        INTEGER PRIMARY KEY AUTOINCREMENT,
    data      TEXT DEFAULT (datetime('now','localtime')),
    tipo      TEXT NOT NULL,          -- 'entrada' | 'saida'
    descricao TEXT,
    valor     REAL NOT NULL DEFAULT 0
);

-- ============ ESTOQUE / SERVIÇOS ============

CREATE TABLE IF NOT EXISTS produtos (
    id        INTEGER PRIMARY KEY AUTOINCREMENT,
    codigo    TEXT,
    nome      TEXT NOT NULL,
    categoria TEXT,
    unidade   TEXT DEFAULT 'un',
    qtd_atual REAL DEFAULT 0,
    custo     REAL DEFAULT 0,
    preco     REAL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS movimentacao_estoque (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    produto_id  INTEGER NOT NULL REFERENCES produtos(id),
    tipo        TEXT NOT NULL,        -- 'entrada' | 'saida'
    quantidade  REAL NOT NULL,
    data        TEXT DEFAULT (datetime('now','localtime')),
    documento   TEXT
);

CREATE TABLE IF NOT EXISTS servicos (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    nome        TEXT NOT NULL,
    descricao   TEXT,
    preco       REAL DEFAULT 0
);

-- ============ AGENDAS ============

CREATE TABLE IF NOT EXISTS agendamentos (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    cliente_id  INTEGER REFERENCES clientes(id),
    veiculo_id  INTEGER REFERENCES veiculos(id),
    servico_id  INTEGER REFERENCES servicos(id),
    data_hora   TEXT,
    status      TEXT DEFAULT 'agendado', -- agendado | confirmado | concluido | cancelado
    observacoes TEXT
);

-- ============ ORÇAMENTOS / OS ============

CREATE TABLE IF NOT EXISTS orcamentos (
    id           INTEGER PRIMARY KEY AUTOINCREMENT,
    data         TEXT DEFAULT (datetime('now','localtime')),
    cliente_id   INTEGER REFERENCES clientes(id),
    veiculo_id   INTEGER REFERENCES veiculos(id),
    servico      TEXT,
    valor        REAL DEFAULT 0,
    status       TEXT DEFAULT 'em_aberto', -- em_aberto | aprovado | recusado | convertido
    tipo         TEXT DEFAULT 'orcamento', -- orcamento | nota (Nota de Serviço / OS)  (migração)
    numero       TEXT,                     -- numeração da OS/nota                       (migração)
    tecnico_id   INTEGER REFERENCES tecnicos(id),  -- técnico responsável              (migração)
    comissao     REAL DEFAULT 0,                -- valor da comissão                  (migração)
    concluido_em TEXT                           -- data de conclusão                   (migração)
);

CREATE TABLE IF NOT EXISTS orcamento_itens (
    id           INTEGER PRIMARY KEY AUTOINCREMENT,
    orcamento_id INTEGER NOT NULL REFERENCES orcamentos(id),
    descricao    TEXT,
    quantidade   REAL DEFAULT 1,
    valor_unit   REAL DEFAULT 0,
    produto_id   INTEGER REFERENCES produtos(id)  -- p/ baixa de estoque              (migração)
);

-- ============ FINANCEIRO ============

CREATE TABLE IF NOT EXISTS financeiro (
    id             INTEGER PRIMARY KEY AUTOINCREMENT,
    tipo           TEXT NOT NULL,            -- 'pagar' | 'receber'
    descricao      TEXT,
    fornecedor     TEXT,
    vencimento     TEXT,
    valor          REAL DEFAULT 0,
    status         TEXT DEFAULT 'em_aberto', -- em_aberto | pago | cancelado
    data_pagamento TEXT,                     -- quando foi pago/recebido           (migração)
    criado_em      TEXT DEFAULT (datetime('now','localtime'))
);

-- ============ SEGURANÇA ============

CREATE TABLE IF NOT EXISTS usuarios (
    id       INTEGER PRIMARY KEY AUTOINCREMENT,
    usuario  TEXT NOT NULL UNIQUE,
    senha    TEXT NOT NULL,                -- hash PBKDF2 (formato PBKDF2$iter$salt$hash)
    nome     TEXT,
    perfil   TEXT DEFAULT 'operador',      -- admin | operador
    ativo    INTEGER DEFAULT 1
);

-- ============ EMPRESA / PERSONALIZAÇÃO ============

CREATE TABLE IF NOT EXISTS empresa (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    nome            TEXT,
    cnpj            TEXT,
    telefone        TEXT,
    endereco        TEXT,
    cidade          TEXT,
    estado          TEXT,
    email           TEXT,
    site            TEXT,
    observacoes     TEXT,
    background_image TEXT,
    background_mode TEXT DEFAULT 'stretch',  -- stretch | center | tile
    logo_path       TEXT,
    logo_width      INTEGER DEFAULT 0,
    logo_height     INTEGER DEFAULT 0
);

-- ============ TÉCNICOS E COMISSÕES ============

CREATE TABLE IF NOT EXISTS tecnicos (
    id                INTEGER PRIMARY KEY AUTOINCREMENT,
    nome              TEXT NOT NULL,
    telefone          TEXT,
    cargo             TEXT,
    comissao_percent  REAL DEFAULT 0,
    ativo             INTEGER DEFAULT 1
);

CREATE TABLE IF NOT EXISTS comissao_pagamentos (
    id         INTEGER PRIMARY KEY AUTOINCREMENT,
    tecnico_id INTEGER NOT NULL REFERENCES tecnicos(id),
    valor      REAL DEFAULT 0,
    data       TEXT DEFAULT (datetime('now','localtime')),
    observacao TEXT
);
```

> Tabela adicional: **`config`** (criada pelo `ConfigDAO`) — armazenamento chave/valor usado para
> preferências do sistema (impressora padrão, parâmetros do backup automático, etc.).

---

## Migrações automáticas (adicionam colunas em bancos antigos)

`Database.Migrar()` faz `ALTER TABLE ... ADD COLUMN` quando a coluna não existe:

- `orcamentos`: `tipo`, `numero`, `tecnico_id`, `comissao`, `concluido_em`
- `veiculos`: `quilometragem`, `proxima_revisao`, `garantia_fim`, `ipva_venc`, `licenciamento_venc`
- `orcamento_itens`: `produto_id`
- `financeiro`: `data_pagamento`

---

## Senhas (importante)

- As senhas são guardadas com **PBKDF2** (`Rfc2898DeriveBytes`, 10.000 iterações, salt 16 bytes, hash 32 bytes).
- Formato armazenado: `PBKDF2$iter$salt$hash` (Base64).
- `UsuarioDAO` faz **upgrade automático** de hashes antigos (SHA-256 hex) ao autenticar.
- Usuário padrão no primeiro uso: **admin / admin** (perfil administrador).
- > O comentário `-- hash SHA-256` na coluna `senha` (em `Database.cs`) está desatualizado — o código real é PBKDF2.

---

## Convenções
- Chave primária: `id INTEGER PRIMARY KEY AUTOINCREMENT`.
- Datas no formato ISO `yyyy-mm-dd` (ordenação alfabética = cronológica).
- Moedas: `REAL` (guardar só números; formatar no visual).
- Datas/hora: `datetime('now','localtime')` usa fuso local da máquina.
- Relacionamentos usam `INTEGER REFERENCES tabela(id)` com `ON DELETE` implícito (default).

## Backup do banco
- Backup agendado copia o `soen.db` para `CaminhoBanco\Backups` (padrão), com retenção de N cópias (default 10).
- Arquivos: `soen_yyyyMMdd_HHmmss.db`.