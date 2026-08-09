# Plano de Desenvolvimento

Roteiro do sistema SOEN por fases. **Regra de ouro:** só passar para a fase seguinte
depois que a anterior estiver funcionando e atualizada nos documentos.

## Fase 1 — Fundação (concluída ✔)
- [x] Backup inicial no git
- [x] Estrutura de documentação criada
- [x] Mapa de menus reorganizado
- [x] Banco SQLite + helper `Database.cs` (cria arquivo e tabelas no 1º uso)
- [x] Padrão de modelo + DAO para Cliente
- [x] CadastroCliente salvar de verdade (gravar no banco)
- [x] Consulta de Clientes (listar com DataGridView)
- [x] Edição e exclusão de Clientes
- [x] `docs/BANCO_DE_DADOS.md` e `docs/MAPA_MENU.md` atualizados

## Fase 2 — Veículos (concluída ✔)
- [x] Cadastro de Veículos (com vínculo de Cliente)
- [x] Manutenções e Histórico de Manutenções
- [x] Descrição detalhada do veículo

## Fase 3 — Vendas e Caixa (concluída ✔)
- [x] Registro de Vendas (itens + cliente + veículo)
- [x] Controle de Caixa (venda gera entrada automaticamente + lançamentos manuais + saldo)
- [x] Análise de margem de lucro

## Fase 4 — Estoque (concluída ✔)
- [x] Cadastro de produtos/peças (inventário)
- [x] Entrada de Estoque
- [x] Saída de Estoque (com validação de saldo)
- [x] Inventário
- [x] Alerta de estoque baixo (destaque + aviso no inventário e dashboard)

## Fase 5 — Serviços, Orçamentos e Agendamentos (concluída ✔)
- [x] Catálogo de Serviços
- [x] Agendamento de Serviços (com "Gerar OS" a partir de agendamento concluído)
- [x] Criação de Orçamentos / Notas de Serviço (múltiplos itens, edição, status, impressão)
- [x] Acompanhamento de Serviços

## Fase 6 — Financeiro (concluída ✔)
- [x] Contas a Pagar
- [x] Contas a Receber

## Fase 7 — Relatórios e Análises (concluída ✔)
- [x] Relatórios de Vendas e Financeiro
- [x] Relatórios de Fornecedores e Credores
- [x] Relatórios de Serviços Prestado
- [x] Relatórios de Estoque
- [x] Margem por Serviço/Peça
- [x] Comissões por Técnico

## Fase 8 — Notificações e Lembretes (concluída ✔)
- [x] Lembretes de Tarefas
- [x] Alertas de Vencimentos (contas e revisões de veículos no dashboard)
- [x] Configuração de Notificações

## Fase 9 — Integração com Contabilidade (concluída ✔)
- [x] Exportação de dados contábeis (CSV)
- [x] Relatórios fiscais

## Fase 10 — Segurança (concluída ✔)
- [x] Login de usuários (com usuário admin padrão no 1º uso)
- [x] Senhas com hash PBKDF2 (salt + iterações) — com upgrade automático de hashes antigos
- [x] Perfis `admin` e `operador` com permissões por perfil

## Fase 11 — Módulos de Oficina / Gestão (concluída ✔)
- [x] Técnicos (CRUD, cargo, comissão %)
- [x] Técnico + comissão vinculados à OS
- [x] Conversão de OS aprovada em Venda (gera venda, caixa e baixa de estoque via `produto_id`)
- [x] Quilometragem e próxima revisão programada no veículo
- [x] Painel Inicial (Dashboard): resumo do dia e pendências críticas
- [x] Recibo/Nota de Venda (impressão)
- [x] Recibo de Lançamento de Caixa (impressão)

## Fase 12 — Relatórios gerenciais avançados (concluída ✔)
- [x] Análise Gerencial / Desempenho (indicadores globais)
- [x] Tempo Médio de Atendimento por Técnico
- [x] Pagamentos de Comissões por Técnico (gerado − pago + histórico)
- [x] Exportação CSV/PDF/Excel e impressão de relatórios

## Fase 13 — Ferramentas e automação (concluída ✔)
- [x] Empresa Configurável (dados + imagem de fundo + logo nos documentos)
- [x] Configuração de Impressora padrão (com página de teste)
- [x] Backup manual e restauração
- [x] Backup automático agendado (diário / semanal / ao sair, com retenção de N cópias)
- [x] Pesquisa Global (Ctrl+F) em clientes, veículos, orçamentos/OS e vendas
- [x] Calendário de Agendamentos
- [x] Gerenciamento de Horários (reagendamento em lote)
- [x] Notificações / Lembretes / Confirmação de Serviços
- [x] Histórico completo do Cliente (abas + imprimir/exportar)

## Fase 14 — Distribuição e atualização (concluída ✔)
- [x] Pasta `dist/conteudo` + Zip de distribuição (sem `soen.db`/`Backups`)
- [x] Auto-update via GitHub Releases (`Erikalellis/Soen---Torrezim`) com TLS 1.2
- [x] Script `publicar.ps1` (bump de versão → build → zip → tag → release)
- [x] Instalação portátil local e em rede (Nolt-DDS e Erika-pc/Windows 10)
- [x] Teste de atualização de ponta a ponta (v1.1.0 → v1.1.1)

---
## Critério de "pronto"
Um módulo é considerado pronto quando:
1. Salva/consulta/edita/exclui de verdade no banco;
2. O menu está ligado à tela;
3. `docs/MAPA_MENU.md` e `docs/BANCO_DE_DADOS.md` estão atualizados;
4. Foi feito commit no git;
5. (Fase 14+) Passou por teste em máquina Windows 10 (Erika-pc) antes de ir a produção.

## Backlog / ideias futuras
- [ ] NF-e / NFC-e (emissão fiscal completa com envio à SEFAZ)
- [ ] Orçamento/OS com avaliação por imagem (fotos do veículo)
- [ ] Notificações por WhatsApp/sms de lembrete de revisão
- [ ] Impressão de boleto/carne de contas a pagar/receber
- [ ] Multi-caixa ou abertura/fechamento com usuário responsável
- [ ] Migração para SQL Server/PostgreSQL quando crescer (padrão DAO já facilita)
- [ ] Tela de auditoria de ações por usuário (log de eventos)