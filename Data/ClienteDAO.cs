using System.Collections.Generic;
using System.Data.SQLite;
using Soen___Torrezim.Models;

namespace Soen___Torrezim.Data
{
    /// <summary>
    /// Acesso a dados da entidade Cliente. Toda comunicação com o banco
    /// acontece aqui (nunca na tela).
    /// </summary>
    public static class ClienteDAO
    {
        /// <summary>Grava um cliente novo ou atualiza um existente (Id &gt; 0). Retorna o Id.</summary>
        public static long Salvar(Cliente c)
        {
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                if (c.Id > 0)
                {
                    cmd.CommandText = @"
UPDATE clientes SET
    tipo        = @tipo,
    cpf_cnpj    = @cpf,
    nome_razao  = @nome,
    sexo        = @sexo,
    nascimento  = @nasc,
    cep         = @cep,
    endereco    = @endereco,
    complemento = @complemento,
    bairro      = @bairro,
    cidade      = @cidade,
    estado      = @estado,
    fone1       = @fone1,
    fone2       = @fone2,
    email1      = @email1,
    email2      = @email2,
    responsavel = @responsavel,
    funcao      = @funcao
WHERE id = @id";
                    cmd.Parameters.AddWithValue("@id", c.Id);
                }
                else
                {
                    cmd.CommandText = @"
INSERT INTO clientes (
    tipo, cpf_cnpj, nome_razao, sexo, nascimento, cep, endereco, complemento,
    bairro, cidade, estado, fone1, fone2, email1, email2, responsavel, funcao)
VALUES (
    @tipo, @cpf, @nome, @sexo, @nascimento, @cep, @endereco, @complemento,
    @bairro, @cidade, @estado, @fone1, @fone2, @email1, @email2, @responsavel, @funcao)";
                }

                cmd.Parameters.AddWithValue("@tipo", Database.Nulo(c.Tipo ?? "cliente"));
                cmd.Parameters.AddWithValue("@cpf", Database.Nulo(c.CpfCnpj));
                cmd.Parameters.AddWithValue("@nome", (object)c.NomeRazao ?? "");
                cmd.Parameters.AddWithValue("@sexo", Database.Nulo(c.Sexo));
                cmd.Parameters.AddWithValue("@nascimento", Database.Nulo(c.Nascimento));
                cmd.Parameters.AddWithValue("@cep", Database.Nulo(c.Cep));
                cmd.Parameters.AddWithValue("@endereco", Database.Nulo(c.Endereco));
                cmd.Parameters.AddWithValue("@complemento", Database.Nulo(c.Complemento));
                cmd.Parameters.AddWithValue("@bairro", Database.Nulo(c.Bairro));
                cmd.Parameters.AddWithValue("@cidade", Database.Nulo(c.Cidade));
                cmd.Parameters.AddWithValue("@estado", Database.Nulo(c.Estado));
                cmd.Parameters.AddWithValue("@fone1", Database.Nulo(c.Fone1));
                cmd.Parameters.AddWithValue("@fone2", Database.Nulo(c.Fone2));
                cmd.Parameters.AddWithValue("@email1", Database.Nulo(c.Email1));
                cmd.Parameters.AddWithValue("@email2", Database.Nulo(c.Email2));
                cmd.Parameters.AddWithValue("@responsavel", Database.Nulo(c.Responsavel));
                cmd.Parameters.AddWithValue("@funcao", Database.Nulo(c.Funcao));

                cmd.ExecuteNonQuery();

                if (c.Id <= 0)
                {
                    c.Id = (long)conn.LastInsertRowId;
                }
                return c.Id;
            }
        }

        /// <summary>Lista clientes. Se <paramref name="filtro"/> não for vazio, busca por nome ou CPF/CNPJ.</summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL injection for security vulnerabilities",
            Justification = "O único valor dinâmico é o parâmetro @filtro (seguro). O SQL é texto estático.")]
        public static List<Cliente> Listar(string filtro = "")
        {
            var lista = new List<Cliente>();
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM clientes WHERE 1=1";
                if (!string.IsNullOrWhiteSpace(filtro))
                {
                    cmd.CommandText += " AND (nome_razao LIKE @f OR cpf_cnpj LIKE @f)";
                    cmd.Parameters.AddWithValue("@f", "%" + filtro.Trim() + "%");
                }
                cmd.CommandText += " ORDER BY nome_razao";

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

        /// <summary>Busca um cliente pelo Id.</summary>
        public static Cliente BuscarPorId(long id)
        {
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM clientes WHERE id = @id";
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

        /// <summary>Exclui um cliente pelo Id.</summary>
        public static void Excluir(long id)
        {
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM clientes WHERE id = @id";
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        private static Cliente LerLinha(SQLiteDataReader r)
        {
            return new Cliente
            {
                Id          = r.GetInt64(r.GetOrdinal("id")),
                Tipo        = LerString(r, "tipo"),
                CpfCnpj     = LerString(r, "cpf_cnpj"),
                NomeRazao   = LerString(r, "nome_razao"),
                Sexo        = LerString(r, "sexo"),
                Nascimento  = LerString(r, "nascimento"),
                Cep         = LerString(r, "cep"),
                Endereco    = LerString(r, "endereco"),
                Complemento = LerString(r, "complemento"),
                Bairro      = LerString(r, "bairro"),
                Cidade      = LerString(r, "cidade"),
                Estado      = LerString(r, "estado"),
                Fone1       = LerString(r, "fone1"),
                Fone2       = LerString(r, "fone2"),
                Email1      = LerString(r, "email1"),
                Email2      = LerString(r, "email2"),
                Responsavel = LerString(r, "responsavel"),
                Funcao      = LerString(r, "funcao")
            };
        }

        private static string LerString(SQLiteDataReader r, string coluna)
        {
            int idx = r.GetOrdinal(coluna);
            return r.IsDBNull(idx) ? "" : r.GetString(idx);
        }
    }
}