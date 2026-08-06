using System.Collections.Generic;
using System.Data.SQLite;
using Soen___Torrezim.Models;

namespace Soen___Torrezim.Data
{
    /// <summary>Acesso a dados das Contas (pagar/receber) do módulo financeiro.</summary>
    public static class FinanceiroDAO
    {
        public static long Salvar(ContaFinanceira c)
        {
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                if (c.Id > 0)
                {
                    cmd.CommandText = @"UPDATE financeiro SET tipo=@tipo, descricao=@desc, fornecedor=@forn,
vencimento=@venc, valor=@valor, status=@status WHERE id=@id";
                    cmd.Parameters.AddWithValue("@id", c.Id);
                }
                else
                {
                    cmd.CommandText = @"INSERT INTO financeiro (tipo, descricao, fornecedor, vencimento, valor, status)
VALUES (@tipo, @desc, @forn, @venc, @valor, @status)";
                }
                cmd.Parameters.AddWithValue("@tipo", c.Tipo ?? "pagar");
                cmd.Parameters.AddWithValue("@desc", Database.Nulo(c.Descricao));
                cmd.Parameters.AddWithValue("@forn", Database.Nulo(c.Fornecedor));
                cmd.Parameters.AddWithValue("@venc", Database.Nulo(c.Vencimento));
                cmd.Parameters.AddWithValue("@valor", c.Valor);
                cmd.Parameters.AddWithValue("@status", Database.Nulo(c.Status ?? "em_aberto"));
                cmd.ExecuteNonQuery();
                if (c.Id <= 0) c.Id = (long)conn.LastInsertRowId;
                return c.Id;
            }
        }

        public static List<ContaFinanceira> Listar(string tipo = "")
        {
            var lista = new List<ContaFinanceira>();
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = string.IsNullOrEmpty(tipo)
                    ? "SELECT * FROM financeiro ORDER BY vencimento, id"
                    : "SELECT * FROM financeiro WHERE tipo=@tipo ORDER BY vencimento, id";
                if (!string.IsNullOrEmpty(tipo)) cmd.Parameters.AddWithValue("@tipo", tipo);
                using (var leitor = cmd.ExecuteReader())
                {
                    while (leitor.Read())
                    {
                        lista.Add(new ContaFinanceira
                        {
                            Id         = leitor.GetInt64(leitor.GetOrdinal("id")),
                            Tipo       = LerStr(leitor, "tipo"),
                            Descricao  = LerStr(leitor, "descricao"),
                            Fornecedor = LerStr(leitor, "fornecedor"),
                            Vencimento = LerStr(leitor, "vencimento"),
                            Valor      = leitor.GetDouble(leitor.GetOrdinal("valor")),
                            Status     = LerStr(leitor, "status"),
                            CriadoEm   = LerStr(leitor, "criado_em")
                        });
                    }
                }
            }
            return lista;
        }

        public static double TotalAberto(string tipo)
        {
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COALESCE(SUM(valor),0) FROM financeiro WHERE tipo=@tipo AND status='em_aberto'";
                cmd.Parameters.AddWithValue("@tipo", tipo);
                object r = cmd.ExecuteScalar();
                return r == null || r == System.DBNull.Value ? 0 : System.Convert.ToDouble(r);
            }
        }

        public static void Excluir(long id)
        {
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM financeiro WHERE id=@id";
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