namespace Soen___Torrezim.Models
{
    /// <summary>Serviço do catálogo (tipo de trabalho feito pela oficina).</summary>
    public class Servico
    {
        public long Id { get; set; }
        public string Nome { get; set; }
        public string Descricao { get; set; }
        public double Preco { get; set; }
    }

    /// <summary>Agendamento de serviço para um cliente/veículo.</summary>
    public class Agendamento
    {
        public long Id { get; set; }
        public long? ClienteId { get; set; }
        public long? VeiculoId { get; set; }
        public long? ServicoId { get; set; }
        public string DataHora { get; set; }
        public string Status { get; set; }   // agendado | confirmado | concluido | cancelado
        public string Observacoes { get; set; }

        // Auxiliares de exibição
        public string NomeCliente { get; set; }
        public string VeiculoPlaca { get; set; }
        public string NomeServico { get; set; }
    }

    /// <summary>Orçamento de serviço para um cliente/veículo.</summary>
    public class Orcamento
    {
        public long Id { get; set; }
        public string Data { get; set; }
        public long? ClienteId { get; set; }
        public long? VeiculoId { get; set; }
        public string Servico { get; set; }
        public double Valor { get; set; }
        public string Status { get; set; } // em_aberto | aprovado | recusado | convertido
        public string Tipo { get; set; }   // orcamento | nota  (Nota de Serviço / OS)
        public string Numero { get; set; } // numeração sequencial das notas de serviço
        public long? TecnicoId { get; set; }
        public double Comissao { get; set; }

        public string NomeCliente { get; set; }
        public string VeiculoPlaca { get; set; }
        public string NomeTecnico { get; set; }
        public System.Collections.Generic.List<OrcamentoItem> Itens { get; set; }
    }

    /// <summary>Item (serviço) de um Orçamento / Nota de Serviço.</summary>
    public class OrcamentoItem
    {
        public long Id { get; set; }
        public long OrcamentoId { get; set; }
        public string Descricao { get; set; }
        public double Quantidade { get; set; } = 1;
        public double ValorUnit { get; set; }
        public long? ProdutoId { get; set; } // peça vinculada ao estoque (baixa automática)

        public double ValorTotal { get { return Quantidade * ValorUnit; } }
    }
}
