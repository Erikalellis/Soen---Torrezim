using System.Collections.Generic;
using System.Data.SQLite;
using Soen___Torrezim.Models;

namespace Soen___Torrezim.Data
{
    /// <summary>Acesso a dados do Caixa.</summary>
    public static class CaixaDAO
    {
        /// <summary>Lança uma entrada ou saída. Retorna o Id.</summary>
        public static long Salvar(LancamentoCaixa l)
        {
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT INTO caixa (data, tipo, descricao, valor)
VALUES (@data, @tipo, @descricao, @valor)";
                cmd.Parameters.AddWithValue("@data", Database.Nulo(l.Data));
                cmd.Parameters.AddWithValue("@tipo", l.Tipo ?? "entrada");
                cmd.Parameters.AddWithValue("@descricao", Database.Nulo(l.Descricao));
                cmd.Parameters.AddWithValue("@valor", l.Valor);
                cmd.ExecuteNonQuery();
                return conn.LastInsertRowId;
            }
        }

        /// <summary>Lista os lançamentos (mais recentes primeiro).</summary>
        public static List<LancamentoCaixa> Listar()
        {
            var lista = new List<LancamentoCaixa>();
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM caixa ORDER BY id DESC";
                using (var leitor = cmd.ExecuteReader())
                {
                    while (leitor.Read())
                    {
                        lista.Add(new LancamentoCaixa
                        {
                            Id        = leitor.GetInt64(leitor.GetOrdinal("id")),
                            Data      = LerStr(leitor, "data"),
                            Tipo      = LerStr(leitor, "tipo"),
                            Descricao = LerStr(leitor, "descricao"),
                            Valor     = leitor.GetDouble(leitor.GetOrdinal("valor"))
                        });
                    }
                }
            }
            return lista;
        }

        /// <summary>Lista os lançamentos de um dia específico (formato aaaa-mm-dd).</summary>
        public static List<LancamentoCaixa> ListarPorDia(string dia)
        {
            var lista = new List<LancamentoCaixa>();
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM caixa WHERE substr(data,1,10) = @dia ORDER BY id";
                cmd.Parameters.AddWithValue("@dia", dia);
                using (var leitor = cmd.ExecuteReader())
                {
                    while (leitor.Read())
                    {
                        lista.Add(new LancamentoCaixa
                        {
                            Id        = leitor.GetInt64(leitor.GetOrdinal("id")),
                            Data      = LerStr(leitor, "data"),
                            Tipo      = LerStr(leitor, "tipo"),
                            Descricao = LerStr(leitor, "descricao"),
                            Valor     = leitor.GetDouble(leitor.GetOrdinal("valor"))
                        });
                    }
                }
            }
            return lista;
        }

        /// <summary>Soma total de entradas menos saídas.</summary>
        public static double Saldo()
        {
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COALESCE(SUM(CASE WHEN tipo='entrada' THEN valor ELSE -valor END),0) FROM caixa";
                object r = cmd.ExecuteScalar();
                return r == System.DBNull.Value || r == null ? 0 : System.Convert.ToDouble(r);
            }
        }

        public static void Excluir(long id)
        {
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM caixa WHERE id=@id";
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        private static string LerStr(SQLiteDataReader r, string col)
        {
            int i = r.GetOrdinal(col);
            return r.IsDBNull(i) ? "" : r.GetString(i);
        }
    }
}