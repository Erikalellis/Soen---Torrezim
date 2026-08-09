# Mapa de Menus e Telas

> Estrutura real do menu principal do sistema SOEN (versão 1.1.x). **Artigo de
> referência** — cada item aponta para a tela real aberta (janela única via `Janelas.Abrir`).

## Estrutura de menus (barra de menus do `FrmPrincipal`)

A ordem apresentada é a desejada:
`Painel Inicial (Dashboard)` · `Pesquisar (Ctrl+F)` · menus de módulos · `Configurações` · `Sair`.

```
SISTEMA SOEN RADIADORES
│
├── 🏠 Painel Inicial (Dashboard)        → DashboardPrincipal
├── 🔍 Pesquisar (Ctrl+F)                → PesquisaGlobal
│
├── 🧾 Controle de Clientes
│   ├── Cadastro de Clientes             → CadastroCliente
│   ├── Consulta de Clientes             → ConsultaCliente
│   ├── Edição de Dados dos Clientes     → ConsultaCliente
│   ├── Exclusão de Clientes             → ConsultaCliente
│   └── Histórico de Compras dos Clientes → HistoricoCompraCliente
│
├── 🚗 Controle de Veículos
│   ├── Cadastro de Veículos             → CadastroVeiculo
│   ├── Manutenções dos Veículos         → ManutecaoVeiculo
│   ├── Histórico de Manutenções         → HistoricoManutencao
│   └── Descrição Detalhada de Cada Veículo → DescricaoVeiculo
│
├── 🤝 Fornecedores e Credores
│   ├── Cadastro de Fornecedores         → CadastroFornecedor
│   ├── Contas a Pagar                   → ContasPagar
│   ├── Contas a Receber                 → ContasReceber
│   └── Relatórios de Fornecedores e Credores → RelatorioFC
│
├── 💵 Vendas, Fluxo de Caixa
│   ├── Registro de Vendas               → RegistroVendas
│   ├── Controle de Caixa                → ControleCaixa
│   ├── Análise de Margem de Lucro       → AnaliseLucro
│   └── Relatórios de Vendas e Financeiros → RelatorioVFi
│
├── 🔧 Serviços e Orçamentos
│   ├── Cadastro de Serviços             → CadastroServico
│   ├── Técnicos                         → Tecnicos
│   ├── Comissões por Técnico            → RelatorioTecnico
│   ├── Tempo Médio de Atendimento por Técnico → RelatorioTempoTecnico
│   ├── Pagamentos de Comissões          → ComissoesPagamentos
│   ├── Agendamento de Serviços          → AgendamentoServicos
│   ├── Criação de Orçamentos            → CriacaoOrcamentos
│   ├── Acompanhamento de Serviços       → AcompanhamentoS
│   ├── Relatórios de Serviços Prestado  → RelatorioSP
│   ├── Calendário de Agendamentos       → CalendariosAgen
│   ├── Gerenciamento de Horários        → GerenciamentoH
│   ├── Notificações de Agendamentos     → NotificaoAgen
│   └── Confirmação de Serviços          → ConfirmacaoS
│
├── 📦 Controle de Estoque
│   ├── Entrada de Estoque               → EntradaEstoque
│   ├── Saída de Estoque                 → SaidaEs
│   └── Inventário de Peças e Materiais  → InventarioPM
│
├── 📊 Outros
│   ├── Relatórios e Análises
│   │   ├── Análise Gerencial            → RelatorioAnalise
│   │   └── Margem por Serviço/Peça      → RelatorioMargem
│   ├── Notificações e Lembretes
│   │   ├── Lembretes de Tarefas         → NotLemb
│   │   └── Alertas de Vencimentos       → ContasPagar
│   └── Integração com Contabilidade (somente admin) → IntegraContabil
│
├── ⚙️ Configurações
│   ├── Dados da Empresa                 → EmpresaConfig
│   ├── Impressora                       → ConfigImpressora
│   ├── Backup e Restauração (somente admin) → BackupRestore
│   ├── Usuários do Sistema (somente admin) → Usuarios
│   ├── Trocar Usuário                   → (relogar via Login)
│   └── Sobre / Informações              → SobreInfo
│
└── ❌ Sair                                  → fecha o FrmPrincipal
```

> O menu **Deep Darkness** original (Soen / Deep Darkness / Erika Lellis / Atualizar é Experimental)
> foi consolidado: créditos ficaram em **Configurações → Sobre / Informações** e
> **Atualizar** é acessível sob demanda. O menu "Outros" agrupa relatórios gerenciais e notificações.

## Permissões por perfil

- **admin:** todos os módulos, incluindo Usuários, Backup e Integração Contábil.
- **operador:** todos os módulos do dia a dia; **Usuários**, **Backup** e **Contábil** ficam desabilitados.

## Telas existentes (todas funcionais ✔)

| Form (arquivo) | Grupo | Função |
|---|---|---|
| Login | — | Autentica usuário (PBKDF2), exibe dados/logo da empresa |
| FrmPrincipal | — | Janela principal: menu, permissões, backup agendado, auto-update |
| DashboardPrincipal | Painel Inicial | Resumo do dia + pendências críticas + faturamento do mês |
| PesquisaGlobal | Pesquisar | Busca global (Ctrl+F); duplo clique abre o registro |
| CadastroCliente | Clientes | Cadastra/edita clientes (CPF/CNPJ, contatos) |
| ConsultaCliente | Clientes | Busca, edita e exclui; abre histórico |
| HistoricoCompraCliente | Clientes | Compras do cliente com total |
| HistoricoCliente | Clientes | Histórico completo em abas (OS, vendas, agendamentos) + imprimir/exportar |
| CadastroVeiculo | Veículos | Cadastra/edita veículo (KM, próxima revisão, garantia, IPVA) |
| ManutecaoVeiculo | Veículos | Registra manutenção com status e histórico |
| HistoricoManutencao | Veículos | Histórico geral (somente leitura) |
| DescricaoVeiculo | Veículos | Ficha completa + alerta de revisão |
| CadastroFornecedor | Fornecedores | Cadastro de fornecedores (tabela clientes, tipo fornecedor) |
| ContasPagar | Financeiro | Contas a pagar com parcelas; pagamento lança no caixa |
| ContasReceber | Financeiro | Contas a receber com parcelas; recebimento lança no caixa |
| RelatorioFC | Relatórios | Fornecedores + Contas a Pagar (CSV/PDF/Excel/Imprimir) |
| RegistroVendas | Vendas | Venda com itens + cliente/veículo; gera entrada no caixa; recibo |
| ControleCaixa | Vendas | Lançamentos manuais, saldo, recibo, fechamento diário |
| AnaliseLucro | Vendas | Margem de lucro por período com gráfico |
| RelatorioVFi | Relatórios | Vendas, Caixa e Contas (CSV/PDF/Excel/Imprimir) |
| CadastroServico | Serviços | Catálogo de serviços |
| AgendamentoServicos | Serviços | Agenda serviço; fluxo agendado→confirmado→concluído; gera OS |
| CriacaoOrcamentos | Serviços | Orçamento/OS com múltiplos itens, técnico e comissão; converte em venda |
| AcompanhamentoS | Serviços | Acompanha andamento dos agendamentos |
| RelatorioSP | Relatórios | Serviços prestados (agendamentos concluídos + OS convertidas) |
| CalendariosAgen | Serviços | Calendário dos agendamentos |
| GerenciamentoH | Serviços | Reagenda horários em lote; gera OS |
| NotificaoAgen | Serviços | Agendamentos de hoje/futuros |
| ConfirmacaoS | Serviços | Confirma agendamentos pendentes |
| NotLemb | Lembretes | Agendamentos de hoje como lembretes |
| Tecnicos | Serviços | CRUD de técnicos (cargo, % comissão, ativo) |
| ComissoesPagamentos | Serviços | Pagamentos de comissões (saldo e histórico) |
| RelatorioTecnico | Relatórios | Comissões por técnico |
| RelatorioTempoTecnico | Relatórios | Tempo médio de atendimento por técnico |
| EntradaEstoque | Estoque | Entrada de produto (soma saldo) |
| SaidaEs | Estoque | Saída de produto (valida saldo) |
| InventarioPM | Estoque | Cadastro de produtos, ajuste, listagem, CSV |
| RelatorioAnalise | Relatórios | Análise gerencial / desempenho |
| RelatorioMargem | Relatórios | Margem por serviço/peça |
| Usuarios | Configuração | CRUD de usuários (perfil admin/operador, senha) |
| EmpresaConfig | Configuração | Dados da empresa + fundo/logo |
| ConfigImpressora | Configuração | Impressora padrão + teste |
| BackupRestore | Configuração | Backup/restore manual + configuração automática |
| SobreInfo | Configuração | Versão, caminho do banco, dados da empresa |
| Atualizar | Configuração | Verifica/baixa/instala atualizações (GitHub) |
| IntegraContabil | Configuração | Exportação contábil CSV (admin) |
| AboutBox1 / DeepDarkness / ErikaLelliscs | Créditos | Créditos do sistema/autores |

> Ao criar um novo módulo, adicione aqui a linha e atualize `PLANO_DESENVOLVIMENTO.md`. É o nosso radar de progresso.