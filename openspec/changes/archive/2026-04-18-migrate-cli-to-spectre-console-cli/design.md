## Contexto

O `FurLab.CLI` é uma ferramenta de linha de comando distribuída como `dotnet tool` (`fur`). Atualmente utiliza `System.CommandLine` para parsing de argumentos e um padrão de **service locator estático** para acessar os serviços do `FurLab.Core`: cinco classes façade (`Logger`, `ConfigurationService`, `UserConfigService`, `CredentialService`, `PostgresDatabaseLister`) cada uma com um campo `private static IServiceProvider?` que é populado manualmente no startup via `SetServiceProvider(sp)`.

Todos os 10 commands são `public static class` com um método `Build()` que constrói e retorna uma `Command` (System.CommandLine) com lambdas inline para a lógica de execução. Isso resulta em arquivos longos e monolíticos (ex: `SettingsCommand.cs` com 600+ linhas), impossibilidade de testar commands em isolamento e violação do princípio de responsabilidade única.

O `FurLab.Core` já possui DI correto por construtor, interfaces para todos os serviços, e `AddFurLabServices()` como ponto de registro. A camada CLI, porém, não aproveita esse design.

Três serviços existem apenas no CLI mas têm responsabilidade de domínio suficiente para residir no Core: `DockerService`, `PostgresPasswordHandler`, e `PostgresBinaryLocator` (já no Core mas sem interface nem registro no DI).

## Objetivos / Não-Objetivos

**Objetivos:**
- Substituir `System.CommandLine` por `Spectre.Console.Cli` mantendo 100% de compatibilidade da interface pública (`fur <command> <subcommand> [args]`)
- Eliminar o padrão de service locator estático — todos os commands recebem dependências via construtor
- Converter cada leaf command em uma classe própria herdando de `AsyncCommand<Settings>` com atributos declarativos
- Criar a ponte DI via `TypeRegistrar`/`TypeResolver` seguindo o padrão oficial do Spectre.Console.Cli
- Centralizar tratamento de exceções e mapeamento para exit codes via `SetExceptionHandler`
- Mover `DockerService`, `PostgresPasswordHandler` e `IPostgresBinaryLocator` para `FurLab.Core` com registro no DI
- Preservar todos os exit codes existentes e comportamento de erro

**Não-Objetivos:**
- Alterar as interfaces existentes do `FurLab.Core` (`IConfigurationService`, `IUserConfigService`, `ICredentialService`, `IDatabaseService`, `IPgPassService`)
- Adicionar novos commands ou funcionalidades
- Migrar `SecurityUtils`, `Utils`, `SqlQueryAnalyzer` para DI — permanecem estáticos
- Criar testes unitários nesta mudança (desbloqueado pela mudança, mas escopo separado)
- Alterar o processo de build, publicação ou empacotamento do dotnet tool

## Decisões

### D1 — `Spectre.Console.Cli` ao invés de continuar com `System.CommandLine`

**Decisão:** Migrar para `Spectre.Console.Cli`.

**Rationale:** O `Spectre.Console.Cli` oferece um modelo declarativo via atributos (`[CommandOption]`, `[CommandArgument]`, `[Description]`, `[DefaultValue]`) que reduz drasticamente o boilerplate de configuração de commands. A herança de `AsyncCommand<Settings>` separa Settings de lógica de execução, facilitando leitura e teste. O suporte nativo a DI via `ITypeRegistrar` é first-class, ao contrário de `System.CommandLine` onde o DI é bolt-on. O ecossistema Spectre já é usado no projeto para output (`AnsiConsole`), então a adição de `Spectre.Console.Cli` é incremental, não uma nova dependência de vendor.

**Alternativa considerada:** Continuar com `System.CommandLine` e apenas refatorar os commands para classes com construtor injetável. Descartado porque o System.CommandLine não tem um modelo idiomático para isso e exigiria mais boilerplate manual.

---

### D2 — `SetExceptionHandler` ao invés de `PropagateExceptions` + try-catch manual

**Decisão:** Usar `config.SetExceptionHandler((ex, resolver) => ...)`.

**Rationale:** Mantém o tratamento de exceções dentro do ciclo de vida do framework. O handler recebe o `ITypeResolver`, permitindo injetar um logger (`resolver?.Resolve(typeof(ILogger<Program>))`) mesmo durante o handling de erro. O pattern matching com switch expression é conciso para mapear N tipos de exceção a exit codes. Ficar próximo do framework facilita manutenção futura e alinhamento com a documentação oficial.

**Alternativa considerada:** `PropagateExceptions()` + bloco try-catch manual no `Program.cs` — migração mais direta do código atual, mas mantém lógica de error handling fora do framework e perde o `ITypeResolver` para logging.

**Mapeamento de exit codes a preservar:**
```
NpgsqlException (SqlState not null)  → 10
NpgsqlException (geral)              → 11
IOException                          → 20
UnauthorizedAccessException          → 30
DirectoryNotFoundException           → 21
FileNotFoundException                → 22
InvalidOperationException            → 40
ArgumentException                    → 41
TimeoutException                     → 50
OperationCanceledException           → 130
Exception (geral)                    → 1
```

---

### D3 — `TypeRegistrar`/`TypeResolver` em `FurLab.CLI/Infrastructure/`

**Decisão:** Criar dois arquivos pequenos (`TypeRegistrar.cs`, `TypeResolver.cs`) em `FurLab.CLI/Infrastructure/` seguindo o padrão da documentação oficial do Spectre.Console.Cli.

**Rationale:** São classes de infraestrutura pura (bridge entre dois containers), não pertencem a nenhum domínio específico. A pasta `Infrastructure/` sinaliza claramente que são peças de "encanamento" e não lógica de negócio.

```csharp
// TypeRegistrar recebe IServiceCollection, constrói o provider ao Build()
public sealed class TypeRegistrar(IServiceCollection services) : ITypeRegistrar
{
    public ITypeResolver Build() => new TypeResolver(services.BuildServiceProvider());
    public void Register(Type service, Type implementation) =>
        services.AddSingleton(service, implementation);
    public void RegisterInstance(Type service, object implementation) =>
        services.AddSingleton(service, implementation);
    public void RegisterLazy(Type service, Func<object> factory) =>
        services.AddSingleton(service, _ => factory());
}
```

---

### D4 — Estrutura de pastas para os commands

**Decisão:** Um arquivo por leaf command, organizados em subpastas por grupo de command.

```
FurLab.CLI/Commands/
├── File/
│   └── FileCombineCommand.cs
├── Claude/
│   ├── ClaudeInstallCommand.cs
│   └── Settings/
│       ├── ClaudeMcpDatabaseCommand.cs
│       └── ClaudeWinEnvCommand.cs
├── OpenCode/
│   └── Settings/
│       ├── OpenCodeMcpDatabaseCommand.cs
│       └── OpenCodeDefaultModelCommand.cs
├── Winget/
│   ├── WingetBackupCommand.cs
│   └── WingetRestoreCommand.cs
├── Database/
│   ├── DatabaseBackupCommand.cs
│   ├── DatabaseRestoreCommand.cs
│   └── PgPass/
│       ├── PgPassAddCommand.cs
│       ├── PgPassListCommand.cs
│       └── PgPassRemoveCommand.cs
├── Query/
│   └── QueryRunCommand.cs
├── Clean/
│   └── CleanCommand.cs
├── WindowsFeatures/
│   ├── WindowsFeaturesExportCommand.cs
│   ├── WindowsFeaturesImportCommand.cs
│   └── WindowsFeaturesListCommand.cs
├── Docker/
│   └── DockerPostgresCommand.cs
└── Settings/
    └── DbServers/
        ├── DbServersListCommand.cs
        ├── DbServersAddCommand.cs
        ├── DbServersRemoveCommand.cs
        ├── DbServersTestCommand.cs
        └── DbServersSetPasswordCommand.cs
```

**Rationale:** Espelha a hierarquia de commands (`fur database backup` → `Database/DatabaseBackupCommand.cs`). Cada arquivo tem responsabilidade única e tamanho gerenciável. Facilita navegação e futura adição de commands.

---

### D5 — `AsyncCommand<Settings>` como classe base para todos os commands

**Decisão:** Todos os leaf commands herdam de `AsyncCommand<Settings>` (não `Command<Settings>`).

**Rationale:** Todos os commands existentes já têm lógica assíncrona (acesso a banco, processos externos, I/O de arquivo). Usar `AsyncCommand` desde o início evita o anti-padrão `.GetAwaiter().GetResult()` que surgiria ao misturar sync/async. O Spectre.Console.Cli suporta nativamente `AsyncCommand` via `ExecuteAsync`.

---

### D6 — Serviços movidos para o Core

**Decisão:** `DockerService`, `PostgresPasswordHandler` e `PostgresBinaryLocator` (com interface) vão para `FurLab.Core`.

**Rationale:**
- `DockerService`: gerencia containers Docker — responsabilidade de infraestrutura que um frontend futuro pode precisar
- `PostgresPasswordHandler`: lida com leitura segura de senha — pode ser usado por qualquer frontend
- `IPostgresBinaryLocator`: localiza binários `pg_dump`/`pg_restore`/`psql` — infraestrutura de banco de dados, não lógica de CLI

Todos registrados em `AddFurLabServices()` como Singleton.

**Interfaces a criar:**
```csharp
// IDockerService
Task<DockerStatus> GetDockerStatusAsync();
Task EnsurePostgresContainerAsync(string containerName, DockerPostgresOptions options);

// IPostgresBinaryLocator
string? FindPgDump();
string? FindPgRestore();
string? FindPsql();

// IPostgresPasswordHandler
string ReadPasswordInteractively(string prompt);
```

---

### D7 — `CsvExporter` permanece no CLI

**Decisão:** `CsvExporter` fica em `FurLab.CLI/Commands/Query/` como classe normal (não estática), injetável no `QueryRunCommand`.

**Rationale:** É responsabilidade exclusiva do `QueryRunCommand` — nenhum outro command usa CSV export. Não tem valor em subir para o Core neste momento. A transição de estático para instanciável (sem interface por ora) é suficiente para permitir injeção via construtor.

---

### D8 — Branches vs Commands no `app.Configure`

**Decisão:** Usar `AddBranch` para grupos com subcommands e `AddCommand<T>` para leaf commands.

```csharp
app.Configure(config =>
{
    config.SetApplicationName("fur");
    config.AddBranch("database", db =>
    {
        db.SetDescription("Database utilities.");
        db.AddCommand<DatabaseBackupCommand>("backup");
        db.AddCommand<DatabaseRestoreCommand>("restore");
        db.AddBranch("pgpass", pgpass =>
        {
            pgpass.SetDescription("Gerencia o arquivo pgpass.conf");
            pgpass.AddCommand<PgPassAddCommand>("add");
            pgpass.AddCommand<PgPassListCommand>("list");
            pgpass.AddCommand<PgPassRemoveCommand>("remove");
        });
    });
    // ... demais branches
});
```

**Rationale:** Espelha exatamente a hierarquia existente. `AddBranch` cria o nó intermediário (ex: `database`) que mostra subcommands no `--help` mas não é executável diretamente.

## Riscos / Trade-offs

**[Risco] `QueryRunCommand` tem 20+ opções e lógica altamente acoplada (Channels, Polly, multi-server)**
→ Mitigação: extrair a lógica de execução do command para métodos privados internos. O command em si apenas coordena Settings → chamada de serviço. O `CsvExporter` já encapsula parte da lógica de escrita.

**[Risco] `SettingsCommand` tem wizard interativo com Spectre.Console (prompts, multi-select, tabelas)**
→ Mitigação: toda a lógica de interação com `AnsiConsole` permanece nos commands. O Spectre.Console.Cli injeta `IAnsiConsole` automaticamente no DI, permitindo uso testável.

**[Risco] `OpenCodeCommand` tem `internal static Func<IReadOnlyList<string>>? ModelsProvider` hook para testes**
→ Mitigação: ao migrar para `OpenCodeDefaultModelCommand`, esse hook se torna um campo `internal` na classe de command ou um serviço injetável. Avaliar durante implementação.

**[Risco] `PgPassCommand` atualmente ignora o DI (`new PgPassService()` direto)**
→ Mitigação: com a migração, `PgPassAddCommand` recebe `IPgPassService` no construtor — o bug se resolve naturalmente.

**[Risco] O `SetExceptionHandler` não é chamado durante parsing se `ITypeResolver` for null**
→ Mitigação: o handler deve verificar `resolver != null` antes de tentar resolver um logger. Erros de parsing são exibidos pelo próprio Spectre.Console.Cli de forma amigável.

**[Trade-off] Mais arquivos, mas menor complexidade por arquivo**
→ O número de arquivos em `Commands/` sobe de 11 para ~25, mas nenhum arquivo terá mais de ~150 linhas. Navegação melhora com a estrutura de pastas espelhando a hierarquia de commands.

## Plano de Migração

A migração pode ser feita de forma incremental por grupo de commands, mantendo a builds passando a cada passo:

1. **Preparação do Core**: criar interfaces e mover `DockerService`, `PostgresBinaryLocator` (com interface), `PostgresPasswordHandler` para `FurLab.Core`; registrar em `AddFurLabServices()`
2. **Infraestrutura CLI**: adicionar `Spectre.Console.Cli` ao `FurLab.CLI`; criar `TypeRegistrar`/`TypeResolver`; criar o novo `Program.cs` com `CommandApp` (mantendo o antigo comentado temporariamente)
3. **Commands sem DI** (migração mais simples): `file`, `clean`, `windowsfeatures`, `winget`, `claude`, `opencode`
4. **Commands com DI**: `docker`, `database` (inclui `pgpass`), `settings`, `query`
5. **Remoção das façades estáticas**: após todos os commands migrados, remover as cinco classes de wrapper e limpar o `Program.cs` antigo
6. **Remover `System.CommandLine`** do `.csproj`
7. **Validação**: executar smoke test completo de todos os commands e verificar exit codes

**Rollback:** como a migração ocorre em branches de código (Git), o rollback é um `git revert`. Não há mudança de schema de dados, arquivos de configuração ou comportamento externo.

## Questões em Aberto

_(nenhuma — todas as decisões foram tomadas durante a fase de exploração)_
