# Plano de Desenvolvimento

Roteiro do sistema SOEN por fases. **Regra de ouro:** só passar para a fase seguinte
depois que a anterior estiver funcionando e atualizada nos documentos.

## Fase 1 — Fundação (concluída ✔)
- [x] Backup inicial no git
- [x] Estrutura de documentação criada
- [x] Mapa de menus reorganizado
- [x] Criar banco SQLite + helper `Database.cs` (cria arquivo e tabelas no 1º uso)
- [x] Criar padrão de modelo + DAO para Cliente
- [x] CadastroCliente salvar de verdade (gravar no banco)
- [x] Consulta de Clientes (listar com DataGridView)
- [x] Edição e exclusão de Clientes
- [x] Atualizar `docs/BANCO_DE_DADOS.md` e `docs/MAPA_MENU.md`

## Fase 2 — Veículos
- [ ] Cadastro de Veículos (vínculo com Cliente)
- [ ] Manutenções e Histórico de Manutenções
- [ ] Descrição detalhada do veículo

## Fase 3 — Vendas e Caixa
- [ ] Registro de Vendas (substitui produto/estoque)
- [ ] Controle de Caixa (venda gera entrada automática)
- [ ] Análise de margem de lucro

## Fase 4 — Estoque
- [ ] Cadastro de produtos/peças
- [ ] Entrada de Estoque
- [ ] Saída de Estoque
- [ ] Inventário

## Fase 5 — Serviços, Orçamentos e Agendamentos
- [ ] Catálogo de Serviços
- [ ] Agendamento de Serviços
- [ ] Criação de Orçamentos
- [ ] Acompanhamento de Serviços

## Fase 6 — Financeiro
- [ ] Contas a Pagar
- [ ] Contas a Receber

## Fase 7 — Relatórios e Análises
- [ ] Relatórios de Vendas e Financeiro
- [ ] Relatórios de Fornecedores e Credores
- [ ] Relatórios de Serviços Prestado
- [ ] Relatórios de Estoque

## Fase 8 — Notificações e Lembretes
- [ ] Lembretes de Tarefas
- [ ] Alertas de Vencimentos
- [ ] Configuração de Notificações

## Fase 9 — Integração com Contabilidade
- [ ] Exportação de dados contábeis
- [ ] Relatórios fiscais

## Fase 10 — Segurança
- [ ] Login de usuários

---

## Critério de "pronto"
Um módulo é considerado pronto quando:
1. Salva/comulta/edita/exclui de verdade no banco;
2. O menu está ligado à tela;
3. `docs/MAPA_MENU.md` e `docs/BANCO_DE_DADOS.md` estão atualizados;
4. Foi feito commit no git.

## Backlog / ideias futuras
- Painel (dashboard) na tela principal
- Backup automático do banco
- Impressão de orçamento/vendas