Checklist de Ajustes Manuais de UI

Objetivo
- Fornecer um checklist detalhado para ajustes finos no Designer (forms prioritárias) visando padronizar espaçamentos, tamanhos de campos, alinhamento de botões e ancoragens para um visual profissional.

Resumo das alterações já aplicadas
- Adicionados Common/BaseForm.cs, Common/UIHelpers.cs, Common/ComboItems.cs.
- Várias Forms (lote 1) refatoradas para usar UIHelpers.CreateButton onde aplicável.
- CadastroCliente: campos de telefone (textBox13/textBox14) aumentados de largura (100 -> 160).
- CadastroVeiculo: botões Salvar/Sair padronizados (BackColor e tamanho do Sair ajustado).
- Build compilou com sucesso após mudanças.

Forms prioritárias para ajustes manuais (ordem sugerida)
1. CadastroCliente
2. CadastroVeiculo
3. CriacaoOrcamentos
4. Usuarios
5. RegistroVendas

Checklist por Form (passos a aplicar no Designer)
- CadastroCliente
  - Aumentar largura de campos de telefone e e-mail se necessário; verificar AutoSize/Anchor para redimensionamento horizontal.
  - Alinhar verticalmente todos os labels/campos em colunas claras (revisar Location.X consistente por coluna).
  - Posicionar botões Salvar e Sair numa barra inferior direita (consistência com outras forms).
  - Definir Anchor nos elementos principais: campos (Left, Top, Right) para suportar redimensionamento.
  - Verificar TabIndex sequencial.

- CadastroVeiculo
  - Remover imagem de fundo excessiva (pictureBox) ou ajustar SizeMode/Location para não sobrepor controles.
  - Garantir que campos de Marca/Modelo tenham largura adequada (usar same Column grid alignment).
  - Reposicionar botões Salvar/Sair no canto inferior direito; padronizar tamanho 75x23 e BackColor.
  - Definir Anchor/AutoSize para comboBox de Marca/Modelo.

- CriacaoOrcamentos
  - Garantir altura/posicionamento do gridItens; labels e inputs alinhados horizontalmente.
  - Botões de ação (Adicionar, Emitir/Salvar, Editar, etc.) agrupados e com espaçamento uniforme.
  - Verificar fonte do lblTotal (bold) e alinhamento à direita do painel de itens.

- Usuarios
  - Realinhar campos Usuário/Nome/Senha/Perfil; garantir botão Salvar/Excluir visíveis e estado padrão.
  - Validar TabIndex e ancoragem dos campos.

- RegistroVendas
  - Ajustar area de itens e botões de ação (Adicionar, Remover, Salvar, Nova Venda) para evitar sobreposição quando redimensionado.
  - Garantir que grid preencha espaço disponível (Anchor Top/Left/Right/Bottom).

Procedimento recomendado para cada Form no Visual Studio Designer
1. Abrir <FormName>.Designer.cs no Designer.
2. Selecionar cada campo e setar Anchor apropriado (Left/Top e Right quando precisar esticar).
3. Usar propriedades Size/Location para alinhar colunas (definir X consistente por coluna).
4. Agrupar botões de ação no canto inferior direito; definir mesma Size e BackColor SystemColors.AppWorkspace.
5. Validar TabIndex (menu View → Tab Order no Designer) e ajustar se necessário.
6. Salvar Designer e compilar a solução para verificar regressões.

Opções de execução
- Geração de checklist (feito) e você aplica manualmente no Designer — recomendado para controle visual.
- Posso aplicar automaticamente alterações em arquivos *.Designer.cs seguindo este checklist (mais rápido, porém pode requerer ajustes manuais posteriores). Requer confirmação explícita (responda: "AUTORIZAR_DESIGNER_APPLY").

Próximo passo sugerido
- Se quer que eu aplique automaticamente alguns ajustes mínimos adicionais (alinhamento X consistente, anchors e tamanhos de botões) nas forms prioritárias, responda: "AUTORIZAR_DESIGNER_APPLY".
- Se prefere aplicar manualmente, responda: "EU_APLICO_MANUALMENTE" e eu paro aqui e gero o relatório final para revisão.

Notas finais
- Toda alteração automática em arquivos Designer será feita em lotes e revisável (posso gerar um diff/patch antes do commit).
- Recomendo abrir as Forms no Designer após alterações automáticas para validar visual e ajustar detalhes finos.
