namespace Soen___Torrezim.Models
{
    /// <summary>
    /// Representa um Cliente (ou Fornecedor, dependendo de <see cref="Tipo"/>).
    /// Campos em PT-BR, espelhando as colunas da tabela "clientes".
    /// </summary>
    public class Cliente
    {
        public long Id { get; set; }
        public string Tipo { get; set; }          // 'cliente' | 'fornecedor'
        public string CpfCnpj { get; set; }
        public string NomeRazao { get; set; }
        public string Sexo { get; set; }
        public string Nascimento { get; set; }
        public string Cep { get; set; }
        public string Endereco { get; set; }
        public string Complemento { get; set; }
        public string Bairro { get; set; }
        public string Cidade { get; set; }
        public string Estado { get; set; }
        public string Fone1 { get; set; }
        public string Fone2 { get; set; }
        public string Email1 { get; set; }
        public string Email2 { get; set; }
        public string Responsavel { get; set; }
        public string Funcao { get; set; }
    }
}