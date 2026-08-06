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
    cliente_id  INTEGER NOT NULL REFERENCES clientes(id),
    placa       TEXT,
    marca       TEXT,
    modelo      TEXT,
    cor         TEXT,
    observacoes TEXT,
    criado_em   TEXT DEFAULT (datetime('now','localtime'))
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
";
            }
        }
    }
}