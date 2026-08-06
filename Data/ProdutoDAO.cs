using System.Collections.Generic;
using System.Data.SQLite;
using Soen___Torrezim.Models;

namespace Soen___Torrezim.Data
{
    /// <summary>Acesso a dados de Produtos (cadastro + estoque).</summary>
    public static class ProdutoDAO
    {
        /// <summary>Grava um produto novo ou atualiza um existente (Id &gt; 0). Retorna o Id.</summary>
        public static long Salvar(Produto p)
        {
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                if (p.Id > 0)
                {
                    cmd.CommandText = @"UPDATE produtos SET
    codigo=@codigo, nome=@nome, categoria=@categoria, unidade=@unidade,
    qtd_atual=@qtd, custo=@custo, preco=@preco WHERE id=@id";
                    cmd.Parameters.AddWithValue("@id", p.Id);
                }
                else
                {
                    cmd.CommandText = @"INSERT INTO produtos (codigo, nome, categoria, unidade, qtd_atual, custo, preco)
VALUES (@codigo, @nome, @categoria, @unidade, @qtd, @custo, @preco)";
                }

                cmd.Parameters.AddWithValue("@codigo", Database.Nulo(p.Codigo));
                cmd.Parameters.AddWithValue("@nome", (object)p.Nome ?? "");
                cmd.Parameters.AddWithValue("@categoria", Database.Nulo(p.Categoria));
                cmd.Parameters.AddWithValue("@unidade", Database.Nulo(p.Unidade ?? "un"));
                cmd.Parameters.AddWithValue("@qtd", p.QtdAtual);
                cmd.Parameters.AddWithValue("@custo", p.Custo);
                cmd.Parameters.AddWithValue("@preco", p.Preco);
                cmd.ExecuteNonQuery();

                if (p.Id <= 0) p.Id = (long)conn.LastInsertRowId;
                return p.Id;
            }
        }

        /// <summary>Lista produtos. Se <paramref name="filtro"/> não for vazio, busca por nome/código/categoria.</summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL injection for security vulnerabilities",
            Justification = "O único valor dinâmico é o parâmetro @f (seguro). O SQL é texto estático.")]
        public static List<Produto> Listar(string filtro = "")
        {
            var lista = new List<Produto>();
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM produtos WHERE 1=1";
                if (!string.IsNullOrWhiteSpace(filtro))
                {
                    cmd.CommandText += " AND (nome LIKE @f OR codigo LIKE @f OR categoria LIKE @f)";
                    cmd.Parameters.AddWithValue("@f", "%" + filtro.Trim() + "%");
                }
                cmd.CommandText += " ORDER BY nome";

                using (var leitor = cmd.ExecuteReader())
                {
                    while (leitor.Read()) lista.Add(LerLinha(leitor));
                }
            }
            return lista;
        }

        public static Produto BuscarPorId(long id)
        {
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM produtos WHERE id=@id";
                cmd.Parameters.AddWithValue("@id", id);
                using (var leitor = cmd.ExecuteReader())
                {
                    if (leitor.Read()) return LerLinha(leitor);
                }
            }
            return null;
        }

        /// <summary>Exclui um produto (as movimentações dele são removidas antes).</summary>
        public static void Excluir(long id)
        {
            using (var conn = Database.AbrirConexao())
            using (var tx = conn.BeginTransaction())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = "DELETE FROM movimentacao_estoque WHERE produto_id=@id; DELETE FROM produtos WHERE id=@id;";
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
                tx.Commit();
            }
        }

        /// <summary>
        /// Aplica uma movimentação (entrada aumenta / saída diminui) na quantidade
        /// do produto e grava o registro na tabela de movimentações.
        /// </summary>
        public static void Movimentar(long produtoId, string tipo, double quantidade, string documento)
        {
            using (var conn = Database.AbrirConexao())
            using (var tx = conn.BeginTransaction())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = "INSERT INTO movimentacao_estoque (produto_id, tipo, quantidade, documento) VALUES (@p, @tipo, @qtd, @doc)";
                    cmd.Parameters.AddWithValue("@p", produtoId);
                    cmd.Parameters.AddWithValue("@tipo", tipo);
                    cmd.Parameters.AddWithValue("@qtd", quantidade);
                    cmd.Parameters.AddWithValue("@doc", Database.Nulo(documento));
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    if (tipo == "saida")
                        cmd.CommandText = "UPDATE produtos SET qtd_atual = qtd_atual - @qtd WHERE id=@id";
                    else
                        cmd.CommandText = "UPDATE produtos SET qtd_atual = qtd_atual + @qtd WHERE id=@id";
                    cmd.Parameters.AddWithValue("@qtd", quantidade);
                    cmd.Parameters.AddWithValue("@id", produtoId);
                    cmd.ExecuteNonQuery();
                }
                tx.Commit();
            }
        }

        private static Produto LerLinha(SQLiteDataReader r)
        {
            return new Produto
            {
                Id        = r.GetInt64(r.GetOrdinal("id")),
                Codigo    = LerStr(r, "codigo"),
                Nome      = LerStr(r, "nome"),
                Categoria = LerStr(r, "categoria"),
                Unidade   = LerStr(r, "unidade"),
                QtdAtual  = r.GetDouble(r.GetOrdinal("qtd_atual")),
                Custo     = r.GetDouble(r.GetOrdinal("custo")),
                Preco     = r.GetDouble(r.GetOrdinal("preco"))
            };
        }

        private static string LerStr(SQLiteDataReader r, string col)
        {
            int i = r.GetOrdinal(col);
            return r.IsDBNull(i) ? "" : r.GetString(i);
        }
    }
}