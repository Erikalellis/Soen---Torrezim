using System.Collections.Generic;
using System.Data.SQLite;
using Soen___Torrezim.Models;

namespace Soen___Torrezim.Data
{
    /// <summary>
    /// Acesso a dados de Vendas. Uma venda é gravada junto com seus itens
    /// (na mesma transação) e já gera a entrada no caixa automaticamente.
    /// </summary>
    public static class VendaDAO
    {
        /// <summary>
        /// Salva a venda com seus itens (transação) e lança a entrada no caixa.
        /// Retorna o Id da venda.
        /// </summary>
        public static long SalvarComCaixa(Venda v)
        {
            using (var conn = Database.AbrirConexao())
            using (var tx = conn.BeginTransaction())
            {
                long idVenda;

                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    if (v.Id > 0)
                    {
                        cmd.CommandText = @"UPDATE vendas SET
    data=@data, cliente_id=@cliente, veiculo_id=@veiculo, valor_total=@total,
    forma_pagamento=@forma, observacoes=@obs WHERE id=@id";
                        cmd.Parameters.AddWithValue("@id", v.Id);
                    }
                    else
                    {
                        cmd.CommandText = @"INSERT INTO vendas (data, cliente_id, veiculo_id, valor_total, forma_pagamento, observacoes)
VALUES (@data,@cliente,@veiculo,@total,@forma,@obs)";
                    }
                    cmd.Parameters.AddWithValue("@data", Database.Nulo(v.Data));
                    cmd.Parameters.AddWithValue("@cliente", (object)v.ClienteId ?? System.DBNull.Value);
                    cmd.Parameters.AddWithValue("@veiculo", (object)v.VeiculoId ?? System.DBNull.Value);
                    cmd.Parameters.AddWithValue("@total", v.ValorTotal);
                    cmd.Parameters.AddWithValue("@forma", Database.Nulo(v.FormaPagamento));
                    cmd.Parameters.AddWithValue("@obs", Database.Nulo(v.Observacoes));
                    cmd.ExecuteNonQuery();

                    if (v.Id > 0) idVenda = v.Id;
                    else idVenda = conn.LastInsertRowId;
                }

                // Remove itens antigos e re-grava (simples para edição)
                using (var del = conn.CreateCommand())
                {
                    del.Transaction = tx;
                    del.CommandText = "DELETE FROM vendas_itens WHERE venda_id=@vid";
                    del.Parameters.AddWithValue("@vid", idVenda);
                    del.ExecuteNonQuery();
                }

                foreach (var item in v.Itens)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = @"INSERT INTO vendas_itens (venda_id, descricao, quantidade, valor_unit)
VALUES (@vid,@desc,@qtd,@unit)";
                        cmd.Parameters.AddWithValue("@vid", idVenda);
                        cmd.Parameters.AddWithValue("@desc", Database.Nulo(item.Descricao));
                        cmd.Parameters.AddWithValue("@qtd", item.Quantidade);
                        cmd.Parameters.AddWithValue("@unit", item.ValorUnit);
                        cmd.ExecuteNonQuery();
                    }
                }

                // Mantém o caixa coerente: venda nova insere a entrada; edição
                // remove o lançamento antigo desta venda e relança com o total atual.
                if (v.Id <= 0)
                {
                    if (v.ValorTotal > 0)
                        InserirLancamentoCaixa(conn, tx, idVenda, v.ValorTotal);
                }
                else
                {
                    RemoverLancamentoCaixa(conn, tx, idVenda);
                    if (v.ValorTotal > 0)
                        InserirLancamentoCaixa(conn, tx, idVenda, v.ValorTotal);
                }

                tx.Commit();
                return idVenda;
            }
        }

        private static void InserirLancamentoCaixa(SQLiteConnection conn, SQLiteTransaction tx, long idVenda, double valor)
        {
            using (var c = conn.CreateCommand())
            {
                c.Transaction = tx;
                c.CommandText = @"INSERT INTO caixa (tipo, descricao, valor)
VALUES ('entrada', @desc, @valor)";
                c.Parameters.AddWithValue("@desc", "Venda nº " + idVenda);
                c.Parameters.AddWithValue("@valor", valor);
                c.ExecuteNonQuery();
            }
        }

        private static void RemoverLancamentoCaixa(SQLiteConnection conn, SQLiteTransaction tx, long idVenda)
        {
            using (var c = conn.CreateCommand())
            {
                c.Transaction = tx;
                c.CommandText = "DELETE FROM caixa WHERE descricao=@desc";
                c.Parameters.AddWithValue("@desc", "Venda nº " + idVenda);
                c.ExecuteNonQuery();
            }
        }

        /// <summary>Lista vendas com cliente e veículo. Opcional filtrar por cliente.</summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL injection for security vulnerabilities",
            Justification = "O único valor dinâmico é o parâmetro @cliente (seguro). O SQL é texto estático.")]
        public static List<Venda> Listar(long? clienteId)
        {
            var lista = new List<Venda>();
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT v.*, c.nome_razao AS nome_cliente,
COALESCE(vh.placa, '') AS veiculo_desc
FROM vendas v
LEFT JOIN clientes c ON c.id=v.cliente_id
LEFT JOIN veiculos vh ON vh.id=v.veiculo_id WHERE 1=1";
                if (clienteId.HasValue)
                {
                    cmd.CommandText += " AND v.cliente_id=@cliente";
                    cmd.Parameters.AddWithValue("@cliente", clienteId.Value);
                }
                cmd.CommandText += " ORDER BY v.id DESC";

                using (var leitor = cmd.ExecuteReader())
                {
                    while (leitor.Read())
                    {
                        lista.Add(LerLinha(leitor));
                    }
                }
            }
            return lista;
        }

        /// <summary>Busca vendas por termo (cliente, nº, placa do veículo ou observação).</summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL injection for security vulnerabilities",
            Justification = "O único valor dinâmico é o parâmetro @f (seguro). O SQL é texto estático.")]
        public static List<Venda> ListarPesquisa(string termo)
        {
            var lista = new List<Venda>();
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT v.*, c.nome_razao AS nome_cliente,
COALESCE(vh.placa, '') AS veiculo_desc
FROM vendas v
LEFT JOIN clientes c ON c.id=v.cliente_id
LEFT JOIN veiculos vh ON vh.id=v.veiculo_id
WHERE c.nome_razao LIKE @f OR vh.placa LIKE @f OR v.observacoes LIKE @f OR CAST(v.id AS TEXT) LIKE @f
ORDER BY v.id DESC";
                cmd.Parameters.AddWithValue("@f", "%" + termo.Trim() + "%");
                using (var leitor = cmd.ExecuteReader())
                {
                    while (leitor.Read()) lista.Add(LerLinha(leitor));
                }
            }
            return lista;
        }

        public static Venda BuscarPorId(long id)
        {
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT v.*, c.nome_razao AS nome_cliente,
COALESCE(vh.placa, '') AS veiculo_desc
FROM vendas v
LEFT JOIN clientes c ON c.id=v.cliente_id
LEFT JOIN veiculos vh ON vh.id=v.veiculo_id WHERE v.id=@id";
                cmd.Parameters.AddWithValue("@id", id);
                Venda venda = null;
                using (var leitor = cmd.ExecuteReader())
                {
                    if (leitor.Read()) venda = LerLinha(leitor);
                }

                if (venda != null)
                {
                    using (var cmd2 = conn.CreateCommand())
                    {
                        cmd2.CommandText = "SELECT * FROM vendas_itens WHERE venda_id=@id ORDER BY id";
                        cmd2.Parameters.AddWithValue("@id", id);
                        using (var r = cmd2.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                venda.Itens.Add(new VendaItem
                                {
                                    Id = r.GetInt64(r.GetOrdinal("id")),
                                    Descricao = r.IsDBNull(r.GetOrdinal("descricao")) ? "" : r.GetString(r.GetOrdinal("descricao")),
                                    Quantidade = r.GetDouble(r.GetOrdinal("quantidade")),
                                    ValorUnit = r.GetDouble(r.GetOrdinal("valor_unit"))
                                });
                            }
                        }
                    }
                }
                return venda;
            }
        }

        public static void Excluir(long id)
        {
            using (var conn = Database.AbrirConexao())
            using (var tx = conn.BeginTransaction())
            {
                // Estorno: remove itens, venda e o lançamento do caixa desta venda.
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"DELETE FROM vendas_itens WHERE venda_id=@id;
DELETE FROM vendas WHERE id=@id;
DELETE FROM caixa WHERE descricao=@desc;";
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@desc", "Venda nº " + id);
                    cmd.ExecuteNonQuery();
                }
                tx.Commit();
            }
        }

        private static Venda LerLinha(SQLiteDataReader r)
        {
            return new Venda
            {
                Id = r.GetInt64(r.GetOrdinal("id")),
                Data = r.IsDBNull(r.GetOrdinal("data")) ? "" : r.GetString(r.GetOrdinal("data")),
                ClienteId = LerNullable(r, "cliente_id"),
                VeiculoId = LerNullable(r, "veiculo_id"),
                ValorTotal = r.GetDouble(r.GetOrdinal("valor_total")),
                FormaPagamento = LerStr(r, "forma_pagamento"),
                Observacoes = LerStr(r, "observacoes"),
                NomeCliente = LerStr(r, "nome_cliente"),
                VeiculoDesc = LerStr(r, "veiculo_desc")
            };
        }

        private static long? LerNullable(SQLiteDataReader r, string col)
        {
            int i = r.GetOrdinal(col);
            return r.IsDBNull(i) ? (long?)null : r.GetInt64(i);
        }

        private static string LerStr(SQLiteDataReader r, string col)
        {
            int i = r.GetOrdinal(col);
            return r.IsDBNull(i) ? "" : r.GetString(i);
        }
    }
}