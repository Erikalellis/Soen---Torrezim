using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace Soen___Torrezim
{
    /// <summary>
    /// Gerenciador de atualizações via GitHub Releases.
    /// Consulta a release mais recente do repositório, compara com a versão local
    /// e aplica a nova build preservando o banco de dados (soen.db) e os backups.
    /// </summary>
    public static class Updater
    {
        public const string RepoDono = "Erikalellis";
        public const string RepoNome = "Soen---Torrezim";
        public const string NomeExe = "Soen - Torrezim.exe";

        private const string UrlReleasesApi =
            "https://api.github.com/repos/" + RepoDono + "/" + RepoNome + "/releases/latest";

        /// <summary>Versão da aplicação em execução (AssemblyFileVersion).</summary>
        public static Version VersaoAtual
        {
            get
            {
                Version v = Assembly.GetExecutingAssembly().GetName().Version;
                return v ?? new Version(0, 0, 0, 0);
            }
        }

        /// <summary>Pasta onde o executável está instalado (banco vive aqui).</summary>
        public static string PastaApp
        {
            get { return AppDomain.CurrentDomain.BaseDirectory; }
        }

        public class ReleaseInfo
        {
            public string Tag;
            public Version Versao;
            public string ZipUrl;
            public string Notas;
        }

        /// <summary>
        /// Consulta a última release publicada no GitHub.
        /// Retorna false + mensagem em <paramref name="erro"/> se não conseguir consultar
        /// (sem rede, sem release publicada, resposta inválida etc.).
        /// </summary>
        public static bool VerificarAtualizacao(out ReleaseInfo release, out string erro)
        {
            release = null;
            erro = null;
            try
            {
                GarantirTls12();
                using (var wc = new WebClient())
                {
                    wc.Encoding = Encoding.UTF8;
                    wc.Headers.Add("User-Agent", "Soen-Atualizador");
                    string json = wc.DownloadString(UrlReleasesApi);

                    var ser = new JavaScriptSerializer();
                    var dict = ser.DeserializeObject(json) as Dictionary<string, object>;
                    if (dict == null)
                    {
                        erro = "Resposta inválida do servidor.";
                        return false;
                    }

                    string tag = dict.ContainsKey("tag_name") ? dict["tag_name"] as string : null;
                    if (string.IsNullOrWhiteSpace(tag))
                    {
                        erro = "Release sem versão identificada.";
                        return false;
                    }

                    Version v = ParseVersaoTag(tag);
                    if (v == null)
                    {
                        erro = "Versão da release não reconhecida: " + tag;
                        return false;
                    }

                    string notas = dict.ContainsKey("body") ? dict["body"] as string : null;

                    string zipUrl = null;
                    if (dict.ContainsKey("assets") && dict["assets"] is object[])
                    {
                        foreach (object a in (object[])dict["assets"])
                        {
                            var asset = a as Dictionary<string, object>;
                            if (asset == null) continue;
                            string nome = asset.ContainsKey("name") ? asset["name"] as string : "";
                            if (nome != null && nome.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                            {
                                zipUrl = asset.ContainsKey("browser_download_url") ? asset["browser_download_url"] as string : null;
                                break;
                            }
                        }
                    }

                    release = new ReleaseInfo { Tag = tag, Versao = v, ZipUrl = zipUrl, Notas = notas };
                    return true;
                }
            }
            catch (WebException)
            {
                erro = "Nenhuma versão publicada ainda ou conexão indisponível.";
                return false;
            }
            catch (Exception ex)
            {
                erro = ex.Message;
                return false;
            }
        }

        /// <summary>true se a release informada for mais nova do que a versão instalada.</summary>
        public static bool TemNovidade(ReleaseInfo r)
        {
            return r != null && r.ZipUrl != null && r.Versao > VersaoAtual;
        }

        /// <summary>
        /// Baixa o zip da release, extrai em pasta temporária e gera o script que
        /// aplica os arquivos na pasta da aplicação (preservando soen.db/Backups) e reinicia.
        /// </summary>
        public static async Task<string> BaixarEInstalarAsync(string zipUrl, IProgress<int> progresso, IProgress<string> status)
        {
            string pastaTemp = Path.Combine(Path.GetTempPath(), "SoenUpdate_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(pastaTemp);

            string zipLocal = Path.Combine(pastaTemp, "atualizacao.zip");
            string extraido = Path.Combine(pastaTemp, "conteudo");
            string script = Path.Combine(pastaTemp, "aplicar_atualizacao.bat");

            await BaixarArquivoAsync(new Uri(zipUrl), zipLocal, progresso).ConfigureAwait(false);
            if (status != null) status.Report("Extraindo arquivos...");
            ZipFile.ExtractToDirectory(zipLocal, extraido);

            if (status != null) status.Report("Preparando instalação...");
            EscreverScript(script, extraido, zipLocal, pastaTemp);
            return script;
        }

        /// <summary>Dispara o script (hidden) e fecha a aplicação para aplicar a atualização.</summary>
        public static void AplicarEEncerrar(string scriptPath)
        {
            var psi = new ProcessStartInfo
            {
                FileName = scriptPath,
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            Process.Start(psi);
            Application.Exit();
        }

        private static Task BaixarArquivoAsync(Uri url, string destino, IProgress<int> progresso)
        {
            GarantirTls12();
            var tcs = new TaskCompletionSource<bool>();
            var wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            wc.Headers.Add("User-Agent", "Soen-Atualizador");
            wc.DownloadProgressChanged += (s, e) =>
            {
                if (progresso != null) progresso.Report(e.ProgressPercentage);
            };
            wc.DownloadFileCompleted += (s, e) =>
            {
                wc.Dispose();
                if (e.Cancelled) tcs.TrySetCanceled();
                else if (e.Error != null) tcs.TrySetException(e.Error);
                else tcs.TrySetResult(true);
            };
            wc.DownloadFileAsync(url, destino);
            return tcs.Task;
        }

        private static void EscreverScript(string script, string extraido, string zipLocal, string pastaTemp)
        {
            string appDir = PastaApp;
            string exeCompleto = Path.Combine(appDir, NomeExe);

            var sb = new StringBuilder();
            sb.AppendLine("@echo off");
            sb.AppendLine("timeout /t 3 /nobreak >nul");
            sb.AppendLine("taskkill /f /im \"" + NomeExe + "\" >nul 2>&1");
            sb.AppendLine("timeout /t 1 /nobreak >nul");
            sb.AppendLine("xcopy /y /e /i /q \"" + extraido + "\" \"" + appDir + "\" >nul");
            sb.AppendLine("rmdir /s /q \"" + extraido + "\"");
            sb.AppendLine("rmdir /s /q \"" + pastaTemp + "\"");
            sb.AppendLine("start \"\" \"" + exeCompleto + "\"");
            sb.AppendLine("del /f /q \"%~f0\" >nul 2>&1");
            sb.AppendLine("exit");
            File.WriteAllText(script, sb.ToString(), Encoding.Default);
        }

        private static void GarantirTls12()
        {
            try
            {
                if (ServicePointManager.SecurityProtocol.HasFlag(SecurityProtocolType.Tls12)) return;
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            }
            catch
            {
                // irrelevante: sem TLS 1.2 não será possível baixar dos servidores atuais
            }
        }

        public static Version ParseVersaoTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return null;
            string t = tag;
            while (t.Length > 0 && (t[0] == 'v' || t[0] == 'V')) t = t.Substring(1);
            Version v;
            if (Version.TryParse(t, out v)) return v;
            string[] p = t.Split('.');
            int a, b = 0, c = 0, d = 0;
            if (p.Length >= 1 && int.TryParse(p[0], out a))
            {
                if (p.Length > 1) int.TryParse(p[1], out b);
                if (p.Length > 2) int.TryParse(p[2], out c);
                if (p.Length > 3) int.TryParse(p[3], out d);
                return new Version(a, b, c, d);
            }
            return null;
        }
    }
}