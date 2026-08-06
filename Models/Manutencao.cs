namespace Soen___Torrezim.Models
{
    /// <summary>
    /// Representa uma manutenção realizada em um veículo de um cliente.
    /// </summary>
    public class Manutencao
    {
        public long Id { get; set; }
        public long VeiculoId { get; set; }
        public long? ClienteId { get; set; }
        public string Data { get; set; }     // ISO yyyy-MM-dd
        public string Descricao { get; set; }
        public double Valor { get; set; }
        public string Status { get; set; }   // 'aberta' | 'concluida' | 'cancelada'

        // Auxiliares de exibição
        public string Placa { get; set; }
        public string VeiculoDesc { get; set; }
        public string NomeCliente { get; set; }
    }
}