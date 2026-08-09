using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Soen___Torrezim
{
    /// <summary>Verificação e aplicação da atualização via GitHub Releases.</summary>
    public partial class Atualizar : BaseForm
    {
        private Label _lblVersaoAtual;
        private Label _lblSituacao;
        private TextBox _txtNotas;
        private ProgressBar _progress;
        private Button _btnVerificar;
        private Button _btnInstalar;
        private Button _btnFechar;
        private Updater.ReleaseInfo _release;
        private bool _instalando;

        public Atualizar()
        {
            InitializeComponent();
            Text = "Soen - Atualizações";
            ClientSize = new Size(480, 380);
            MinimumSize = new Size(480, 380);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = SystemColors.GradientInactiveCaption;

            var l1 = new Label
            {
                Text = "SOEN - Atualizações",
                Location = new Point(12, 14),
                AutoSize = true,
                Font = new Font("Segoe UI", 12, FontStyle.Bold)
            };

            _lblVersaoAtual = UIHelpers.CreateLabel(
                "Versão atual: " + Application.ProductVersion, new Point(12, 48));

            _lblSituacao = UIHelpers.CreateLabel(
                "Verificando atualizações...", new Point(12, 72));

            var lblNotas = UIHelpers.CreateLabel("Notas da versão:", new Point(12, 96));
            _txtNotas = new TextBox
            {
                Location = new Point(12, 116),
                Size = new Size(456, 150),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 9F),
                BackColor = SystemColors.Window
            };

            _progress = new ProgressBar
            {
                Location = new Point(12, 278),
                Size = new Size(456, 20),
                Style = ProgressBarStyle.Continuous
            };

            _btnVerificar = UIHelpers.CreateButton("Verificar atualizações", new Point(12, 312), new Size(150, 30));
            _btnVerificar.Click += (s, e) => Verificar();

            _btnInstalar = UIHelpers.CreateButton("Baixar e Instalar", new Point(170, 312), new Size(140, 30));
            _btnInstalar.Enabled = false;
            _btnInstalar.Click += async (s, e) => await InstalarAsync();

            _btnFechar = UIHelpers.CreateButton("Fechar", new Point(318, 312), new Size(90, 30));
            _btnFechar.Click += (s, e) => Close();

            Controls.AddRange(new Control[]
            {
                l1, _lblVersaoAtual, _lblSituacao, lblNotas, _txtNotas,
                _progress, _btnVerificar, _btnInstalar, _btnFechar
            });
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Verificar();
        }

        private void Verificar()
        {
            if (_instalando) return;
            SetEstado(false, "Verificando atualizações...");
            _progress.Style = ProgressBarStyle.Marquee;
            VerificarAsync();
        }

        private async void VerificarAsync()
        {
            Updater.ReleaseInfo release = null;
            string erro = null;
            bool ok = await Task.Run(() => Updater.VerificarAtualizacao(out release, out erro));

            if (IsDisposed || !IsHandleCreated) return;

            if (!ok)
            {
                SetLabel(_lblSituacao, erro ?? "Não foi possível verificar atualizações.");
            }
            else if (Updater.TemNovidade(release))
            {
                _release = release;
                SetLabel(_lblSituacao, "Nova versão disponível: " + release.Tag);
                SetNotas(release.Notas);
            }
            else if (release != null)
            {
                SetLabel(_lblSituacao, "Seu sistema está atualizado (versão " + release.Tag + ").");
                SetNotas(release.Notas);
            }

            ProximoEstadoUI();
        }

        private async Task InstalarAsync()
        {
            if (_instalando || _release == null || _release.ZipUrl == null) return;
            _instalando = true;
            SetEstado(false, "Baixando atualização...");
            _progress.Style = ProgressBarStyle.Continuous;

            var progresso = new Progress<int>(p => _progress.Value = p);
            var status = new Progress<string>(s =>
            {
                if (!IsDisposed) SetLabel(_lblSituacao, s);
            });

            try
            {
                string script = await Task.Run(() =>
                    Updater.BaixarEInstalarAsync(_release.ZipUrl, progresso, status));

                if (IsDisposed || !IsHandleCreated) return;
                var r = MessageBox.Show(this,
                    "A atualização será aplicada e o programa será reiniciado.\nDeseja continuar?",
                    "Atualização", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (r != DialogResult.Yes)
                {
                    _instalando = false;
                    ProximoEstadoUI();
                    return;
                }

                SetLabel(_lblSituacao, "Aplicando atualização...");
                Updater.AplicarEEncerrar(script);
            }
            catch (Exception ex)
            {
                _instalando = false;
                MessageBox.Show(this, "Falha ao baixar a atualização:\n" + ex.Message,
                    "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetLabel(_lblSituacao, "Falha ao baixar a atualização.");
                ProximoEstadoUI();
            }
        }

        private void ProximoEstadoUI()
        {
            _progress.Style = ProgressBarStyle.Blocks;
            _progress.Value = 0;
            bool temNova = Updater.TemNovidade(_release);
            _btnVerificar.Enabled = true;
            _btnInstalar.Enabled = temNova && !_instalando;
            _btnFechar.Enabled = true;
        }

        private void SetEstado(bool habilitado, string status)
        {
            _btnVerificar.Enabled = false;
            _btnInstalar.Enabled = false;
            _btnFechar.Enabled = habilitado;
            SetLabel(_lblSituacao, status);
        }

        private void SetLabel(Label lbl, string texto)
        {
            if (lbl == null || IsDisposed || !IsHandleCreated) return;
            if (InvokeRequired) BeginInvoke(new Action(() => lbl.Text = texto));
            else lbl.Text = texto;
        }

        private void SetNotas(string notas)
        {
            if (IsDisposed) return;
            _txtNotas.Text = string.IsNullOrWhiteSpace(notas)
                ? "(sem notas)"
                : notas.Trim();
        }
    }
}