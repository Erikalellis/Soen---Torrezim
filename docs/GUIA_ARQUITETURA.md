# Guia de Arquitetura

Padrão que vamos **repetir em todo módulo** do sistema. É o que transforma o
trabalho de muitos módulos em "copiar e adaptar" em vez de "criar do zero".

## Camadas

```
┌────────────────────────────┐
│  Tela (WinForms)          │  → só interface: ler campos, chamar DAO, mostrar resultado
├────────────────────────────┤
│  Modelo (classe)          │  → representa a entidade (ex.: Cliente)
├────────────────────────────┤
│  DAO (acesso a dados)     │  → Salvar/Buscar/Listar/Excluir usando SQL
├────────────────────────────┤
│  Database.cs              │  → cria o .db e tabelas; devolve conexão
└────────────────────────────┘
```

**Regra:** a tela NUNCA escreve SQL. Só o DAO fala com o banco.

## Pastas do projeto

```
Soen - Torrezim/
├── Program.cs            # Entrada: Database.Inicializar() + admin padrão + Login → FrmPrincipal
├── *.cs                  # Telas (WinForms) na raiz
├── Common/               # Infra compartilhada (Updater, Sessao, Exportacao, Logger...)
├── Data/                 # Database.cs + DAOs + BackupAgendado + Config/Impressora
├── Models/               # Classes de entidade
├── Properties/           # AssemblyInfo, Resources, Settings
├── docs/                 # Documentação
└── publicar.ps1          # Build/empacotar/publicar Release no GitHub
```

## Nomes de arquivo (padrão)

| Camada | Arquivo | Exemplo |
|---|---|---|
| Modelo | `Models/Cliente.cs` | classe `Cliente` |
| DAO | `Data/ClienteDAO.cs` | classe `ClienteDAO` |
| Helper | `Data/Database.cs` | classe estática `Database` |
| Tela | `CadastroCliente.cs` | Form (na raiz) |

## Estrutura de cada camada

### 1. Modelo — `Models/Cliente.cs`

```csharp
public class Cliente
{
    public long Id { get; set; }
    public string CpfCnpj { get; set; }
    public string NomeRazao { get; set; }
    public string Endereco { get; set; }
    // ... demais propriedades espelhando a tabela
}
```

> Modelos ficam em `Models/`; cada um espelha uma tabela (ver `docs/BANCO_DE_DADOS.md`).

### 2. DAO — `Data/ClienteDAO.cs`

```csharp
public class ClienteDAO
{
    public static long Salvar(Cliente c) { /* INSERT ou UPDATE */ }
    public static List<Cliente> Listar(string filtro) { /* SELECT com WHERE */ }
    public static Cliente BuscarPorId(long id) { /* SELECT WHERE id */ }
    public static void Excluir(long id) { /* DELETE */ }
}
```

- Usa `Database.AbrirConexao()` (devolve conexão aberta) e `Database.Nulo()` (limpa valores para gravação).
- Operações transacionais (ex.: venda + itens + caixa) usam `SQLiteTransaction`.

### 3. Tela — usa assim

```csharp
var c = new Cliente { NomeRazao = txtNome.Text, CpfCnpj = txtCpf.Text };
ClienteDAO.Salvar(c);
MessageBox.Show("Cliente salvo com sucesso!");
```

## Infra compartilhada — `Common/`

| Arquivo | Responsabilidade |
|---|---|
| `Updater.cs` | Auto-atualização via GitHub Releases (`Erikalellis/Soen---Torrezim`), TLS 1.2, download + aplicar com preservação do `soen.db` |
| `Sessao.cs` | Usuário autenticado (`UsuarioAtual`, `EhAdmin`) — usada em `FrmPrincipal.AplicarPermissoes` |
| `Exportacao.cs` | Gera `.xlsx` e `.pdf` a partir de `DataGridView` (sem bibliotecas externas) |
| `Logger.cs` | Log de erros/informações em `logs\soen.log` na pasta do exe |
| `BaseForm.cs` | Form padrão: fonte Segoe UI, StatusStrip, aplica imagem de fundo da empresa |
| `UIHelpers.cs` | Fábricas de controle (Label/Button/TextBox) para UI programática |
| `ComboItems.cs` | Preenche ComboBox (cliente, veículo, serviço, produto, técnico) |
| `Janelas.cs` | Abre cada módulo em janela única (foca a existente): `Janelas.Abrir(() => new X())` |

## Impressão e exportação — `RelatorioHelper.cs`

Central de impressão/exportação:

- `VisualizarReciboCaixa`, `VisualizarReciboVenda`, `VisualizarResumoCaixa` (fechamento) e `VisualizarDocumento` (Orçamento/OS) → `PrintPreviewDialog` com logo da empresa.
- `ExportarCsv(grid, nome)` → CSV com `;` e UTF-8.
- `AdicionarBotoesExportar` → adiciona botões CSV/PDF/Excel/Imprimir nos relatórios.

> Os formatos PDF/Excel são produzidos por `Common/Exportacao.cs`; o CSV de integração contábil
> (`IntegraContabil`) usa seu próprio exporter.

## Segurança (login)

- `Data/UsuarioDAO.cs`: senhas com **PBKDF2** (ver `docs/BANCO_DE_DADOS.md`), autenticação com comparação de tempo constante e upgrade automático de hashes antigos.
- `Program.cs` garante o usuário `admin` no primeiro uso.
- `FrmPrincipal` aplica permissões por perfil: `operador` não acessa Usuários/Backup/Contábil.

## Backup automático — `Data/BackupAgendado.cs`

- Frequências: diário, semanal, ao sair do sistema.
- `FrmPrincipal_Load` → `BackupAgendado.VerificarAgenda()` + Timer de 1h; `OnFormClosing` → `VerificarAgenda(true)`.
- Destino padrão `..\Backups`; retenção de N cópias (default 10) via `LimparExcedentes`.
- Configurações persistidas na tabela `config` (`ConfigDAO`).

## Boas práticas que seguimos

1. Nada de SQL fora dos DAOs.
2. Toda data salva em ISO `yyyy-MM-dd`.
3. Toda tela de listagem usa `DataGridView`.
4. Após salvar/excluir, mostrar `MessageBox` de confirmação.
5. Tratar erro com `try/catch` mostrando `MessageBox.Show(ex.Message)` e gravando no `Logger`.
6. Só commitar no git quando a tela funcionar (ver critério em PLANO_DESENVOLVIMENTO).
7. Módulos abrem em janela única via `Janelas.Abrir(...)`.
8. Fechamento de caixa e baixas de estoque sempre dentro de transação.