using System.Collections.Generic;
using System.Data.SQLite;
using Soen___Torrezim.Models;

namespace Soen___Torrezim.Data
{
    /// <summary>Acesso a dados da entidade Veículo.</summary>
    public static class VeiculoDAO
    {
        /// <summary>Grava um veículo novo ou atualiza um existente (Id &gt; 0). Retorna o Id.</summary>
        public static long Salvar(Veiculo v)
        {
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                if (v.Id > 0)
                {
                    cmd.CommandText = @"UPDATE veiculos SET
    cliente_id = @cliente, placa = @placa, marca = @marca, modelo = @modelo,
    cor = @cor, observacoes = @obs, quilometragem = @km, proxima_revisao = @rev
WHERE id = @id";
                    cmd.Parameters.AddWithValue("@id", v.Id);
                }
                else
                {
                    cmd.CommandText = @"INSERT INTO veiculos (cliente_id, placa, marca, modelo, cor, observacoes, quilometragem, proxima_revisao)
VALUES (@cliente, @placa, @marca, @modelo, @cor, @obs, @km, @rev)";
                }

                cmd.Parameters.AddWithValue("@cliente", v.ClienteId);
                cmd.Parameters.AddWithValue("@placa", Database.Nulo(v.Placa));
                cmd.Parameters.AddWithValue("@marca", Database.Nulo(v.Marca));
                cmd.Parameters.AddWithValue("@modelo", Database.Nulo(v.Modelo));
                cmd.Parameters.AddWithValue("@cor", Database.Nulo(v.Cor));
                cmd.Parameters.AddWithValue("@obs", Database.Nulo(v.Observacoes));
                cmd.Parameters.AddWithValue("@km", v.Quilometragem);
                cmd.Parameters.AddWithValue("@rev", Database.Nulo(v.ProximaRevisao));

                cmd.ExecuteNonQuery();

                if (v.Id <= 0)
                {
                    v.Id = (long)conn.LastInsertRowId;
                }
                return v.Id;
            }
        }

        /// <summary>Lista veículos. Opcional filtrar por cliente.</summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL injection for security vulnerabilities",
            Justification = "O único valor dinâmico é o parâmetro @cliente (seguro). O SQL é texto estático.")]
        public static List<Veiculo> Listar(long? clienteId)
        {
            var lista = new List<Veiculo>();
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT v.*, c.nome_razao AS nome_cliente
FROM veiculos v LEFT JOIN clientes c ON c.id = v.cliente_id WHERE 1=1";
                if (clienteId.HasValue)
                {
                    cmd.CommandText += " AND v.cliente_id = @cliente";
                    cmd.Parameters.AddWithValue("@cliente", clienteId.Value);
                }
                cmd.CommandText += " ORDER BY v.placa";

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

        /// <summary>Busca um veículo pelo Id.</summary>
        public static Veiculo BuscarPorId(long id)
        {
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT v.*, c.nome_razao AS nome_cliente FROM veiculos v LEFT JOIN clientes c ON c.id = v.cliente_id WHERE v.id = @id";
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

        /// <summary>Exclui um veículo pelo Id.</summary>
        public static void Excluir(long id)
        {
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM veiculos WHERE id = @id";
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        private static Veiculo LerLinha(SQLiteDataReader r)
        {
            var v = new Veiculo
            {
                Id          = r.GetInt64(r.GetOrdinal("id")),
                ClienteId   = r.GetInt64(r.GetOrdinal("cliente_id")),
                Placa       = LerString(r, "placa"),
                Marca       = LerString(r, "marca"),
                Modelo      = LerString(r, "modelo"),
                Cor         = LerString(r, "cor"),
                Observacoes = LerString(r, "observacoes"),
                NomeCliente = LerString(r, "nome_cliente")
            };
            int idx;
            idx = r.GetOrdinal("quilometragem");
            if (!r.IsDBNull(idx)) v.Quilometragem = r.GetDouble(idx);
            v.ProximaRevisao = LerString(r, "proxima_revisao");
            return v;
        }

        private static string LerString(SQLiteDataReader r, string coluna)
        {
            int idx = r.GetOrdinal(coluna);
            return r.IsDBNull(idx) ? "" : r.GetString(idx);
        }
    }
}