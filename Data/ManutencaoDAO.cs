using System.Collections.Generic;
using System.Data.SQLite;
using Soen___Torrezim.Models;

namespace Soen___Torrezim.Data
{
    /// <summary>Acesso a dados da entidade Manutenção.</summary>
    public static class ManutencaoDAO
    {
        /// <summary>Grava uma manutenção nova ou atualiza uma existente (Id &gt; 0). Retorna o Id.</summary>
        public static long Salvar(Manutencao m)
        {
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                if (m.Id > 0)
                {
                    cmd.CommandText = @"UPDATE manutencoes SET
    veiculo_id = @veiculo, cliente_id = @cliente, data = @data,
    descricao = @descricao, valor = @valor, status = @status
WHERE id = @id";
                    cmd.Parameters.AddWithValue("@id", m.Id);
                }
                else
                {
                    cmd.CommandText = @"INSERT INTO manutencoes (veiculo_id, cliente_id, data, descricao, valor, status)
VALUES (@veiculo, @cliente, @data, @descricao, @valor, @status)";
                }

                cmd.Parameters.AddWithValue("@veiculo", m.VeiculoId);
                cmd.Parameters.AddWithValue("@cliente", (object)m.ClienteId ?? System.DBNull.Value);
                cmd.Parameters.AddWithValue("@data", Database.Nulo(m.Data));
                cmd.Parameters.AddWithValue("@descricao", Database.Nulo(m.Descricao));
                cmd.Parameters.AddWithValue("@valor", m.Valor);
                cmd.Parameters.AddWithValue("@status", Database.Nulo(m.Status ?? "aberta"));

                cmd.ExecuteNonQuery();

                if (m.Id <= 0)
                {
                    m.Id = (long)conn.LastInsertRowId;
                }
                return m.Id;
            }
        }

        /// <summary>Lista manutenções. Opcional filtrar por veículo.</summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL injection for security vulnerabilities",
            Justification = "O único valor dinâmico é o parâmetro @veiculo (seguro). O SQL é texto estático.")]
        public static List<Manutencao> Listar(long? veiculoId)
        {
            var lista = new List<Manutencao>();
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT m.*, v.placa, v.marca, v.modelo, c.nome_razao AS nome_cliente
FROM manutencoes m
LEFT JOIN veiculos v ON v.id = m.veiculo_id
LEFT JOIN clientes c ON c.id = m.cliente_id WHERE 1=1";
                if (veiculoId.HasValue)
                {
                    cmd.CommandText += " AND m.veiculo_id = @veiculo";
                    cmd.Parameters.AddWithValue("@veiculo", veiculoId.Value);
                }
                cmd.CommandText += " ORDER BY m.data DESC, m.id DESC";

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

        /// <summary>Busca uma manutenção pelo Id.</summary>
        public static Manutencao BuscarPorId(long id)
        {
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT m.*, v.placa, v.marca, v.modelo, c.nome_razao AS nome_cliente
FROM manutencoes m
LEFT JOIN veiculos v ON v.id = m.veiculo_id
LEFT JOIN clientes c ON c.id = m.cliente_id
WHERE m.id = @id";
                cmd.Parameters.AddWithValue("@id", id);
                using (var leitor = cmd.ExecuteReader())
                {
                    if (leitor.Read())
                    {
                        return LerLinha(leitor);
                    }
                }
            }
            return null;
        }

        /// <summary>Exclui uma manutenção pelo Id.</summary>
        public static void Excluir(long id)
        {
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM manutencoes WHERE id = @id";
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        private static Manutencao LerLinha(SQLiteDataReader r)
        {
            var m = new Manutencao
            {
                Id          = r.GetInt64(r.GetOrdinal("id")),
                VeiculoId   = r.GetInt64(r.GetOrdinal("veiculo_id")),
                Data        = LerString(r, "data"),
                Descricao   = LerString(r, "descricao"),
                Valor       = r.IsDBNull(r.GetOrdinal("valor")) ? 0 : r.GetDouble(r.GetOrdinal("valor")),
                Status      = LerString(r, "status"),
                Placa       = LerString(r, "placa"),
                VeiculoDesc = LerString(r, "modelo"),
                NomeCliente = LerString(r, "nome_cliente")
            };
            int idxCli = r.GetOrdinal("cliente_id");
            m.ClienteId = r.IsDBNull(idxCli) ? (long?)null : r.GetInt64(idxCli);
            return m;
        }

        private static string LerString(SQLiteDataReader r, string coluna)
        {
            int idx = r.GetOrdinal(coluna);
            return r.IsDBNull(idx) ? "" : r.GetString(idx);
        }
    }
}