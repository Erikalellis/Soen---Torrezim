PR: Padronização de UI e correções iniciais

Resumo

Esta PR aplica padronização inicial da UI para o projeto Soen - Torrezim.

Principais alterações:
- Adiciona Common/BaseForm.cs, Common/UIHelpers.cs, Common/ComboItems.cs.
- Padroniza botões e controles em várias Forms (substituição por UIHelpers onde aplicável).
- Ajustes mínimos em arquivos Designer (CadastroCliente, CadastroVeiculo) para melhorar largura de campos e alinhamento de botões.
- Correções de compilação e pequenas renomeações de propriedades.

Abordagem
- Mudanças aplicadas em lotes, mantendo arquivos Designer intactos quando possível.
- Foram gerados arquivos de relatório em Common/ para facilitar revisão.

Testes
- A solução compila com sucesso após as alterações.
- Recomenda-se revisar no Visual Studio Designer e testar fluxos principais (Cadastro de Cliente/Veículo, Orçamentos, Agendamentos).

Instruções para revisão
1. Revisar alterações em Common/ para avaliar padrões aplicados.
2. Conferir Designer changes em CadastroCliente.Designer.cs e CadastroVeiculo.Designer.cs.
3. Executar a solução e testar telas citadas.

Rollback
- Para reverter localmente: git checkout master; git branch -D feature/ui-standardization
- Ou usar o patch ui_changes.patch gerado na raiz do repo.

Notas
- Recomenda-se squash/merge por conveniência, ou dividir em commit menores caso seja necessário debate por arquivo.
