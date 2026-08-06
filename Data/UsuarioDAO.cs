using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Security.Cryptography;
using System.Text;
using Soen___Torrezim.Models;

namespace Soen___Torrezim.Data
{
    /// <summary>Acesso a dados dos Usuários, com hash de senha (SHA-256).</summary>
    public static class UsuarioDAO
    {
        /// <summary>Retorna o hash hexadecimal SHA-256 de uma senha.</summary>
        public static string Hash(string senha)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(senha ?? ""));
                var sb = new StringBuilder();
                foreach (byte b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        public static long Salvar(Usuario u)
        {
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                if (u.Id > 0)
                {
                    cmd.CommandText = "UPDATE usuarios SET usuario=@usuario, nome=@nome, perfil=@perfil, ativo=@ativo WHERE id=@id";
                    cmd.Parameters.AddWithValue("@id", u.Id);
                }
                else
                {
                    cmd.CommandText = "INSERT INTO usuarios (usuario, senha, nome, perfil, ativo) VALUES (@usuario, @senha, @nome, @perfil, @ativo)";
                    cmd.Parameters.AddWithValue("@senha", UsuarioDAO.Hash(u.Senha));
                }
                cmd.Parameters.AddWithValue("@usuario", u.Login ?? "");
                cmd.Parameters.AddWithValue("@nome", Database.Nulo(u.Nome));
                cmd.Parameters.AddWithValue("@perfil", Database.Nulo(u.Perfil ?? "operador"));
                cmd.Parameters.AddWithValue("@ativo", u.Ativo ? 1 : 0);
                cmd.ExecuteNonQuery();
                if (u.Id <= 0) u.Id = (long)conn.LastInsertRowId;
                return u.Id;
            }
        }

        public static List<Usuario> Listar()
        {
            var lista = new List<Usuario>();
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM usuarios ORDER BY usuario";
                using (var leitor = cmd.ExecuteReader())
                {
                    while (leitor.Read())
                    {
                        lista.Add(new Usuario
                        {
                            Id = leitor.GetInt64(leitor.GetOrdinal("id")),
                            Login = LerStr(leitor, "usuario"),
                            Nome = LerStr(leitor, "nome"),
                            Perfil = LerStr(leitor, "perfil"),
                            Ativo = leitor.GetInt32(leitor.GetOrdinal("ativo")) != 0
                        });
                    }
                }
            }
            return lista;
        }

        public static Usuario Autenticar(string usuario, string senha)
        {
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM usuarios WHERE usuario=@u AND senha=@s AND ativo=1";
                cmd.Parameters.AddWithValue("@u", usuario ?? "");
                cmd.Parameters.AddWithValue("@s", Hash(senha));
                using (var leitor = cmd.ExecuteReader())
                {
                    if (leitor.Read())
                    {
                        return new Usuario
                        {
                            Id = leitor.GetInt64(leitor.GetOrdinal("id")),
                            Login = LerStr(leitor, "usuario"),
                            Nome = LerStr(leitor, "nome"),
                            Perfil = LerStr(leitor, "perfil"),
                            Ativo = true
                        };
                    }
                }
            }
            return null;
        }

        /// <summary>Garante que existe ao menos um usuário admin (padrão: admin / admin).</summary>
        public static void GarantirAdminPadrao()
        {
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM usuarios";
                long n = (long)cmd.ExecuteScalar();
                if (n == 0)
                {
                    var admin = new Usuario { Login = "admin", Senha = "admin", Nome = "Administrador", Perfil = "admin" };
                    Salvar(admin);
                }
            }
        }

        public static void Excluir(long id)
        {
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM usuarios WHERE id=@id";
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        public static void AtualizarSenha(long id, string senha)
        {
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "UPDATE usuarios SET senha=@s WHERE id=@id";
                cmd.Parameters.AddWithValue("@s", Hash(senha));
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