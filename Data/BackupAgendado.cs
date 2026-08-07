using System;
using System.Globalization;
using System.IO;

namespace Soen___Torrezim.Data
{
    /// <summary>
    /// Backup automático do banco SQLite: copia o arquivo para uma pasta agendada,
    /// aplica política de retenção (mantém as N cópias mais recentes) e controla a
    /// frequência (diária, semanal ou ao sair).
    /// </summary>
    public static class BackupAgendado
    {
        public const string ChaveAutomatico = "backup_automatico";
        public const string ChaveDestino = "backup_destino";
        public const string ChaveFrequencia = "backup_frequencia";
        public const string ChaveReter = "backup_reter";
        public const string ChaveUltimo = "backup_ultimo";

        public const string FrequenciaDiario = "diario";
        public const string FrequenciaSemanal = "semanal";
        public const string FrequenciaSair = "sair";

        private static string DestinoPadrao
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups"); }
        }

        public static bool Automatico
        {
            get { return ConfigDAO.Obter(ChaveAutomatico, "0") == "1"; }
        }

        public static string Frequencia
        {
            get { return ConfigDAO.Obter(ChaveFrequencia, FrequenciaDiario); }
        }

        public static string Destino
        {
            get
            {
                string d = ConfigDAO.Obter(ChaveDestino, "");
                return string.IsNullOrWhiteSpace(d) ? DestinoPadrao : d;
            }
        }

        public static int Reter
        {
            get { return ConfigDAO.ObterInt(ChaveReter, 10); }
        }

        public static string UltimoBackup
        {
            get { return ConfigDAO.Obter(ChaveUltimo, ""); }
        }

        /// <summary>
        /// Verifica se é hora de executar o backup automático segundo a frequência
        /// configurada. <paramref name="aoSair"/> força o disparo da frequência "ao sair".
        /// </summary>
        public static void VerificarAgenda(bool aoSair = false)
        {
            if (!Automatico) return;
            string freq = Frequencia;
            if (aoSair && freq != FrequenciaSair) return;   // encerramento só dispara "ao sair"
            if (!aoSair && freq == FrequenciaSair) return;  // "ao sair" dispara apenas no fechamento

            if (DeveExecutar(freq))
                Executar();
        }

        private static bool DeveExecutar(string freq)
        {
            string ultimo = UltimoBackup;
            if (string.IsNullOrWhiteSpace(ultimo)) return true;
            DateTime dt;
            if (!DateTime.TryParseExact(ultimo, "yyyyMMdd_HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                return false;
            if (freq == FrequenciaDiario)
                return dt.Date != DateTime.Now.Date;
            if (freq == FrequenciaSemanal)
                return dt.AddDays(7) <= DateTime.Now;
            if (freq == FrequenciaSair)
                return true;
            return false;
        }

        /// <summary>Cria uma cópia do banco e aplica a retenção. Retorna o caminho criado (ou null).</summary>
        public static string Executar()
        {
            if (!File.Exists(Database.CaminhoBanco)) return null;
            string dir = Destino;
            try { Directory.CreateDirectory(dir); }
            catch { return null; }

            string arquivo = Path.Combine(dir, "soen_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".db");
            try
            {
                File.Copy(Database.CaminhoBanco, arquivo, true);
            }
            catch
            {
                return null;
            }

            ConfigDAO.Salvar(ChaveUltimo, DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            LimparExcedentes(dir);
            return arquivo;
        }

        /// <summary>Remove cópias antigas mantendo apenas as <see cref="Reter"/> mais recentes.</summary>
        public static void LimparExcedentes(string dir)
        {
            int retor = Reter;
            if (retor <= 0 || !Directory.Exists(dir)) return;
            var antigos = Directory.GetFiles(dir, "soen_*.db");
            Array.Sort(antigos);
            for (int i = 0; i < antigos.Length - retor; i++)
            {
                try { File.Delete(antigos[i]); } catch { }
            }
        }
    }
}