using System;
using System.Collections.Generic;
using System.Data.SQLite;
using Soen___Torrezim.Models;

namespace Soen___Torrezim.Data
{
    /// <summary>Acesso a dados de Serviços (catálogo), Agendamentos e Orçamentos.</summary>
    public static class ServicoDAO
    {
        // ===== Serviços (catálogo) =====
        public static long SalvarServico(Servico s)
        {
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                if (s.Id > 0)
                {
                    cmd.CommandText = "UPDATE servicos SET nome=@nome, descricao=@desc, preco=@preco WHERE id=@id";
                    cmd.Parameters.AddWithValue("@id", s.Id);
                }
                else
                {
                    cmd.CommandText = "INSERT INTO servicos (nome, descricao, preco) VALUES (@nome, @desc, @preco)";
                }
                cmd.Parameters.AddWithValue("@nome", (object)s.Nome ?? "");
                cmd.Parameters.AddWithValue("@desc", Database.Nulo(s.Descricao));
                cmd.Parameters.AddWithValue("@preco", s.Preco);
                cmd.ExecuteNonQuery();
                if (s.Id <= 0) s.Id = (long)conn.LastInsertRowId;
                return s.Id;
            }
        }

        public static List<Servico> ListarServicos()
        {
            var lista = new List<Servico>();
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM servicos ORDER BY nome";
                using (var leitor = cmd.ExecuteReader())
                {
                    while (leitor.Read())
                    {
                        lista.Add(new Servico
                        {
                            Id = leitor.GetInt64(leitor.GetOrdinal("id")),
                            Nome = LerStr(leitor, "nome"),
                            Descricao = LerStr(leitor, "descricao"),
                            Preco = leitor.GetDouble(leitor.GetOrdinal("preco"))
                        });
                    }
                }
            }
            return lista;
        }

        public static void ExcluirServico(long id)
        {
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM servicos WHERE id=@id";
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        // ===== Agendamentos =====
        public static long SalvarAgendamento(Agendamento a)
        {
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                if (a.Id > 0)
                {
                    cmd.CommandText = @"UPDATE agendamentos SET cliente_id=@cli, veiculo_id=@vei,
servico_id=@serv, data_hora=@dh, status=@status, observacoes=@obs WHERE id=@id";
                    cmd.Parameters.AddWithValue("@id", a.Id);
                }
                else
                {
                    cmd.CommandText = @"INSERT INTO agendamentos (cliente_id, veiculo_id, servico_id, data_hora, status, observacoes)
VALUES (@cli, @vei, @serv, @dh, @status, @obs)";
                }
                cmd.Parameters.AddWithValue("@cliente", (object)a.ClienteId ?? System.DBNull.Value);
                cmd.Parameters.AddWithValue("@vei", (object)a.VeiculoId ?? System.DBNull.Value);
                cmd.Parameters.AddWithValue("@serv", (object)a.ServicoId ?? System.DBNull.Value);
                cmd.Parameters.AddWithValue("@dh", Database.Nulo(a.DataHora));
                cmd.Parameters.AddWithValue("@status", Database.Nulo(a.Status ?? "agendado"));
                cmd.Parameters.AddWithValue("@obs", Database.Nulo(a.Observacoes));
                cmd.ExecuteNonQuery();
                if (a.Id <= 0) a.Id = (long)conn.LastInsertRowId;
                return a.Id;
            }
        }

        public static List<Agendamento> ListarAgendamentos()
        {
            var lista = new List<Agendamento>();
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT a.*, c.nome_razao AS nome_cliente, v.placa AS veiculo_placa, s.nome AS nome_servico
FROM agendamentos a
LEFT JOIN clientes c ON c.id=a.cliente_id
LEFT JOIN veiculos v ON v.id=a.veiculo_id
LEFT JOIN servicos s ON s.id=a.servico_id
ORDER BY a.data_hora DESC, a.id DESC";
                using (var leitor = cmd.ExecuteReader())
                {
                    while (leitor.Read()) lista.Add(LerAgendamento(leitor));
                }
            }
            return lista;
        }

        public static void ExcluirAgendamento(long id)
        {
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM agendamentos WHERE id=@id";
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        private static Agendamento LerAgendamento(SQLiteDataReader leitor)
        {
            var a = new Agendamento
            {
                Id = leitor.GetInt64(leitor.GetOrdinal("id")),
                DataHora = LerStr(leitor, "data_hora"),
                Status = LerStr(leitor, "status"),
                Observacoes = LerStr(leitor, "observacoes"),
                NomeCliente = LerStr(leitor, "nome_cliente"),
                VeiculoPlaca = LerStr(leitor, "veiculo_placa"),
                NomeServico = LerStr(leitor, "nome_servico")
            };
            a.ClienteId = LerLongNullable(leitor, "cliente_id");
            a.VeiculoId = LerLongNullable(leitor, "veiculo_id");
            a.ServicoId = LerLongNullable(leitor, "servico_id");
            return a;
        }

        // ===== Orçamentos / Notas de Serviço =====
        public static long SalvarOrcamento(Orcamento o)
        {
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                if (string.IsNullOrWhiteSpace(o.Tipo)) o.Tipo = "orcamento";
                if (o.Tipo == "nota" && string.IsNullOrWhiteSpace(o.Numero))
                    o.Numero = ProximoNumeroNota(conn);

                if (o.Id > 0)
                {
                    cmd.CommandText = @"UPDATE orcamentos SET data=@data, cliente_id=@cli, veiculo_id=@vei,
servico=@serv, valor=@valor, status=@status, tipo=@tipo, numero=@numero,
tecnico_id=@tec, comissao=@com WHERE id=@id";
                    cmd.Parameters.AddWithValue("@id", o.Id);
                }
                else
                {
                    cmd.CommandText = @"INSERT INTO orcamentos (data, cliente_id, veiculo_id, servico, valor, status, tipo, numero, tecnico_id, comissao)
VALUES (@data, @cli, @vei, @serv, @valor, @status, @tipo, @numero, @tec, @com)";
                }
                cmd.Parameters.AddWithValue("@data", Database.Nulo(o.Data));
                cmd.Parameters.AddWithValue("@cli", (object)o.ClienteId ?? System.DBNull.Value);
                cmd.Parameters.AddWithValue("@vei", (object)o.VeiculoId ?? System.DBNull.Value);
                cmd.Parameters.AddWithValue("@serv", Database.Nulo(o.Servico));
                cmd.Parameters.AddWithValue("@valor", o.Valor);
                cmd.Parameters.AddWithValue("@status", Database.Nulo(o.Status ?? "em_aberto"));
                cmd.Parameters.AddWithValue("@tipo", Database.Nulo(o.Tipo));
                cmd.Parameters.AddWithValue("@numero", Database.Nulo(o.Numero));
                cmd.Parameters.AddWithValue("@tec", (object)o.TecnicoId ?? System.DBNull.Value);
                cmd.Parameters.AddWithValue("@com", o.Comissao);
                cmd.ExecuteNonQuery();
                if (o.Id <= 0) o.Id = (long)conn.LastInsertRowId;
                return o.Id;
            }
        }

        /// <summary>Gera o próximo número sequencial de Nota de Serviço (ex.: OS-0001).</summary>
        private static string ProximoNumeroNota(SQLiteConnection conn)
        {
            long n = 0;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM orcamentos WHERE tipo='nota'";
                n = (long)cmd.ExecuteScalar();
            }
            return "OS-" + (n + 1).ToString("0000");
        }

        public static List<Orcamento> ListarOrcamentos()
        {
            var lista = new List<Orcamento>();
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT o.*, c.nome_razao AS nome_cliente, v.placa AS veiculo_placa, t.nome AS nome_tecnico
FROM orcamentos o
LEFT JOIN clientes c ON c.id=o.cliente_id
LEFT JOIN veiculos v ON v.id=o.veiculo_id
LEFT JOIN tecnicos t ON t.id=o.tecnico_id
ORDER BY o.id DESC";
                using (var leitor = cmd.ExecuteReader())
                {
                    while (leitor.Read())
                    {
                        lista.Add(new Orcamento
                        {
                            Id = leitor.GetInt64(leitor.GetOrdinal("id")),
                            Data = LerStr(leitor, "data"),
                            ClienteId = LerLongNullable(leitor, "cliente_id"),
                            VeiculoId = LerLongNullable(leitor, "veiculo_id"),
                            TecnicoId = LerLongNullable(leitor, "tecnico_id"),
                            Comissao = LerDouble(leitor, "comissao"),
                            Servico = LerStr(leitor, "servico"),
                            Valor = leitor.GetDouble(leitor.GetOrdinal("valor")),
                            Status = LerStr(leitor, "status"),
                            Tipo = LerStr(leitor, "tipo"),
                            Numero = LerStr(leitor, "numero"),
                            NomeCliente = LerStr(leitor, "nome_cliente"),
                            VeiculoPlaca = LerStr(leitor, "veiculo_placa"),
                            NomeTecnico = LerStr(leitor, "nome_tecnico")
                        });
                    }
                }
            }
            return lista;
        }

        public static void ExcluirOrcamento(long id)
        {
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM orcamento_itens WHERE orcamento_id=@id; DELETE FROM orcamentos WHERE id=@id";
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        // ===== Itens do Orçamento / Nota de Serviço =====

        /// <summary>Substitui a lista de itens de um orçamento (remove os antigos e insere os novos).</summary>
        public static void SalvarItensOrcamento(long orcamentoId, List<OrcamentoItem> itens)
        {
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM orcamento_itens WHERE orcamento_id=@o";
                cmd.Parameters.AddWithValue("@o", orcamentoId);
                cmd.ExecuteNonQuery();
            }
            if (itens == null) return;
            foreach (var it in itens)
            {
                if (string.IsNullOrWhiteSpace(it.Descricao) && it.Quantidade <= 0 && it.ValorUnit <= 0) continue;
                using (var conn = Database.AbrirConexao())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO orcamento_itens (orcamento_id, descricao, quantidade, valor_unit, produto_id)
VALUES (@o, @d, @q, @vu, @p)";
                    cmd.Parameters.AddWithValue("@o", orcamentoId);
                    cmd.Parameters.AddWithValue("@d", Database.Nulo(it.Descricao));
                    cmd.Parameters.AddWithValue("@q", it.Quantidade);
                    cmd.Parameters.AddWithValue("@vu", it.ValorUnit);
                    cmd.Parameters.AddWithValue("@p", (object)it.ProdutoId ?? System.DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static List<OrcamentoItem> ListarItensOrcamento(long orcamentoId)
        {
            var lista = new List<OrcamentoItem>();
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM orcamento_itens WHERE orcamento_id=@o ORDER BY id";
                cmd.Parameters.AddWithValue("@o", orcamentoId);
                using (var leitor = cmd.ExecuteReader())
                {
                    while (leitor.Read())
                    {
                        lista.Add(new OrcamentoItem
                        {
                            Id = leitor.GetInt64(leitor.GetOrdinal("id")),
                            OrcamentoId = orcamentoId,
                            Descricao = LerStr(leitor, "descricao"),
                            Quantidade = leitor.GetDouble(leitor.GetOrdinal("quantidade")),
                            ValorUnit = leitor.GetDouble(leitor.GetOrdinal("valor_unit")),
                            ProdutoId = LerLongNullable(leitor, "produto_id")
                        });
                    }
                }
            }
            return lista;
        }

        /// <summary>
        /// Converte uma OS/Orçamento aprovado em venda. Gera a venda com seus
        /// itens, lança a entrada no caixa, dá baixa no estoque dos itens que
        /// possuem produto vinculado e marca o status como "convertido".
        /// </summary>
        public static void ConverterEmVenda(long orcamentoId)
        {
            // Carrega o orçamento
            List<Orcamento> todos = ListarOrcamentos();
            Orcamento o = todos.Find(x => x.Id == orcamentoId);
            if (o == null) return;

            List<OrcamentoItem> itens = ListarItensOrcamento(orcamentoId);
            if (itens.Count == 0) return;

            // Valida o saldo de estoque ANTES de criar a venda (evita venda sem baixa).
            foreach (var it in itens)
            {
                if (!it.ProdutoId.HasValue || it.Quantidade <= 0) continue;
                Produto p = ProdutoDAO.BuscarPorId(it.ProdutoId.Value);
                if (p == null)
                    throw new InvalidOperationException("Peça vinculada não encontrada no estoque: " + it.Descricao);
                if (p.QtdAtual < it.Quantidade)
                    throw new InvalidOperationException("Estoque insuficiente para " + it.Descricao +
                        " (disponível: " + p.QtdAtual.ToString("0.##", System.Globalization.CultureInfo.GetCultureInfo("pt-BR")) + ").");
            }

            var venda = new Venda
            {
                Data = o.Data,
                ClienteId = o.ClienteId,
                VeiculoId = o.VeiculoId,
                ValorTotal = o.Valor,
                Observacoes = "Gerada da OS " + (string.IsNullOrWhiteSpace(o.Numero) ? "#" + o.Id : o.Numero)
            };
            foreach (var it in itens)
                venda.Itens.Add(new VendaItem
                {
                    Descricao = it.Descricao,
                    Quantidade = it.Quantidade,
                    ValorUnit = it.ValorUnit
                });

            // Venda + itens + caixa (transação própria)
            long idVenda = VendaDAO.SalvarComCaixa(venda);

            // Baixa de estoque de peças vinculadas
            string doc = "OS " + (string.IsNullOrWhiteSpace(o.Numero) ? "#" + o.Id : o.Numero);
            foreach (var it in itens)
            {
                if (it.ProdutoId.HasValue && it.Quantidade > 0)
                    ProdutoDAO.Movimentar(it.ProdutoId.Value, "saida", it.Quantidade, doc);
            }

            // Marca a OS como convertida
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "UPDATE orcamentos SET status='convertido' WHERE id=@id";
                cmd.Parameters.AddWithValue("@id", orcamentoId);
                cmd.ExecuteNonQuery();
            }
        }

        private static long? LerLongNullable(SQLiteDataReader leitor, string col)
        {
            int i = leitor.GetOrdinal(col);
            return leitor.IsDBNull(i) ? (long?)null : leitor.GetInt64(i);
        }

        private static string LerStr(SQLiteDataReader r, string col)
        {
            int i = r.GetOrdinal(col);
            return r.IsDBNull(i) ? "" : r.GetString(i);
        }

        private static double LerDouble(SQLiteDataReader r, string col)
        {
            int i = r.GetOrdinal(col);
            return r.IsDBNull(i) ? 0d : r.GetDouble(i);
        }
    }
}