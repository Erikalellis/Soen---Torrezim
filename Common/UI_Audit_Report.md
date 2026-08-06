Relatório de Auditoria de UI

Resumo:
- Forms totais detectadas: 38
- Forms que já herdam BaseForm: AgendamentoServicos, CriacaoOrcamentos, RegistroVendas, ManutecaoVeiculo, Usuarios
- Uso de UIHelpers: mínimo (Common/UIHelpers.cs está disponível, mas poucas Forms o utilizam)

Achados (amostra de telas e botões detectados):
- AgendamentoServicos.cs: Agendar, Concluir Sel., Cancelar Sel., Excluir Sel.
- CriacaoOrcamentos.cs: Adicionar, Emitir / Salvar, Editar Sel., Aprovar Sel., Converter, Excluir Sel., Visualizar Doc.
- ManutecaoVeiculo.cs: Registrar Manutenção (Salvar), Concluir Selecionada, Excluir Selecionada
- RegistroVendas.cs: Adicionar, Excluir Selecionada, botões de ação do fluxo de vendas
- Usuarios.cs: Salvar, Excluir Sel.
- Várias telas de cadastro (CadastroCliente, CadastroVeiculo, CadastroServico, CadastroFornecedor) possuem botão Salvar (alguns via Designer) e Sair.
- Telas de listagem/consultas possuem botões de Buscar/Editar/Excluir na maior parte dos casos.

Observações gerais:
- Muitos Forms ainda não derivam de BaseForm e não usam UIHelpers; portanto não há padronização de espaçamentos/fonte/StatusStrip nesses casos.
- Detectei presença de botões principais na maioria das telas; porém a verificação automática não garante que os handlers (eventos Click) estejam implementados/funcionais.
- "Padronizar espaçamentos" exige revisão manual no Designer para alinhamentos finos; posso aplicar mudanças automáticas simples (usar UIHelpers para criar botões/labels) mas isso pode requerer ajustes manuais posteriores.

Próximo passo proposto (aguardo sua confirmação):
1) Gerar uma lista priorizada de Forms para padronização (ex.: telas de cadastro e fluxo principal: CadastroCliente, CadastroVeiculo, CriacaoOrcamentos, AgendamentoServicos, Usuarios).
2) Para cada Form aprovada, refatorar criação de controles para usar UIHelpers (mantendo lógica) e garantir herança BaseForm.
3) Validar handlers Click: verificar se métodos referenciados existem e adicionar stubs de feedback (MessageBox) onde faltarem, para tornar botões funcionalmente presentes.

Digite 'OK' para eu gerar a lista priorizada e começar a refatoração nas primeiras N=3 telas, ou responda com outra instrução.
