namespace Soen___Torrezim.Data
{
    /// <summary>Configuração da impressora padrão do sistema (persistida na tabela config).</summary>
    public static class ImpressoraConfig
    {
        public const string ChavePadrao = "impressora_padrao";

        /// <summary>Nome da impressora padrão configurada (vazio se não definida).</summary>
        public static string Padrao
        {
            get { return ConfigDAO.Obter(ChavePadrao, ""); }
        }

        public static void SalvarPadrao(string nomeImpressora)
        {
            ConfigDAO.Salvar(ChavePadrao, nomeImpressora ?? "");
        }
    }
}