using System.Collections.Generic;
using System.Data.SQLite;
using Soen___Torrezim.Models;

namespace Soen___Torrezim.Data
{
    /// <summary>Acesso a dados dos Técnicos.</summary>
    public static class TecnicoDAO
    {
        public static long Salvar(Tecnico t)
        {
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                if (t.Id > 0)
                {
                    cmd.CommandText = "UPDATE tecnicos SET nome=@nome, telefone=@telefone, cargo=@cargo, comissao_percent=@c, ativo=@ativo WHERE id=@id";
                    cmd.Parameters.AddWithValue("@id", t.Id);
                }
                else
                {
                    cmd.CommandText = "INSERT INTO tecnicos (nome, telefone, cargo, comissao_percent, ativo) VALUES (@nome, @telefone, @cargo, @c, @ativo)";
                }
                cmd.Parameters.AddWithValue("@nome", t.Nome ?? "");
                cmd.Parameters.AddWithValue("@telefone", Database.Nulo(t.Telefone));
                cmd.Parameters.AddWithValue("@cargo", Database.Nulo(t.Cargo));
                cmd.Parameters.AddWithValue("@c", t.ComissaoPercent);
                cmd.Parameters.AddWithValue("@ativo", t.Ativo ? 1 : 0);
                cmd.ExecuteNonQuery();
                if (t.Id <= 0) t.Id = (long)conn.LastInsertRowId;
                return t.Id;
            }
        }

        public static List<Tecnico> Listar(bool somenteAtivos = false)
        {
            var lista = new List<Tecnico>();
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = somenteAtivos
                    ? "SELECT * FROM tecnicos WHERE ativo=1 ORDER BY nome"
                    : "SELECT * FROM tecnicos ORDER BY nome";
                using (var leitor = cmd.ExecuteReader())
                {
                    while (leitor.Read())
                    {
                        lista.Add(new Tecnico
                        {
                            Id = leitor.GetInt64(leitor.GetOrdinal("id")),
                            Nome = LerStr(leitor, "nome"),
                            Telefone = LerStr(leitor, "telefone"),
                            Cargo = LerStr(leitor, "cargo"),
                            ComissaoPercent = leitor.GetDouble(leitor.GetOrdinal("comissao_percent")),
                            Ativo = leitor.GetInt32(leitor.GetOrdinal("ativo")) != 0
                        });
                    }
                }
            }
            return lista;
        }

        public static void Excluir(long id)
        {
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM tecnicos WHERE id=@id";
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Resumo de comissões por técnico: total gerado (OS convertidas em venda),
        /// total pago e saldo devedor.
        /// </summary>
        public static List<ComissaoResumo> ListarResumoComissoes()
        {
            var lista = new List<ComissaoResumo>();
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT t.id AS tec_id, t.nome AS tec_nome,
    COALESCE(SUM(CASE WHEN o.status='convertido' THEN o.comissao ELSE 0 END),0) AS gerado,
    COALESCE((SELECT SUM(cp.valor) FROM comissao_pagamentos cp WHERE cp.tecnico_id = t.id),0) AS pago
FROM tecnicos t
LEFT JOIN orcamentos o ON o.tecnico_id = t.id
GROUP BY t.id, t.nome
ORDER BY t.nome";
                using (var leitor = cmd.ExecuteReader())
                {
                    while (leitor.Read())
                    {
                        lista.Add(new ComissaoResumo
                        {
                            TecnicoId = leitor.GetInt64(leitor.GetOrdinal("tec_id")),
                            NomeTecnico = LerStr(leitor, "tec_nome"),
                            Gerado = leitor.GetDouble(leitor.GetOrdinal("gerado")),
                            Pago = leitor.GetDouble(leitor.GetOrdinal("pago"))
                        });
                    }
                }
            }
            return lista;
        }

        /// <summary>Registra um pagamento de comissão para o técnico.</summary>
        public static void RegistrarPagamento(long tecnicoId, double valor, string observacao)
        {
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO comissao_pagamentos (tecnico_id, valor, observacao) VALUES (@t, @v, @o)";
                cmd.Parameters.AddWithValue("@t", tecnicoId);
                cmd.Parameters.AddWithValue("@v", valor);
                cmd.Parameters.AddWithValue("@o", Database.Nulo(observacao));
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>Exclui um pagamento de comissão.</summary>
        public static void ExcluirPagamento(long pagamentoId)
        {
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM comissao_pagamentos WHERE id=@id";
                cmd.Parameters.AddWithValue("@id", pagamentoId);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>Lista os pagamentos de comissão, opcionalmente filtrados por técnico.</summary>
        public static List<ComissaoPagamento> ListarPagamentos(long? tecnicoId = null)
        {
            var lista = new List<ComissaoPagamento>();
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT cp.id, cp.tecnico_id, t.nome AS tec_nome, cp.valor, cp.data, cp.observacao
FROM comissao_pagamentos cp
JOIN tecnicos t ON t.id = cp.tecnico_id
WHERE (@t IS NULL OR cp.tecnico_id = @t)
ORDER BY cp.data DESC, cp.id DESC";
                cmd.Parameters.AddWithValue("@t", tecnicoId.HasValue ? (object)tecnicoId.Value : System.DBNull.Value);
                using (var leitor = cmd.ExecuteReader())
                {
                    while (leitor.Read())
                    {
                        lista.Add(new ComissaoPagamento
                        {
                            Id = leitor.GetInt64(leitor.GetOrdinal("id")),
                            TecnicoId = leitor.GetInt64(leitor.GetOrdinal("tecnico_id")),
                            NomeTecnico = LerStr(leitor, "tec_nome"),
                            Valor = leitor.GetDouble(leitor.GetOrdinal("valor")),
                            Data = LerStr(leitor, "data"),
                            Observacao = LerStr(leitor, "observacao")
                        });
                    }
                }
            }
            return lista;
        }

        private static string LerStr(SQLiteDataReader r, string col)
        {
            int i = r.GetOrdinal(col);
            return r.IsDBNull(i) ? "" : r.GetString(i);
        }
    }
}