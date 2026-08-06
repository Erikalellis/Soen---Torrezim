namespace Soen___Torrezim.Models
{
    /// <summary>Técnico / mecânico da oficina (executa as Ordens de Serviço).</summary>
    public class Tecnico
    {
        public long Id { get; set; }
        public string Nome { get; set; }
        public string Telefone { get; set; }
        public string Cargo { get; set; }
        public double ComissaoPercent { get; set; }
        public bool Ativo { get; set; } = true;
    }
}