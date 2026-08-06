using Soen___Torrezim.Models;

namespace Soen___Torrezim
{
    public class ComboCliente
    {
        public long Id { get; set; }
        public string Nome { get; set; }
        public override string ToString() { return Nome; }
    }

    public class ComboVeiculo
    {
        public Veiculo Veiculo { get; set; }
        public override string ToString()
        {
            var v = Veiculo;
            if (v == null) return "";
            return string.IsNullOrWhiteSpace(v.Modelo) ? v.Placa : v.Placa + " - " + v.Modelo;
        }
    }

    public class ComboVeiculoItem
    {
        public long Id { get; set; }
        public string Rotulo { get; set; }
        public override string ToString() { return Rotulo; }
    }

    public class ComboServico
    {
        public long Id { get; set; }
        public string Nome { get; set; }
        public override string ToString() { return Nome; }
    }

    public class ComboProduto
    {
        public long Id { get; set; }
        public string Nome { get; set; }
        public double Qtd { get; set; }
        public override string ToString() { return Nome; }
    }
}
