using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Security.Cryptography;
using System.Text;
using Soen___Torrezim.Models;

namespace Soen___Torrezim.Data
{
    /// <summary>Acesso a dados dos Usuários, com senhas protegidas por PBKDF2 (salt + iterações).</summary>
    public static class UsuarioDAO
    {
        private const string PREFIXO_PBKDF2 = "PBKDF2$";
        private const int ITERACOES = 10000;
        private const int TAMANHO_SAL = 16;
        private const int TAMANHO_HASH = 32;

        /// <summary>Deriva a senha em PBKDF2 com salt aleatório — formato "PBKDF2$iter$salt$hash".</summary>
        public static string Hash(string senha)
        {
            byte[] salt = new byte[TAMANHO_SAL];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            return DerivaHash(senha, salt, ITERACOES);
        }

        private static string DerivaHash(string senha, byte[] salt, int iteracoes)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(senha ?? "", salt, iteracoes))
            {
                byte[] hash = pbkdf2.GetBytes(TAMANHO_HASH);
                return PREFIXO_PBKDF2 + iteracoes.ToString() + "$" +
                    Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(hash);
            }
        }

        private static bool VerificarHash(string senha, string armazenada)
        {
            if (string.IsNullOrEmpty(armazenada)) return false;
            string[] partes = armazenada.Split('$');
            if (partes.Length == 4 && partes[0] == "PBKDF2")
            {
                try
                {
                    int iteracoes = int.Parse(partes[1]);
                    byte[] salt = Convert.FromBase64String(partes[2]);
                    byte[] esperado = Convert.FromBase64String(partes[3]);
                    using (var pbkdf2 = new Rfc2898DeriveBytes(senha ?? "", salt, iteracoes))
                    {
                        byte[] atual = pbkdf2.GetBytes(esperado.Length);
                        if (atual.Length != esperado.Length) return false;
                        int diff = 0;
                        for (int i = 0; i < atual.Length; i++) diff |= atual[i] ^ esperado[i];
                        return diff == 0;
                    }
                }
                catch
                {
                    return false;
                }
            }
            // Fallback: hashes SHA-256 geradas antes da migração para PBKDF2.
            return string.Equals(HashSha256(senha), armazenada, StringComparison.OrdinalIgnoreCase);
        }

        private static string HashSha256(string senha)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(senha ?? ""));
                var sb = new StringBuilder();
                foreach (byte b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        private static void UpgradeSenha(long id, string senha)
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

        /// <summary>Verifica se já existe um usuário com o mesmo login.</summary>
        public static bool ExisteUsuario(string login)
        {
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM usuarios WHERE usuario=@u";
                cmd.Parameters.AddWithValue("@u", login ?? "");
                return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
            }
        }

        public static Usuario Autenticar(string usuario, string senha)
        {
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM usuarios WHERE usuario=@u AND ativo=1";
                cmd.Parameters.AddWithValue("@u", usuario ?? "");
                using (var leitor = cmd.ExecuteReader())
                {
                    if (leitor.Read())
                    {
                        string senhaHash = LerStr(leitor, "senha");
                        if (VerificarHash(senha, senhaHash))
                        {
                            long id = leitor.GetInt64(leitor.GetOrdinal("id"));
                            if (!senhaHash.StartsWith(PREFIXO_PBKDF2))
                                UpgradeSenha(id, senha);
                            return new Usuario
                            {
                                Id = id,
                                Login = LerStr(leitor, "usuario"),
                                Nome = LerStr(leitor, "nome"),
                                Perfil = LerStr(leitor, "perfil"),
                                Ativo = true
                            };
                        }
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