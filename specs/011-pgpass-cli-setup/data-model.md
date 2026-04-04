# Modelo de Dados: Comando CLI para Configurar pgpass

**Feature**: `011-pgpass-cli-setup` | **Data**: 2026-04-04

---

## Entidades de Domínio

### PgPassEntry

Representa uma única linha no arquivo `pgpass.conf`.

| Campo | Tipo C# | Obrigatório | Padrão | Restrições |
|-------|---------|-------------|--------|------------|
| `Hostname` | `string` | Não | `"localhost"` | Valor literal ou `"*"` (curinga). Validado por `SecurityUtils.IsValidHost()` se fornecido explicitamente. |
| `Port` | `string` | Não | `"5432"` | Valor literal ou `"*"` (curinga). Validado por `SecurityUtils.IsValidPort()` se fornecido explicitamente. |
| `Database` | `string` | Não | `"*"` | Valor literal ou `"*"` (curinga). Não pode ser vazio se fornecido. |
| `Username` | `string` | Não | `"postgres"` | Valor literal ou `"*"` (curinga). Não pode ser vazio se fornecido. |
| `Password` | `string` | Sim | — | Nunca vazio. Nunca curinga. Caracteres `:` e `\` são escapados antes da serialização. |

**Chave de identidade (unicidade)**: combinação de `(Hostname, Port, Database, Username)`. Dois registros com a mesma chave são considerados duplicatas; a senha pode diferir mas a entrada é tratada como idêntica para fins de detecção de duplicata.

**Ordenação no arquivo**: Entradas mais específicas devem preceder entradas com curinga (regra de correspondência do PostgreSQL: primeira linha que casa vence).

---

### Representação em arquivo

```
# Formato: hostname:porta:banco:usuario:senha
# Exemplo (com padrões):
localhost:5432:meu_banco:postgres:minhasenha

# Exemplo com valores explícitos:
db.exemplo.com:5433:producao:deploy_user:s3nh4\:especial

# Exemplo com curinga no banco:
localhost:5432:*:postgres:senhapadrao
```

**Regras de escape** (aplicadas pelo `PgPassService` na serialização):
- `:` → `\:`
- `\` → `\\`

**Regras de unescape** (aplicadas pelo `PgPassService` na desserialização):
- `\:` → `:`
- `\\` → `\`

---

## Modelos C# — Camada de Domínio

### `DevMaid.Core/Models/PgPassEntry.cs`

```csharp
public record PgPassEntry
{
    public string Hostname { get; init; } = "localhost";
    public string Port     { get; init; } = "5432";
    public string Database { get; init; } = "*";
    public string Username { get; init; } = "postgres";
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// Chave de identidade para detecção de duplicatas.
    /// </summary>
    public (string Hostname, string Port, string Database, string Username) IdentityKey
        => (Hostname, Port, Database, Username);
}
```

### `DevMaid.Core/Models/PgPassResult.cs`

```csharp
public record PgPassResult
{
    public bool    Success { get; init; }
    public string  Message { get; init; } = string.Empty;
    public bool    IsDuplicate { get; init; }

    public static PgPassResult Ok(string message)
        => new() { Success = true, Message = message };

    public static PgPassResult Duplicate(string message)
        => new() { Success = true, IsDuplicate = true, Message = message };

    public static PgPassResult Fail(string message)
        => new() { Success = false, Message = message };
}
```

> **Nota:** `IsDuplicate = true` com `Success = true` — duplicata não é um erro; o estado desejado já existe. Código de saída permanece `0`.

---

## DTOs de Comando — Camada CLI

### `DevMaid.CLI/CommandOptions/PgPassCommandOptions.cs`

```csharp
// Add subcommand options
public class PgPassAddOptions
{
    public string  Database { get; set; } = string.Empty;  // obrigatório
    public string? Password { get; set; }                   // opcional; prompt interativo se ausente
    public string? Hostname { get; set; }                   // padrão: "localhost"
    public string? Port     { get; set; }                   // padrão: "5432"
    public string? Username { get; set; }                   // padrão: "postgres"
}

// List subcommand options (sem parâmetros adicionais além de formato futuro)
public class PgPassListOptions { }

// Remove subcommand options
public class PgPassRemoveOptions
{
    public string  Database { get; set; } = string.Empty;  // obrigatório
    public string? Hostname { get; set; }                   // padrão: "localhost"
    public string? Port     { get; set; }                   // padrão: "5432"
    public string? Username { get; set; }                   // padrão: "postgres"
}
```

---

## Arquivo pgpass.conf — Entidade de Armazenamento

### `DevMaid.Core/Models/PgPassFile.cs` *(opcional — pode ser encapsulado no Service)*

```csharp
public class PgPassFile
{
    public string            FilePath { get; init; }
    public List<PgPassEntry> Entries  { get; init; } = [];
}
```

**Localização**: `%APPDATA%\postgresql\pgpass.conf`  
**Codificação**: UTF-8  
**Estrutura**: Zero ou mais linhas; linhas com `#` são comentários preservados na leitura/escrita  
**Capacidade esperada**: dezenas a centenas de entradas (sem limite definido)

---

## Transições de Estado

```
[Arquivo não existe]
    │
    ▼ devmaid pgpass add <banco> --password <senha>
[Arquivo criado com 1 entrada]
    │
    ├─▶ devmaid pgpass add <banco2> --password <senha2>
    │       └─▶ [Arquivo com N entradas]
    │
    ├─▶ devmaid pgpass add <banco> --password <senha>   ← duplicata (mesma chave)
    │       └─▶ [Arquivo inalterado] + mensagem informativa
    │
    ├─▶ devmaid pgpass list
    │       └─▶ [Arquivo inalterado] + saída com senhas mascaradas
    │
    └─▶ devmaid pgpass remove --database <banco>
            ├─▶ [entrada encontrada] → [Arquivo com N-1 entradas]
            └─▶ [entrada não encontrada] → [Arquivo inalterado] + mensagem informativa
```

---

## Regras de Validação

| Cenário | Comportamento | Código de Saída |
|---------|---------------|-----------------|
| Banco de dados vazio no `add` | Erro: argumento obrigatório ausente | `2` |
| Senha vazia após prompt | Erro: senha não pode ser vazia | `2` |
| Host inválido | Erro: formato de host inválido | `2` |
| Porta inválida | Erro: porta deve ser numérica (1–65535) | `2` |
| Entrada duplicata | Informativo: entrada já existe | `0` |
| Permissão negada em AppData | Erro: executar como administrador | `1` |
| Arquivo travado / somente-leitura | Erro: não foi possível gravar no arquivo | `1` |
| Arquivo não existe no `list` | Informativo: nenhuma entrada configurada | `0` |
| Entrada não encontrada no `remove` | Informativo: entrada não encontrada | `0` |
