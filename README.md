# SOEN Radiadores Torrezim

Sistema de gestão para oficina de radiadores (SOEN). Desenvolvido em **C# WinForms (.NET Framework 4.7.2)** com banco **SQLite** local.

> Primeiro projeto desenvolvido do zero pelo autor. Documentação completa na pasta `docs/` — consultar antes de mudar o código.

## Status

- ✅ **Sistema completo em produção** (versão 1.1.3)
- ✅ 45+ telas funcionais: cadastros, operação, financeiro, estoque, relatórios, segurança
- ✅ Auto-atualização via GitHub Releases
- ✅ Robustez: backup antes de migrações, verificação de integridade do banco com restauração automática, arquivamento de documentos em PDF
- ✅ Instalado em 2 máquinas (Nolt-DDS / Windows 11 e Erika-pc / Windows 10 — máquina de teste)
- 📦 **Pacote completo para pendrive** com a SoenWebApi (WhatsApp) embutida — Node.js e Chromium portáteis, sem instalar nada no PC
- 🧪 Novas funcionalidades entram por testes no Windows 10 antes de irem para produção

## Funcionalidades

| Área | Módulos |
|---|---|
| **Cadastros** | Clientes (CPF/CNPJ), Fornecedores, Veículos (KM, garantia, IPVA, licenciamento), Serviços, Técnicos |
| **Operação** | Agendamentos, Orçamentos e Notas de Serviço (múltiplos itens, técnico, comissão), Vendas, Acompanhamento |
| **Estoque** | Produtos, Entrada/Saída, Inventário, alerta de estoque baixo |
| **Financeiro** | Controle de Caixa (fechamento diário + recibo), Contas a Pagar/Receber (com parcelas), Análise de Lucro |
| **Relatórios** | Vendas/Financeiro, Fornecedores/Credores, Serviços Prestados, Comissões, Tempo Médio de Atendimento, Margem, Análise Gerencial (CSV/PDF/Excel/Imprimir) |
| **Segurança** | Login com senha PBKDF2, perfis **admin** e **operador**, permissões por perfil |
| **Recursos** | Backup automático agendado, Restauração, Exportação contábil (CSV), Pesquisa Global (Ctrl+F), Impressão de recibos/OS, Calendário de agendamentos, Dashboard |
| **Robustez** | Backup automático antes de migrações, verificação de integridade do banco (PRAGMA) com restauração do último backup, arquivamento automático de documentos em PDF (`recibos\AAAA\MM\`) |
| **WhatsApp (SoenWebApi)** | Controle pelo menu **SoenWebApi (WhatsApp)** do app: Iniciar/Parar/Status/Abrir Painel (`/admin`) e Documentação (`/docs`). Serviço local embutido (Node.js + Chromium portáteis), sem abrir Chromium automaticamente |

## Estrutura de pastas

```
Docs:        docs/                 # Documentação do projeto (LEIA ANTES DE MEXER)
Código:
├── Program.cs                     # Entrada: inicia banco, admin padrão, abre Login
├── FrmPrincipal.cs                # Janela principal (menu, permissões, backup, auto-update)
├── Login.cs                       # Autenticação de usuário
├── DashboardPrincipal.cs          # Painel inicial (resumo do dia + pendências)
├── *.cs                           # Demais telas (uma por formulário)
├── Common/                        # Updater, Sessão, Exportação, Logger, BaseForm, Janelas
├── Data/                          # Database.cs + DAOs + BackupAgendado
├── Models/                        # Classes de entidade (Cliente, Venda, Caixa, ...)
├── Properties/                    # AssemblyInfo, Resources, Settings
├── docs/                          # Mapa de menus, plano, banco, arquitetura
└── dist/                          # (fora do repo) ZIPs de release + conteudo de instalação
```

## Como rodar (local / desenvolvimento)

1. Abra `Soen - Torrezim.sln` no Visual Studio (2017+).
2. Build + Run (F5).

O banco de dados `soen.db` é criado automaticamente na pasta do executável no primeiro uso (SQLite, sem servidor).

### Build por linha de comando

```powershell
dotnet build "Soen - Torrezim.sln" -c Release
```

## Primeiro acesso (login padrão)

No primeiro uso o sistema cria o usuário padrão:

- **Usuário:** `admin`
- **Senha:** `admin`
- **Perfil:** administrador

> Troque a senha após o primeiro acesso (menu Configurações → Usuários do Sistema).

## Como publicar uma nova versão

O script `publicar.ps1` automatiza: bump de versão → build Release → empacotar (sem `soen.db`/`Backups`) → `dist\conteudo` → tag git → Release no GitHub (`Erikalellis/Soen---Torrezim`).

```powershell
.\publicar.ps1 -Versao 1.2.0            # publica oficial
.\publicar.ps1 -Draft -Notas "teste"     # rascunho (não vai para os usuários)
.\publicar.ps1 -SemGit                   # só build + zip, sem tag/release
```

Requisitos: [GitHub CLI](https://cli.github.com/) autenticado (`gh auth login`).

> **Estratégia de distribuição:** as Releases do GitHub ficam **leves** (~4 MB — só o app) para o auto-update. O pacote **completo** (app + SoenWebApi com Node/Chromium, ~740 MB) é distribuído por **pendrive** (`dist\pendrive\`), para instalação e quando a WebApi precisar de atualização. Atualizações grandes não precisam passar pelo GitHub.

## Atualização automática

O app verifica, ao abrir (e sob demanda), a última Release do repositório GitHub. Se houver versão mais nova:

1. Mostra aviso automático (ou menu sobre → Atualizar).
2. Baixa o ZIP da Release, extrai em pasta temporária.
3. Substitui os binários **preservando `soen.db` e a pasta `Backups`**.
4. Reinicia o aplicativo.

## Instalação

### Opção 1 — Pacote para pendrive (recomendado para novos clientes)

O pacote completo fica em `dist\pendrive\SOEN - TORREZIM Pendrive\` (~740 MB) e contém tudo, embutido:

| Item | Descrição |
|---|---|
| `Aplicativo\` | Binários do SOEN (exe + DLLs + SQLite x64/x86) |
| `SoenWebApi\` | Serviço WhatsApp completo: Node.js portátil, Chromium, node_modules e código |
| `Instalar.bat` | Instalador: copia app + WebApi, verifica .NET 4.7.2+, cria atalho, preserva banco |
| `IniciarWebApi.bat` | Inicia o serviço de WhatsApp (gera `.env` e ajusta o Chromium automaticamente) |
| `LEIA_ME.txt` | Instruções passo a passo |

**Como instalar:** conectar o pendrive → duplo clique em `Instalar.bat` → aguardar a cópia (~poucos minutos) → atalho criado no Desktop. **Sem instalar Node.js nem Chrome** — o pacote traz os dois embutidos.

**WhatsApp (primeiro uso):** dentro do SOEN, abrir o menu **SoenWebApi (WhatsApp)** → **Iniciar SoenWebApi** → **Abrir Painel no Navegador** (`http://localhost:3000/admin`) → escanear o QR Code com o WhatsApp do celular da empresa.

> O instalador **não abre** o Chromium/navegador automaticamente ao terminar. O controle da SoenWebApi (Iniciar / Parar / Status / Abrir Painel / Documentação) ficou disponível no menu **SoenWebApi (WhatsApp)** dentro do aplicativo.

> A integração do SOEN com a SoenWebApi (envio de avisos/lembretes pelo app) está no roadmap. O serviço já roda de forma independente, com controle a partir do menu **SoenWebApi (WhatsApp)** do aplicativo.

### Opção 2 — Apenas o aplicativo (ZIP da Release, ~4 MB)

O sistema é **portátil** (não usa instalador ClickOnce/Inno Setup). Para instalar em uma máquina:

1. Copie o conteúdo da Release ZIP (ou de `dist\conteudo`) para uma pasta com permissão de escrita.
2. **Não** copie `soen.db`/`Backups` sobre uma instalação existente (o sistema preserva o banco local).
3. Crie um atalho para `Soen - Torrezim.exe`.

Instalações atualmente em uso:

| Máquina | Local |
|---|---|
| Nolt-DDS (Windows 11) | `C:\Users\erika_lellis\AppData\Local\SOEN - Torrezim\` |
| Erika-pc (Windows 10, teste) | `C:\Users\dds\AppData\Local\SOEN - Torrezim\` |

> Em uma instalação compartilhada em rede, todas as máquinas usam o MESMO `soen.db` (dados compartilhados). Cada pasta de instalação tem o próprio banco.

## Docs

- [Mapa dos menus](docs/MAPA_MENU.md)
- [Plano de desenvolvimento](docs/PLANO_DESENVOLVIMENTO.md)
- [Banco de dados](docs/BANCO_DE_DADOS.md)
- [Guia de arquitetura](docs/GUIA_ARQUITETURA.md)