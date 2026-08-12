using System;
using System.Data.SQLite;
using System.IO;
using Soen___Torrezim.Models;

namespace Soen___Torrezim.Data
{
    /// <summary>Acesso a dados da Empresa (uma única configuração ativa).</summary>
    public static class EmpresaDAO
    {
        private static bool? _temColunasNovas;

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
                        var e = new Empresa
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
                        // Campos opcionais (podem não existir em DB antigo)
                        if (TemColunasNovas(conn))
                        {
                            e.BackgroundImagePath = LerStr(leitor, "background_image");
                            e.BackgroundMode = LerStr(leitor, "background_mode");
                            e.LogoPath = LerStr(leitor, "logo_path");
                            e.LogoWidth = leitor.IsDBNull(leitor.GetOrdinal("logo_width")) ? 0 : leitor.GetInt32(leitor.GetOrdinal("logo_width"));
                            e.LogoHeight = leitor.IsDBNull(leitor.GetOrdinal("logo_height")) ? 0 : leitor.GetInt32(leitor.GetOrdinal("logo_height"));
                        }
                        return e;
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
                    bool completas = TemColunasNovas(conn);
                    if (id > 0)
                    {
                        cmd.CommandText = completas ?
@"UPDATE empresa SET nome=@nome, cnpj=@cnpj, telefone=@telefone,
endereco=@endereco, cidade=@cidade, estado=@estado, email=@email, site=@site, observacoes=@obs, 
background_image=@background, background_mode=@mode, logo_path=@logo, logo_width=@lwidth, logo_height=@lheight WHERE id=@id" :
@"UPDATE empresa SET nome=@nome, cnpj=@cnpj, telefone=@telefone,
endereco=@endereco, cidade=@cidade, estado=@estado, email=@email, site=@site, observacoes=@obs WHERE id=@id";
                        cmd.Parameters.AddWithValue("@id", id);
                    }
                    else
                    {
                        cmd.CommandText = completas ?
@"INSERT INTO empresa (nome, cnpj, telefone, endereco, cidade, estado, email, site, observacoes, background_image, background_mode, logo_path, logo_width, logo_height)
VALUES (@nome, @cnpj, @telefone, @endereco, @cidade, @estado, @email, @site, @obs, @background, @mode, @logo, @lwidth, @lheight)" :
@"INSERT INTO empresa (nome, cnpj, telefone, endereco, cidade, estado, email, site, observacoes)
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

                    if (completas)
                    {
                        cmd.Parameters.AddWithValue("@background", Database.Nulo(e.BackgroundImagePath));
                        cmd.Parameters.AddWithValue("@mode", Database.Nulo(e.BackgroundMode));
                        cmd.Parameters.AddWithValue("@logo", Database.Nulo(e.LogoPath));
                        cmd.Parameters.AddWithValue("@lwidth", e.LogoWidth);
                        cmd.Parameters.AddWithValue("@lheight", e.LogoHeight);
                    }

                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex);
                        throw;
                    }
                }
            }
        }

        private static string LerStr(SQLiteDataReader r, string col)
        {
            int i = r.GetOrdinal(col);
            return r.IsDBNull(i) ? "" : r.GetString(i);
        }

        private static bool TemColunasNovas(SQLiteConnection conn)
        {
            if (_temColunasNovas == null)
            {
                _temColunasNovas = HasColumn(conn, "background_image") &&
                                   HasColumn(conn, "background_mode") &&
                                   HasColumn(conn, "logo_path") &&
                                   HasColumn(conn, "logo_width") &&
                                   HasColumn(conn, "logo_height");
            }
            return _temColunasNovas.Value;
        }

        private static bool HasColumn(SQLiteConnection conn, string coluna)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "PRAGMA table_info(empresa)";
                using (var leitor = cmd.ExecuteReader())
                {
                    while (leitor.Read())
                        if (string.Equals(leitor.GetString(1), coluna, StringComparison.OrdinalIgnoreCase)) return true;
                }
            }
            return false;
        }
    }
}