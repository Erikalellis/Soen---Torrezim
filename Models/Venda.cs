using System.Collections.Generic;

namespace Soen___Torrezim.Models
{
    /// <summary>Item de uma venda (um serviço ou produto vendido).</summary>
    public class VendaItem
    {
        public long Id { get; set; }
        public long VendaId { get; set; }
        public string Descricao { get; set; }
        public double Quantidade { get; set; }
        public double ValorUnit { get; set; }
    }

    /// <summary>Uma venda: cabeçalho + uma ou mais linhas (itens).</summary>
    public class Venda
    {
        public long Id { get; set; }
        public string Data { get; set; }
        public long? ClienteId { get; set; }
        public long? VeiculoId { get; set; }
        public double ValorTotal { get; set; }
        public string FormaPagamento { get; set; }
        public string Observacoes { get; set; }

        public List<VendaItem> Itens { get; set; } = new List<VendaItem>();

        // Auxiliares de exibição
        public string NomeCliente { get; set; }
        public string VeiculoDesc { get; set; }
    }
}