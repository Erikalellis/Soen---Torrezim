namespace Soen___Torrezim.Models
{
    /// <summary>
    /// Representa um Veículo cadastrado, vinculado a um Cliente (dono).
    /// </summary>
    public class Veiculo
    {
        public long Id { get; set; }
        public long ClienteId { get; set; }
        public string Placa { get; set; }
        public string Marca { get; set; }
        public string Modelo { get; set; }
        public string Cor { get; set; }
        public string Observacoes { get; set; }

        // Auxiliar para exibição (nome do dono) — preenchido pela tela/consulta.
        public string NomeCliente { get; set; }
    }
}