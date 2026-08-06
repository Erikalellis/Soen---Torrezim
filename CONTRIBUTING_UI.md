Padronização de UI - Projeto Soen - Torrezim

Objetivo:
Padronizar a aparência e componentes comuns das janelas (Forms) do projeto.

Principais elementos:
- BaseForm (Common/BaseForm.cs): classe base para Forms que fornece estilo padrão (fonte Segoe UI 9), cor de fundo e um StatusStrip com ToolStripStatusLabel acessível via BaseStatusLabel.
- UIHelpers (Common/UIHelpers.cs): métodos utilitários para criar Labels, Buttons e TextBoxes com estilo padronizado.
- ComboItems (Common/ComboItems.cs): classes auxiliares para popular ComboBoxes (ComboCliente, ComboVeiculo, ComboVeiculoItem, ComboServico, ComboProduto).

Como usar:
- Para criar um novo Form por código, derive de BaseForm em vez de Form.
- Não remova o StatusStrip do BaseForm; use BaseStatusLabel nas classes derivadas para atualizar mensagens de status.
- Evite alterar arquivos *.Designer.cs manualmente. Se o Designer gerar um StatusStrip, mantenha-o — a padronização automática aplicada modificou apenas StatusStrip criados por código.

Exemplo rápido:
public class MinhaTela : BaseForm
{
    public MinhaTela()
    {
        var lbl = UIHelpers.CreateLabel("Nome:", new Point(10,10));
        var txt = UIHelpers.CreateTextBox(new Point(80,10), new Size(200,20));
        Controls.AddRange(new Control[] { lbl, txt });
        SetStatus("Pronto");
    }
}

Notas:
- A padronização atual foi aplicada apenas a Forms criados por código para minimizar risco com arquivos gerados pelo Designer.
- Se quiser que eu aplique padronização aos Forms designer-generated, preciso adaptar InitializeComponent() para preservar compatibilidade com o Visual Studio Designer.
