using System;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;

namespace Soen___Torrezim.Data
{
    /// <summary>
    /// Responsável por criar o arquivo de banco (SQLite) e as tabelas
    /// no primeiro uso. Devolve conexões já abertas.
    /// </summary>
    public static class Database
    {
        /// <summary>Nome do arquivo do banco (fica na pasta do executável).</summary>
        public const string NomeArquivo = "soen.db";

        private static string _connectionString;

        /// <summary>Caminho completo do arquivo de banco.</summary>
        public static string CaminhoBanco
        {
            get { return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, NomeArquivo); }
        }

        /// <summary>Inicializa o banco: cria arquivo + tabelas se não existirem.</summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL injection for security vulnerabilities",
            Justification = "Script de criação de tabelas é texto fixo do próprio código, sem entrada do usuário.")]
        public static void Inicializar()
        {
            var sb = new SQLiteConnectionStringBuilder
            {
                DataSource = CaminhoBanco
            };
            _connectionString = sb.ConnectionString;

            using (var conn = AbrirConexao())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = ScriptTabelas;
                    cmd.ExecuteNonQuery();
                }
                Migrar(conn);
            }
        }

        /// <summary>Migrações leves para bancos criados em versões anteriores.</summary>
        private static void Migrar(SQLiteConnection conn)
        {
            AdicionarColunaSeFaltar(conn, "orcamentos", "tipo", "TEXT DEFAULT 'orcamento'");
            AdicionarColunaSeFaltar(conn, "orcamentos", "numero", "TEXT");
            AdicionarColunaSeFaltar(conn, "orcamentos", "tecnico_id", "INTEGER");
            AdicionarColunaSeFaltar(conn, "orcamentos", "comissao", "REAL DEFAULT 0");
            AdicionarColunaSeFaltar(conn, "orcamentos", "concluido_em", "TEXT");
            AdicionarColunaSeFaltar(conn, "veiculos", "quilometragem", "REAL DEFAULT 0");
            AdicionarColunaSeFaltar(conn, "veiculos", "proxima_revisao", "TEXT");
            AdicionarColunaSeFaltar(conn, "orcamento_itens", "produto_id", "INTEGER");
        }

        private static void AdicionarColunaSeFaltar(SQLiteConnection conn, string tabela, string coluna, string definicao)
        {
            bool existe = false;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "PRAGMA table_info(" + tabela + ")";
                using (var leitor = cmd.ExecuteReader())
                {
                    while (leitor.Read())
                        if (string.Equals(leitor.GetString(1), coluna, StringComparison.OrdinalIgnoreCase)) { existe = true; break; }
                }
            }
            if (existe) return;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "ALTER TABLE " + tabela + " ADD COLUMN " + coluna + " " + definicao;
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>Abre uma conexão nova e já conectada.</summary>
        public static SQLiteConnection AbrirConexao()
        {
            if (_connectionString == null)
            {
                Inicializar();
            }
            var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            return conn;
        }

        /// <summary>Limpa um valor para gravação (null vira null; texto vazio vira null).</summary>
        public static object Nulo(string valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? (object)DBNull.Value : valor.Trim();
        }

        /// <summary>Script de criação das tabelas (Fase 1).</summary>
        private static string ScriptTabelas
        {
            get
            {
                return @"
CREATE TABLE IF NOT EXISTS clientes (
    id            INTEGER PRIMARY KEY AUTOINCREMENT,
    tipo          TEXT    NOT NULL DEFAULT 'cliente',
    cpf_cnpj      TEXT,
    nome_razao    TEXT    NOT NULL,
    sexo          TEXT,
    nascimento    TEXT,
    cep           TEXT,
    endereco      TEXT,
    complemento   TEXT,
    bairro        TEXT,
    cidade        TEXT,
    estado        TEXT,
    fone1         TEXT,
    fone2         TEXT,
    email1        TEXT,
    email2        TEXT,
    responsavel   TEXT,
    funcao        TEXT,
    criado_em     TEXT    DEFAULT (datetime('now','localtime'))
);

CREATE TABLE IF NOT EXISTS veiculos (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    cliente_id  INTEGER REFERENCES clientes(id),
    placa       TEXT,
    marca       TEXT,
    modelo      TEXT,
    cor         TEXT,
    observacoes TEXT,
    quilometragem REAL DEFAULT 0,
    proxima_revisao TEXT
);

CREATE TABLE IF NOT EXISTS manutencoes (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    veiculo_id  INTEGER REFERENCES veiculos(id),
    cliente_id  INTEGER REFERENCES clientes(id),
    data        TEXT,
    descricao   TEXT,
    valor       REAL,
    status      TEXT DEFAULT 'aberta'
);

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

CREATE TABLE IF NOT EXISTS agendamentos (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    cliente_id  INTEGER REFERENCES clientes(id),
    veiculo_id  INTEGER REFERENCES veiculos(id),
    servico_id  INTEGER REFERENCES servicos(id),
    data_hora   TEXT,
    status      TEXT DEFAULT 'agendado', -- agendado | confirmado | concluido | cancelado
    observacoes TEXT
);

CREATE TABLE IF NOT EXISTS orcamentos (
    id         INTEGER PRIMARY KEY AUTOINCREMENT,
    data       TEXT DEFAULT (datetime('now','localtime')),
    cliente_id INTEGER REFERENCES clientes(id),
    veiculo_id INTEGER REFERENCES veiculos(id),
    servico    TEXT,
    valor      REAL DEFAULT 0,
    status     TEXT DEFAULT 'em_aberto', -- em_aberto | aprovado | recusado | convertido
    tipo       TEXT DEFAULT 'orcamento', -- orcamento (sem compromisso) | nota (Nota de Serviço / OS)
    numero     TEXT,
    tecnico_id INTEGER REFERENCES tecnicos(id),
    comissao   REAL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS orcamento_itens (
    id           INTEGER PRIMARY KEY AUTOINCREMENT,
    orcamento_id INTEGER NOT NULL REFERENCES orcamentos(id),
    descricao    TEXT,
    quantidade   REAL DEFAULT 1,
    valor_unit   REAL DEFAULT 0,
    produto_id   INTEGER REFERENCES produtos(id)
);

CREATE TABLE IF NOT EXISTS financeiro (
    id           INTEGER PRIMARY KEY AUTOINCREMENT,
    tipo         TEXT NOT NULL,            -- 'pagar' | 'receber'
    descricao    TEXT,
    fornecedor   TEXT,
    vencimento   TEXT,
    valor        REAL DEFAULT 0,
    status       TEXT DEFAULT 'em_aberto', -- em_aberto | pago | cancelado
    criado_em    TEXT DEFAULT (datetime('now','localtime'))
);

CREATE TABLE IF NOT EXISTS usuarios (
    id       INTEGER PRIMARY KEY AUTOINCREMENT,
    usuario  TEXT NOT NULL UNIQUE,
    senha    TEXT NOT NULL,                -- hash SHA-256
    nome     TEXT,
    perfil   TEXT DEFAULT 'operador',      -- admin | operador
    ativo    INTEGER DEFAULT 1
);

CREATE TABLE IF NOT EXISTS empresa (
    id        INTEGER PRIMARY KEY AUTOINCREMENT,
    nome      TEXT,
    cnpj      TEXT,
    telefone  TEXT,
    endereco  TEXT,
    cidade    TEXT,
    estado    TEXT,
    email     TEXT,
    site      TEXT,
    observacoes TEXT,
    background_image TEXT,
    background_mode  TEXT DEFAULT 'stretch',
    logo_path  TEXT,
    logo_width INTEGER DEFAULT 0,
    logo_height INTEGER DEFAULT 0
);

CREATE TABLE IF NOT EXISTS tecnicos (
    id         INTEGER PRIMARY KEY AUTOINCREMENT,
    nome       TEXT NOT NULL,
    telefone   TEXT,
    cargo      TEXT,
    comissao_percent REAL DEFAULT 0,
    ativo      INTEGER DEFAULT 1
);
";
            }
        }
    }
}