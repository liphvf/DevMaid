---
name: furlab-command-manager
description: Create, update, and remove CLI commands for the FurLab project following the established folder structure and naming conventions.
license: MIT
compatibility: Requires FurLab.CLI project structure with Spectre.Console.Cli.
metadata:
  author: furlab
  version: "1.0"
---

Manage CLI commands in the FurLab project (create, update, remove) following the strict folder structure and naming conventions.

---

## Folder Structure Convention

All commands MUST follow this structure:

```
FurLab.CLI/Commands/
├── {Group}/                           ← Command group (e.g., "Database", "Claude")
│   ├── {Subcommand}/                  ← Each subcommand gets its own folder
│   │   ├── {Group}{Subcommand}Command.cs       ← Command implementation
│   │   ├── {Group}{Subcommand}Settings.cs      ← Settings class
│   │   └── {Group}{Subcommand}Config.cs        ← Optional config class
│   └── SharedHelper.cs                ← Shared classes (optional)
```

**Examples:**
- `Database/Backup/DatabaseBackupCommand.cs` → `class DatabaseBackupCommand`
- `Claude/Install/ClaudeInstallCommand.cs` → `class ClaudeInstallCommand`
- `Settings/DbServers/Add/DbServersAddCommand.cs` → `class DbServersAddCommand`

---

## Operations

### 1. CREATE a new command

**Step 1.1: Validate inputs**
- Extract from user request:
  - **Group**: The command group (e.g., "database", "claude")
  - **Subcommand**: The subcommand name (e.g., "backup", "install")
  - **Description**: What the command does
  - **Settings**: List of arguments/options needed
  - **Parent**: If nested under another command (e.g., "settings" under "claude")

**Step 1.2: Calculate paths and names**

```
FolderPath = "FurLab.CLI/Commands/{Group}/{Subcommand}"
If has Parent:
  FolderPath = "FurLab.CLI/Commands/{Group}/{Parent}/{Subcommand}"

FilePrefix = "{Group}{Subcommand}"
If has Parent:
  FilePrefix = "{Group}{Parent}{Subcommand}"

CommandFile = "{FolderPath}/{FilePrefix}Command.cs"
SettingsFile = "{FolderPath}/{FilePrefix}Settings.cs"
ConfigFile = "{FolderPath}/{FilePrefix}Config.cs" (optional)
```

**Step 1.3: Create folder structure**

```bash
mkdir -p "{FolderPath}"
```

**Step 1.4: Generate Command.cs**

Template:
```csharp
using Spectre.Console.Cli;

namespace FurLab.CLI.Commands.{Group}.{Subcommand};

/// <summary>
/// {Description}
/// </summary>
public sealed class {FilePrefix}Command : AsyncCommand<{FilePrefix}Settings>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="{FilePrefix}Command"/> class.
    /// </summary>
    public {FilePrefix}Command()
    {
    }

    /// <inheritdoc/>
    protected override async Task<int> ExecuteAsync(CommandContext context, {FilePrefix}Settings settings, CancellationToken cancellation)
    {
        // TODO: Implement command logic
        await Task.CompletedTask;
        return 0;
    }
}
```

**Step 1.5: Generate Settings.cs**

Template:
```csharp
using System.ComponentModel;
using Spectre.Console.Cli;

namespace FurLab.CLI.Commands.{Group}.{Subcommand};

/// <summary>
/// Settings for the {Group} {Subcommand} command.
/// </summary>
public sealed class {FilePrefix}Settings : CommandSettings
{
    // TODO: Add [CommandArgument] and [CommandOption] properties
}
```

**Step 1.6: Register in Program.cs**

Find the appropriate section in `FurLab.CLI/Program.cs`:

If top-level command under existing group:
```csharp
config.AddBranch("{group}", {groupVar} =>
{
    {groupVar}.AddCommand<Commands.{Group}.{Subcommand}.{FilePrefix}Command>("{subcommand}");
});
```

If nested under parent:
```csharp
config.AddBranch("{group}", {groupVar} =>
{
    {groupVar}.AddBranch("{parent}", {parentVar} =>
    {
        {parentVar}.AddCommand<Commands.{Group}.{Parent}.{Subcommand}.{FilePrefix}Command>("{subcommand}");
    });
});
```

**Step 1.7: Verify build**

```bash
dotnet build --nologo
```

---

### 2. UPDATE an existing command

**Step 2.1: Locate the command files**

Search in `FurLab.CLI/Commands/` for files matching `*{Subcommand}Command.cs`.

**Step 2.2: Identify what to update**
- Settings (add/remove arguments or options)
- Command logic
- Description/documentation
- Namespace (if moving)

**Step 2.3: Apply changes preserving naming**

If renaming a class, ensure file name matches:
- File: `{NewPrefix}Command.cs`
- Class: `class {NewPrefix}Command`

**Step 2.4: Update all references**

Search and update in:
- `Program.cs` - registration
- Other commands that reference this one
- Test files

**Step 2.5: Verify build**

```bash
dotnet build --nologo
```

---

### 3. REMOVE a command

**Step 3.1: Locate the command folder**

Find the folder containing the command files.

**Step 3.2: Remove from Program.cs**

Delete the `AddCommand<...>()` line for this command.

**Step 3.3: Delete files**

Remove:
- `{Prefix}Command.cs`
- `{Prefix}Settings.cs`
- `{Prefix}Config.cs` (if exists)

**Step 3.4: Clean up empty folders**

If the folder is now empty, remove it.

**Step 3.5: Verify build**

```bash
dotnet build --nologo
```

---

## Naming Rules (STRICT)

| Element | Pattern | Example |
|---------|---------|---------|
| Folder | `Commands/{Group}/{Subcommand}/` | `Commands/Database/Backup/` |
| Command file | `{Prefix}Command.cs` | `DatabaseBackupCommand.cs` |
| Settings file | `{Prefix}Settings.cs` | `DatabaseBackupSettings.cs` |
| Config file | `{Prefix}Config.cs` | `DatabaseBackupConfig.cs` |
| Class name | Same as file name | `class DatabaseBackupCommand` |
| Namespace | Matches folder path | `Commands.Database.Backup` |

**Prefix calculation:**
- Simple: `{Group}{Subcommand}` → `DatabaseBackup`
- With parent: `{Group}{Parent}{Subcommand}` → `ClaudeSettingsMcpDatabase`

---

## Common Settings Patterns

**Required argument:**
```csharp
[CommandArgument(0, "<name>")]
[Description("The name of the item.")]
public string Name { get; init; } = string.Empty;
```

**Optional argument:**
```csharp
[CommandArgument(0, "[name]")]
[Description("Optional name.")]
public string? Name { get; init; }
```

**Option with short/long form:**
```csharp
[CommandOption("-o|--output")]
[Description("Output file path.")]
public string? OutputPath { get; init; }
```

**Boolean flag:**
```csharp
[CommandOption("--global")]
[Description("Apply globally.")]
public bool Global { get; init; }
```

**Validation:**
```csharp
public override ValidationResult Validate()
{
    if (string.IsNullOrWhiteSpace(Name))
    {
        return ValidationResult.Error("Name is required.");
    }
    return base.Validate();
}
```

---

## Guardrails

- **NEVER** use generic names like `Command.cs` or `Settings.cs` - always prefix with hierarchy
- **ALWAYS** match class name exactly to file name (without extension)
- **ALWAYS** update `Program.cs` when adding/removing commands
- **NEVER** commit without verifying `dotnet build` succeeds
- **ALWAYS** place each command in its own subfolder
- **NEVER** share Settings classes between commands - each command has its own

---

## Examples

### Example 1: Create simple command

User: "Create a new command `docker redis` that starts a Redis container"

Action:
1. Folder: `Commands/Docker/Redis/`
2. Files: `DockerRedisCommand.cs`, `DockerRedisSettings.cs`
3. Classes: `DockerRedisCommand`, `DockerRedisSettings`
4. Register in Program.cs under `docker` branch

### Example 2: Create nested command

User: "Add `claude settings custom-model` command"

Action:
1. Folder: `Commands/Claude/Settings/CustomModel/`
2. Files: `ClaudeSettingsCustomModelCommand.cs`, `ClaudeSettingsCustomModelSettings.cs`
3. Classes: `ClaudeSettingsCustomModelCommand`, `ClaudeSettingsCustomModelSettings`
4. Register in Program.cs under `claude` → `settings` branch

### Example 3: Remove command

User: "Remove the `database pgpass list` command"

Action:
1. Locate: `Commands/Database/PgPass/List/`
2. Files: `PgPassListCommand.cs`, `PgPassListSettings.cs`
3. Remove registration from Program.cs
4. Delete folder and files
