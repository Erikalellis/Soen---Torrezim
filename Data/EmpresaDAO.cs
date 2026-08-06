using System;
using System.Data.SQLite;
using Soen___Torrezim.Models;

namespace Soen___Torrezim.Data
{
    /// <summary>Acesso a dados da Empresa (uma única configuração ativa).</summary>
    public static class EmpresaDAO
    {
        /// <summary>Retorna os dados cadastrados ou um objeto em branco se ainda não houver.</summary>
        public static Empresa Obter()
        {
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM empresa ORDER BY id LIMIT 1";
                using (var leitor = cmd.ExecuteReader())
                {
                    if (leitor.Read())
                    {
                        return new Empresa
                        {
                            Id = leitor.GetInt64(leitor.GetOrdinal("id")),
                            Nome = LerStr(leitor, "nome"),
                            Cnpj = LerStr(leitor, "cnpj"),
                            Telefone = LerStr(leitor, "telefone"),
                            Endereco = LerStr(leitor, "endereco"),
                            Cidade = LerStr(leitor, "cidade"),
                            Estado = LerStr(leitor, "estado"),
                            Email = LerStr(leitor, "email"),
                            Site = LerStr(leitor, "site"),
                            Observacoes = LerStr(leitor, "observacoes")
                        };
                    }
                }
            }
            return new Empresa();
        }

        public static void Salvar(Empresa e)
        {
            using (var conn = Database.AbrirConexao())
            {
                // Garante que só uma linha exista.
                long id = 0;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT id FROM empresa ORDER BY id LIMIT 1";
                    var r = cmd.ExecuteScalar();
                    if (r != null && r != DBNull.Value) id = Convert.ToInt64(r);
                }

                using (var cmd = conn.CreateCommand())
                {
                    if (id > 0)
                    {
                        cmd.CommandText = @"UPDATE empresa SET nome=@nome, cnpj=@cnpj, telefone=@telefone,
endereco=@endereco, cidade=@cidade, estado=@estado, email=@email, site=@site, observacoes=@obs WHERE id=@id";
                        cmd.Parameters.AddWithValue("@id", id);
                    }
                    else
                    {
                        cmd.CommandText = @"INSERT INTO empresa (nome, cnpj, telefone, endereco, cidade, estado, email, site, observacoes)
VALUES (@nome, @cnpj, @telefone, @endereco, @cidade, @estado, @email, @site, @obs)";
                    }
                    cmd.Parameters.AddWithValue("@nome", Database.Nulo(e.Nome));
                    cmd.Parameters.AddWithValue("@cnpj", Database.Nulo(e.Cnpj));
                    cmd.Parameters.AddWithValue("@telefone", Database.Nulo(e.Telefone));
                    cmd.Parameters.AddWithValue("@endereco", Database.Nulo(e.Endereco));
                    cmd.Parameters.AddWithValue("@cidade", Database.Nulo(e.Cidade));
                    cmd.Parameters.AddWithValue("@estado", Database.Nulo(e.Estado));
                    cmd.Parameters.AddWithValue("@email", Database.Nulo(e.Email));
                    cmd.Parameters.AddWithValue("@site", Database.Nulo(e.Site));
                    cmd.Parameters.AddWithValue("@obs", Database.Nulo(e.Observacoes));
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static string LerStr(SQLiteDataReader r, string col)
        {
            int i = r.GetOrdinal(col);
            return r.IsDBNull(i) ? "" : r.GetString(i);
        }
    }
}