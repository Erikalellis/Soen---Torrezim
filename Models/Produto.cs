namespace Soen___Torrezim.Models
{
    /// <summary>Produto / peça / material do estoque.</summary>
    public class Produto
    {
        public long Id { get; set; }
        public string Codigo { get; set; }
        public string Nome { get; set; }
        public string Categoria { get; set; }
        public string Unidade { get; set; }
        public double QtdAtual { get; set; }
        public double Custo { get; set; }
        public double Preco { get; set; }
    }
}