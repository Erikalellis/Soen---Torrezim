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

        private static string LerStr(SQLiteDataReader r, string col)
        {
            int i = r.GetOrdinal(col);
            return r.IsDBNull(i) ? "" : r.GetString(i);
        }
    }
}