namespace Soen___Torrezim.Models
{
    /// <summary>
    /// Conta a pagar ou a receber do módulo financeiro.
    /// "Tipo": 'pagar' | 'receber'. "Status": em_aberto | pago | cancelado.
    /// </summary>
    public class ContaFinanceira
    {
        public long Id { get; set; }
        public string Tipo { get; set; }       // 'pagar' | 'receber'
        public string Descricao { get; set; }
        public string Fornecedor { get; set; }
        public string Vencimento { get; set; }
        public double Valor { get; set; }
        public string Status { get; set; }
        public string CriadoEm { get; set; }
    }
}