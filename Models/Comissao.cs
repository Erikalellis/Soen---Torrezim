namespace Soen___Torrezim.Models
{
    /// <summary>Resumo de comissões por técnico (gerado vs. pago vs. saldo).</summary>
    public class ComissaoResumo
    {
        public long TecnicoId { get; set; }
        public string NomeTecnico { get; set; }
        public double Gerado { get; set; }
        public double Pago { get; set; }
        public double Saldo { get { return Gerado - Pago; } }
    }

    /// <summary>Registro individual de pagamento de comissão.</summary>
    public class ComissaoPagamento
    {
        public long Id { get; set; }
        public long TecnicoId { get; set; }
        public string NomeTecnico { get; set; }
        public double Valor { get; set; }
        public string Data { get; set; }
        public string Observacao { get; set; }
    }
}