# PR Review — `qualityaain` → `main`

**Branch:** `qualityaain`
**Commits:** 14 commits à frente da `main`
**Escopo:** Refatoração significativa do `QueryCommand`, novo sistema de configuração de usuário (`furlab.jsonc`), `SettingsCommand`, `SqlQueryAnalyzer`, suporte a query inline (`-c`), execução paralela multi-servidor, detecção de queries destrutivas.

---

## Sumário Executivo

Esta PR representa uma mudança arquitetural importante: migração de configuração via `appsettings.json` para um arquivo de configuração gerenciado pelo usuário (`furlab.jsonc`), e uma reescrita substancial do `QueryCommand` para suportar multi-servidor de forma nativa. O código está bem estruturado, com boa cobertura de testes e vários pontos de qualidade já endereçados. Resta um item pendente antes do merge.

---

## Aprovado

- **Arquitetura limpa:** A separação `IUserConfigService` (Core) / `UserConfigService` (CLI wrapper estático) é razoável para compatibilidade. A interface está bem definida.
- **Modelos bem documentados:** `ServerConfigEntry`, `UserConfig`, `UserDefaults` com XML docs claros.
- **`SqlQueryAnalyzer`:** Lógica simples e direta para detecção de queries destrutivas com suporte a CTEs e stripping de comentários SQL. Testes unitários cobrindo todos os casos relevantes, incluindo a limitação conhecida de múltiplos CTEs documentada com `// KNOWN LIMITATION`.
- **Testes:** `UserConfigServiceTests` (15 testes, incluindo 4 de `TryLoadLegacyConfig`), `SqlQueryAnalyzerTests` (24 testes, 5 cobrindo CTEs), `CsvExportTests` (8 testes chamando `CsvExporter` diretamente) são bem escritos, isolados com diretórios temporários, e cobrem casos de borda.
- **`ValidateDatabaseAccessAsync` async:** Corretamente usa `OpenAsync` / `ExecuteScalarAsync`, `CancellationToken` passado em toda a cadeia incluindo `ListDatabasesAsync`.
- **`Environment.Exit` removido dos handlers:** Todos os métodos de lógica de negócio lançam exceções tipadas. `Environment.Exit` existe apenas no `AppDomain.CurrentDomain.UnhandledException` — correto.
- **`Program.cs` com handler completo:** `EnableDefaultExceptionHandler = false` garante que exceções propagam para o `try/catch`. 14 catch clauses cobrindo todos os tipos lançados (`ArgumentException`, `FileNotFoundException`, `OperationCanceledException`, `PostgresBinaryNotFoundException`, `BackupFailedException`, `RestoreFailedException`, `PathTraversalException`, etc.) com exit codes semânticos (130 para cancelamento, 2 para uso incorreto, 1 para erros de runtime).
- **`CsvExporter` extraído:** `WriteConsolidatedCsv`, `WriteServerCsv`, `BuildColumnList` em classe própria `internal static`. Sobrecargas testáveis via `CsvWriter` sem I/O de arquivo. `QueryCommand` delega via expression-body.
- **`UnescapeInlineQuery` testado:** 9 testes cobrindo outer quotes, backslash escape, aspas desbalanceadas, string vazia, e casos de borda. Método exposto como `internal static` com XML doc explicando a necessidade.
- **`TryLoadLegacyConfig` com implementação real e testes:** `TestableUserConfigService` implementa o parsing completo do `appsettings.json` legado. 4 testes cobrindo arquivo ausente, migração completa de todos os campos, seção ausente, e JSON inválido.
- **`RemoveServer` simplificado:** Dead code removido. Lógica direta: nome fornecido → remove diretamente; caso contrário → `MultiSelectionPrompt` interativo.
- **Strings padronizadas para inglês:** Todas as mensagens de UI em `QueryCommand` e `SettingsCommand` estão em inglês.
- **Uso de `Polly`:** Retry com backoff exponencial para falhas transientes de conexão.
- **`DockerStatus` extraído para arquivo próprio:** Refatoração cosmética correta.

---

## Problema Pendente

### 1. `AddServerCommandOptions.Database` (singular) é dead property

**Arquivo:** `FurLab.CLI/CommandOptions/AddServerCommandOptions.cs` e `FurLab.CLI/Commands/SettingsCommand.cs`

A opção `--database` é declarada, registrada no comando e lida pelo parser:

```csharp
// AddServerCommandOptions.cs
public string? Database { get; set; }   // singular — nunca consumido
public string? Databases { get; set; }  // plural — usado em BuildServerFromOptions
```

```csharp
// SettingsCommand.cs — lida pelo parser mas não consumida
Database = parseResult.GetValue(databaseOption),
```

```csharp
// BuildServerFromOptions — só usa Databases, ignora Database
var databases = ParseDatabases(options.Databases);
```

O usuário que passa `--database mydb` acredita ter configurado o banco de dados padrão, mas o valor é silenciosamente descartado. A opção `--databases` (plural, com vírgulas) é a que realmente funciona.

**Recomendação:** Ou remover `--database` / `Database` completamente, ou fazer `BuildServerFromOptions` usá-lo como fallback quando `Databases` for nulo/vazio:

```csharp
var databases = ParseDatabases(
    !string.IsNullOrWhiteSpace(options.Databases) ? options.Databases : options.Database
);
```

---

## Menores / Nitpicks

- `ResiliencePipeline` é `static readonly` em `QueryCommand` — correto para o caso de uso atual, mas vale considerar injeção futura se a configuração de retry precisar variar por servidor.
- `ServerConfigEntry.Password` doc menciona `"stored in plain text for dev environments"` — razoável como nota por ora, mas não há orientação sobre uso em produção. Melhoria de armazenamento planejada para o futuro.

---

## Resultado

| Categoria | Status |
|---|---|
| Funcionalidade core | Aprovado |
| Arquitetura | Aprovado |
| Segurança | Aprovado com ressalva (Password plain text — melhoria planejada) |
| Qualidade de código | Aprovado com ressalva (#1 dead property) |
| Consistência | Aprovado |
| Testes | Aprovado |

**Recomendação:** Aprovado condicionalmente. Corrigir o item **#1** (`--database` dead property) antes do merge — o comportamento atual é silenciosamente enganoso para o usuário.
