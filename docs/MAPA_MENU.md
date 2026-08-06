# Mapa de Menus e Telas

> Estrutura reorganizada e coerente do sistema SOEN. Substitui o menu original
> (que tinha muitos itens sobrepostos). **Artigo de referência** — cada item aponta
> para uma tela real.

## Estrutura de menus

```
SISTEMA SOEN RADIADORES
│
├── 🧾 CADASTROS
│   ├── Clientes                → Form Cliente (salvar/consultar/editar/excluir)
│   ├── Veículos                → Form Veículo
│   ├── Fornecedores            → Form Fornecedor
│   └── Serviços (catálogo)     → Form Serviço (nome + preço)
│
├── ⚙️ OPERAÇÃO (dia a dia)
│   ├── Agendamentos
│   ├── Orçamentos
│   ├── Entrada de Estoque
│   ├── Saída de Estoque
│   └── Registro de Vendas
│
├── 💰 FINANCEIRO
│   ├── Contas a Pagar
│   ├── Contas a Receber
│   └── Controle de Caixa
│
├── 📊 CONSULTAS & RELATÓRIOS
│   ├── Histórico de Compras do Cliente
│   ├── Histórico de Manutenções
│   ├── Descrição Detalhada do Veículo
│   ├── Inventário e Estoque
│   ├── Relatórios de Vendas e Financeiro
│   └── Análise de Margem de Lucro
│
├── 🔭 AVANÇADO (fase futura)
│   ├── Notificações e Lembretes
│   ├── Integração com Contabilidade
│   └── Relatórios Personalizados / Estatísticas
│
├── ❌ Sair
└── ℹ️ SOBRE (menu de marca do autor)
    ├── Soen
    ├── Deep Darkness
    ├── Erika Lellis
    └── Atualizar (experimental)
```

## Mapeamento antigo → novo

O menu original tinha ~50 itens com sobreposição. O agrupamento novo elimina
duplicatas:

| Função original | Grupo novo |
|---|---|
| Cadastro de Clientes | CADASTROS → Clientes |
| Consulta, Edição, Exclusão de Clientes | ações *dentro* de CADASTROS → Clientes |
| História de Compras | CONSULTAS → Histórico de Compras |
| Cadastro, Manutenções, Histórico, Descrição de Veículos | CADASTROS → Veículos + CONSULTAS |
| Cadastro de Fornecedores | CADASTROS → Fornecedores |
| Contas a Pagar/Receber | FINANCEIRO |
| Registro de Vendas, Controle de Caixa | OPERAÇÃO + FINANCEIRO |
| Agendamento, Orçamento, Acompanhamento | OPERAÇÃO |
| Entrada/Saída/Inventário de Estoque | OPERAÇÃO + CONSULTAS |
| Notificações, Lembretes, Contabilidade | AVANÇADO |

## Telas existentes vs. planejadas

Marque com ✔️ quando a tela estiver funcional.

| Form (arquivo) | Grupo | Campo(s) originário | Status |
|---|---|---|---|
| CadastroCliente | CADASTROS | ✔️ layout feito | ✔️ funcional |
| ConsultaCliente | CONSULTAS | ✔️ | ✔️ funcional (busca/edita/exclui) |
| CadastroVeiculo | CADASTROS | ✔️ layout feito | ✔️ funcional |
| ManutecaoVeiculo | OPERAÇÃO | ✔️ | ✔️ funcional |
| HistoricoManutencao | CONSULTAS | ✔️ | ✔️ funcional |
| DescricaoVeiculo | CONSULTAS | ✔️ | ✔️ funcional |
| RegistroVendas | OPERAÇÃO | ✔️ | ✔️ funcional |
| ControleCaixa | FINANCEIRO | ✔️ | ✔️ funcional |
| CadastroFornecedor | CADASTROS | ⬜ | ⬜ |
| ContasPagar/ContasReceber | FINANCEIRO | ⬜ | ⬜ |
| RegistroVendas | OPERAÇÃO | ⬜ | ⬜ |
| ControleCaixa | FINANCEIRO | ⬜ | ⬜ |
| AnaliseLucro | CONSULTAS | ⬜ | ⬜ |
| AgendamentoServicos | OPERAÇÃO | ⬜ | ⬜ |
| CriacaoOrcamentos | OPERAÇÃO | ⬜ | ⬜ |
| AcompanhamentoS | OPERAÇÃO | ⬜ | ⬜ |
| Entrada/SaidaEstoque | OPERAÇÃO | ⬜ | ⬜ |
| InventarioPM | CONSULTAS | ⬜ | ⬜ |
| … e demais | | | |

> Quando criar de fato um módulo, atualize esta tabela. É o nosso radar de progresso.