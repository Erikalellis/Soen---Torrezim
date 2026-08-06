namespace Soen___Torrezim.Models
{
    /// <summary>Lançamento do caixa (entrada ou saída).</summary>
    public class LancamentoCaixa
    {
        public long Id { get; set; }
        public string Data { get; set; }
        public string Tipo { get; set; }   // 'entrada' | 'saida'
        public string Descricao { get; set; }
        public double Valor { get; set; }
    }
}