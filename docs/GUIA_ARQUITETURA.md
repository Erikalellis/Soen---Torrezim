# Guia de Arquitetura

Padrão que vamos **repetir em todo módulo** do sistema. É o que transforma o
trabalho de 30 módulos em "copiar e adaptar" em vez de "criar do zero".

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

## Nomes de arquivo (padrão)

| Camada | Arquivo | Exemplo |
|---|---|---|
| Modelo | `Models/Cliente.cs` | classe `Cliente` |
| DAO | `Data/ClienteDAO.cs` | classe `ClienteDAO` |
| Helper | `Data/Database.cs` | classe estática `Database` |
| Tela | `ClienteForm.cs` | (substitui as telas atuais) |

## Exemplo do padrão (Cliente)

### 1. Modelo — `Models/Cliente.cs`

```csharp
public class Cliente
{
    public long Id { get; set; }
    public string CpfCnpj { get; set; }
    public string NomeRazao { get; set; }
    public string Sexo { get; set; }
    public string Nascimento { get; set; }
    public string Cep { get; set; }
    public string Endereco { get; set; }
    public string Complemento { get; set; }
    public string Bairro { get; set; }
    public string Cidade { get; set; }
    public string Estado { get; set; }
    public string Fone1 { get; set; }
    public string Fone2 { get; set; }
    public string Email1 { get; set; }
    public string Email2 { get; set; }
    public string Responsavel { get; set; }
    public string Funcao { get; set; }
    public string Tipo { get; set; } // "cliente" | "fornecedor"
}
```

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

### 3. Tela — usa assim

```csharp
var c = new Cliente {
    NomeRazao = txtNome.Text,
    CpfCnpj   = txtCpf.Text,
    // ... preencher os demais
};
ClienteDAO.Salvar(c);
MessageBox.Show("Cliente salvo com sucesso!");
```

## Boas práticas que seguimos

1. Nada de SQL fora dos DAOs.
2. Toda data salva em ISO `yyyy-MM-dd`.
3. Toda tela de listagem usa `DataGridView`.
4. Após salvar/excluir, mostrar `MessageBox` de confirmação.
5. Tratar erro com `try/catch` mostrando `MessageBox.Show(ex.Message)`.
6. Só commitar no git quando a tela funcionar (ver critério em PLANO_DESENVOLVIMENTO).

## Diagrama de pastas futura

```
Soen - Torrezim/
├── Models/            # classes de entidade
├── Data/              # Database.cs + DAOs
├── Forms/             # telas (cada módulo)
├── docs/              # documentação
└── <telas atuais na raiz serão migradas aos poucos>
```