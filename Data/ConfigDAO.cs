using System;
using System.Data.SQLite;

namespace Soen___Torrezim.Data
{
    /// <summary>
    /// Armazenamento genérico chave/valor (tabela "config"), usado para
    /// preferências do sistema — incluindo as configurações do backup automático.
    /// </summary>
    public static class ConfigDAO
    {
        private static void GarantirTabela(SQLiteConnection conn)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "CREATE TABLE IF NOT EXISTS config (chave TEXT PRIMARY KEY, valor TEXT)";
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>Obtém o valor de uma chave; retorna <paramref name="padrao"/> se não existir.</summary>
        public static string Obter(string chave, string padrao = "")
        {
            using (var conn = Database.AbrirConexao())
            {
                GarantirTabela(conn);
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT valor FROM config WHERE chave=@c";
                    cmd.Parameters.AddWithValue("@c", chave);
                    var r = cmd.ExecuteScalar();
                    return r == null || r == System.DBNull.Value ? padrao : r.ToString();
                }
            }
        }

        /// <summary>Salva ou atualiza o valor de uma chave.</summary>
        public static void Salvar(string chave, string valor)
        {
            using (var conn = Database.AbrirConexao())
            {
                GarantirTabela(conn);
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "REPLACE INTO config (chave, valor) VALUES (@c, @v)";
                    cmd.Parameters.AddWithValue("@c", chave);
                    cmd.Parameters.AddWithValue("@v", Database.Nulo(valor));
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static int ObterInt(string chave, int padrao)
        {
            int v;
            return int.TryParse(Obter(chave, padrao.ToString()), out v) ? v : padrao;
        }
    }
}