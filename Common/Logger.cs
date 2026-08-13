using System;
using System.IO;

namespace Soen___Torrezim
{
    public static class Logger
    {
        private static readonly object _sync = new object();
        private static readonly string _logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory ?? ".", "logs");
        private static readonly string _logFile = Path.Combine(_logDir, "soen.log");

        /// <summary>Tamanho máximo do log (10 MB) antes de girar para soen_yyyyMMdd.log.</summary>
        private const long MAX_BYTES = 10 * 1024 * 1024;

        public static void LogError(Exception ex)
        {
            try { Escrever("ERROR", ex == null ? "(null)" : ex.ToString()); }
            catch { /* never throw from logger */ }
        }

        public static void LogInfo(string message)
        {
            try { Escrever("INFO", message); }
            catch { }
        }

        private static void Escrever(string nivel, string mensagem)
        {
            lock (_sync)
            {
                if (!Directory.Exists(_logDir)) Directory.CreateDirectory(_logDir);
                RotacionarSeNecessario();
                File.AppendAllText(_logFile,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {nivel}: {mensagem}{Environment.NewLine}");
            }
        }

        /// <summary>
        /// Rotação simples: quando soen.log ultrapassa MAX_BYTES, o arquivo é renomeado
        /// para soen_yyyyMMdd_HHmmss.log (mantém histórico) e um novo soen.log é iniciado.
        /// Mantém no máximo <paramref name="manterHistorico"/> arquivos rotacionados.
        /// </summary>
        private static void RotacionarSeNecessario(int manterHistorico = 8)
        {
            try
            {
                var info = new FileInfo(_logFile);
                if (!info.Exists || info.Length < MAX_BYTES) return;
                string rotacionado = Path.Combine(_logDir,
                    "soen_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log");
                File.Move(_logFile, rotacionado);

                var antigos = Directory.GetFiles(_logDir, "soen_*.log");
                Array.Sort(antigos);
                for (int i = 0; i < antigos.Length - manterHistorico; i++)
                {
                    try { File.Delete(antigos[i]); } catch { }
                }
            }
            catch { /* rotação é best-effort */ }
        }

        /// <summary>Caminho do arquivo de log atual (para exportação/diagnóstico).</summary>
        public static string CaminhoLog
        {
            get { return _logFile; }
        }
    }
}