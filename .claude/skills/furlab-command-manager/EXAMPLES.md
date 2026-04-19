# Exemplos de Uso - furlab-command-manager

## Criar um comando simples

```
Crie um comando "database verify" que verifica a conexão com o banco de dados
```

**Ações da skill:**
1. Criar pasta: `FurLab.CLI/Commands/Database/Verify/`
2. Criar `DatabaseVerifyCommand.cs` com classe `DatabaseVerifyCommand`
3. Criar `DatabaseVerifySettings.cs` com classe `DatabaseVerifySettings`
4. Adicionar em Program.cs:
   ```csharp
   db.AddCommand<Commands.Database.Verify.DatabaseVerifyCommand>("verify");
   ```

## Criar um comando aninhado

```
Adicione o comando "claude settings reset" para resetar configurações do Claude
```

**Ações da skill:**
1. Criar pasta: `FurLab.CLI/Commands/Claude/Settings/Reset/`
2. Criar `ClaudeSettingsResetCommand.cs`
3. Criar `ClaudeSettingsResetSettings.cs`
4. Adicionar em Program.cs:
   ```csharp
   claude.AddBranch("settings", settings =>
   {
       settings.AddCommand<Commands.Claude.Settings.Reset.ClaudeSettingsResetCommand>("reset");
   });
   ```

## Atualizar um comando existente

```
Adicione um argumento "--timeout" no comando database backup
```

**Ações da skill:**
1. Localizar: `FurLab.CLI/Commands/Database/Backup/DatabaseBackupSettings.cs`
2. Adicionar propriedade:
   ```csharp
   [CommandOption("--timeout")]
   [Description("Timeout in seconds.")]
   public int? Timeout { get; init; }
   ```
3. Atualizar `DatabaseBackupCommand.cs` para usar o novo parâmetro
4. Verificar build

## Remover um comando

```
Remova o comando "docker redis" (já não usamos mais)
```

**Ações da skill:**
1. Localizar pasta: `FurLab.CLI/Commands/Docker/Redis/`
2. Remover de Program.cs:
   ```csharp
   // docker.AddCommand<Commands.Docker.Redis.DockerRedisCommand>("redis");
   ```
3. Deletar arquivos:
   - `DockerRedisCommand.cs`
   - `DockerRedisSettings.cs`
4. Remover pasta se vazia
5. Verificar build

## Criar comando com múltiplos argumentos

```
Crie o comando "files copy" com argumentos: <source> <destination> [--overwrite]
```

**Estrutura gerada:**

**FileCopySettings.cs:**
```csharp
using System.ComponentModel;
using Spectre.Console.Cli;

namespace FurLab.CLI.Commands.Files.Copy;

public sealed class FileCopySettings : CommandSettings
{
    [CommandArgument(0, "<source>")]
    [Description("Source file path.")]
    public string Source { get; init; } = string.Empty;

    [CommandArgument(1, "<destination>")]
    [Description("Destination file path.")]
    public string Destination { get; init; } = string.Empty;

    [CommandOption("--overwrite")]
    [Description("Overwrite existing file.")]
    public bool Overwrite { get; init; }
}
```

**FileCopyCommand.cs:**
```csharp
using Spectre.Console.Cli;

namespace FurLab.CLI.Commands.Files.Copy;

public sealed class FileCopyCommand : AsyncCommand<FileCopySettings>
{
    protected override async Task<int> ExecuteAsync(CommandContext context, FileCopySettings settings, CancellationToken cancellation)
    {
        if (File.Exists(settings.Destination) && !settings.Overwrite)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Destination exists. Use --overwrite to replace.");
            return 1;
        }
        
        File.Copy(settings.Source, settings.Destination, settings.Overwrite);
        return 0;
    }
}
```

## Convenções Importantes

Sempre siga estas regras:

1. **Nome do arquivo = Nome da classe**
   - ✅ `DatabaseBackupCommand.cs` → `class DatabaseBackupCommand`
   - ❌ Nunca `Command.cs` → `class BackupCommand`

2. **Estrutura de pastas hierárquica**
   ```
   Commands/
   ├── Database/
   │   ├── Backup/
   │   │   ├── DatabaseBackupCommand.cs
   │   │   └── DatabaseBackupSettings.cs
   │   └── Restore/
   │       ├── DatabaseRestoreCommand.cs
   │       └── DatabaseRestoreSettings.cs
   ```

3. **Prefixo do arquivo**
   - Simples: `{Grupo}{Subcomando}`
   - Aninhado: `{Grupo}{Pai}{Subcomando}`

4. **Atualização do Program.cs**
   Sempre registrar o comando após criar
