Relatório Detalhado de Auditoria de UI

Forms analisadas (38). Abaixo, lista de Forms que já herdam BaseForm:
- AgendamentoServicos.cs
- CriacaoOrcamentos.cs
- ManutecaoVeiculo.cs
- RegistroVendas.cs
- Usuarios.cs
- CadastroCliente.cs (atualizado)
- CadastroVeiculo.cs (atualizado)

Uso UIHelpers: nenhum dos Forms principais ainda usa CreateButton/CreateTextBox; implementação disponível em Common/UIHelpers.cs.

Botões detectados (amostra):
- CadastroCliente: Salvar (button1), Sair (button2)
- CadastroVeiculo: Salvar (button1), Sair (button2)
- CriacaoOrcamentos: Adicionar, Emitir / Salvar, Editar Sel., Aprovar Sel., Converter, Excluir Sel., Visualizar Doc.

Observações:
- As telas de cadastro foram atualizadas para herdar BaseForm sem alterar arquivos Designer.
- Ainda não foram aplicadas mudanças de espaçamentos ou substituição de criação de controles por UIHelpers (requer mais alterações manuais).

Recomendação:
- Próximo passo: para cada Form prioritário, substituir botões criados por código por UIHelpers.CreateButton e garantir que todos os botões principais têm handlers implementados.
- Sugiro iniciar por: CadastroCliente, CadastroVeiculo, CriacaoOrcamentos.

Digite 'APLICAR' para eu refatorar automaticamente os N=3 forms prioritários substituindo criação de botões por UIHelpers e adicionando stubs para handlers ausentes.
