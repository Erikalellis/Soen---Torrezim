using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Soen___Torrezim
{
    /// <summary>
    /// Garante acesso a SoenWebApi (WhatsApp) instalada junto ao aplicativo:
    /// localiza a pasta, verifica se o servico esta online e permite iniciar/parar.
    /// Nao abre o Chromium automaticamente - o usuario abre o painel pelo menu.
    /// </summary>
    public static class SoenWebApiManager
    {
        public const int PortaPadrao = 3000;

        /// <summary>URL raiz do servico (http://localhost:porta).</summary>
        public static string UrlBase
        {
            get { return "http://localhost:" + ObterPorta(); }
        }

        /// <summary>URL do painel administrativo (configura o WhatsApp / QR code).</summary>
        public static string UrlPainel
        {
            get { return UrlBase + "/admin"; }
        }

        /// <summary>URL da documentacao da API.</summary>
        public static string UrlDocs
        {
            get { return UrlBase + "/docs"; }
        }

        /// <summary>Nome do arquivo de inicializacao na pasta da webapi.</summary>
        public static string BatchIniciar
        {
            get { return "IniciarWebApi.bat"; }
        }

        /// <summary>Retorna o caminho da pasta da SoenWebApi, ou null se nao instalada.</summary>
        public static string Localizar()
        {
            string[] candidatos =
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SoenWebApi"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SOEN - Torrezim", "SoenWebApi")
            };
            foreach (string dir in candidatos)
            {
                if (Directory.Exists(dir)) return dir;
            }
            return null;
        }

        /// <summary>Porta configurada da webapi (config.json > .env > 3000).</summary>
        public static int ObterPorta()
        {
            try
            {
                string dir = Localizar();
                if (dir != null)
                {
                    string cfgPath = Path.Combine(dir, "config.json");
                    if (File.Exists(cfgPath))
                    {
                        string json = File.ReadAllText(cfgPath);
                        var m = System.Text.RegularExpressions.Regex.Match(json, "\"port\"\\s*:\\s*(\\d+)");
                        if (m.Success) return int.Parse(m.Groups[1].Value);
                    }
                    string envPath = Path.Combine(dir, ".env");
                    if (File.Exists(envPath))
                    {
                        foreach (var linha in File.ReadAllLines(envPath))
                        {
                            if (linha.TrimStart().StartsWith("PORT="))
                            {
                                int p; if (int.TryParse(linha.Split('=')[1].Trim(), out p)) return p;
                            }
                        }
                    }
                }
            }
            catch { }
            return PortaPadrao;
        }

        /// <summary>Checa de forma sincrona se o servico responde em /ping.</summary>
        public static bool EstahOnline()
        {
            return EstahOnlineAsync().Result;
        }

        /// <summary>Checa se o servico responde em /ping (sem travar a UI).</summary>
        public static async Task<bool> EstahOnlineAsync()
        {
            try
            {
                var req = (HttpWebRequest)WebRequest.Create(UrlBase + "/ping");
                req.Timeout = 2000;
                req.Method = "GET";
                using (var resp = (HttpWebResponse)await req.GetResponseAsync())
                {
                    return resp.StatusCode == HttpStatusCode.OK;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Inicia o servico abrindo o IniciarWebApi.bat. Retorna false se nao instalado.</summary>
        public static bool Iniciar()
        {
            string dir = Localizar();
            if (dir == null) return false;
            string bat = Path.Combine(dir, BatchIniciar);
            if (!File.Exists(bat)) return false;
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = bat,
                    WorkingDirectory = dir,
                    UseShellExecute = true
                };
                Process.Start(psi);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Encerra os processos node.exe e chromium da webapi instalada.</summary>
        public static void Parar()
        {
            string dir = Localizar();
            if (dir == null) return;
            try
            {
                string raizNode = Path.Combine(dir, "node") + Path.DirectorySeparatorChar;
                foreach (var p in Process.GetProcessesByName("node"))
                {
                    try
                    {
                        string path = p.MainModule != null ? p.MainModule.FileName : null;
                        if (path != null && path.StartsWith(raizNode, StringComparison.OrdinalIgnoreCase))
                            p.Kill();
                    }
                    catch { }
                }
                string raizChromium = Path.Combine(dir, "ChromiumPortable") + Path.DirectorySeparatorChar;
                foreach (var p in Process.GetProcessesByName("chrome"))
                {
                    try
                    {
                        string path = p.MainModule != null ? p.MainModule.FileName : null;
                        if (path != null && path.StartsWith(raizChromium, StringComparison.OrdinalIgnoreCase))
                            p.Kill();
                    }
                    catch { }
                }
            }
            catch { }
        }

        /// <summary>Abre o navegador padrao do usuario na URL informada.</summary>
        public static void AbrirNavegador(string url)
        {
            try { Process.Start(url); }
            catch { System.Windows.Forms.MessageBox.Show("Nao foi possivel abrir " + url); }
        }
    }
}