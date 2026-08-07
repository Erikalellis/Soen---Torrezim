namespace Soen___Torrezim.Models
{
    /// <summary>Dados da empresa (usados em documentos, relatórios e cabeçalho do sistema).</summary>
    public class Empresa
    {
        public long Id { get; set; }
        public string Nome { get; set; }
        public string Cnpj { get; set; }
        public string Telefone { get; set; }
        public string Endereco { get; set; }
        public string Cidade { get; set; }
        public string Estado { get; set; }
        public string Email { get; set; }
        public string Site { get; set; }
        public string Observacoes { get; set; }

        // UI customization
        public string BackgroundImagePath { get; set; }
        public string BackgroundMode { get; set; } // stretch | center | tile
        public string LogoPath { get; set; }
        public int LogoWidth { get; set; }
        public int LogoHeight { get; set; }
    }
}
