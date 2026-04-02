# Feature Spec: Winget Package Manager

**ID:** 006  
**Slug:** winget-package-manager  
**Status:** Implemented  
**Version:** 1.0  

---

## Purpose

Allow developers to back up and restore their Windows application environment (via winget) when migrating to a new machine, reinstalling Windows, or replicating a development setup across machines.

---

## User Stories

**US-006.1** — As a developer migrating to a new machine, I want to export all my winget-installed packages to a JSON file, so that I can reproduce my development environment on the new machine.

**US-006.2** — As a developer on a fresh Windows installation, I want to restore all packages from a backup file, so that I get my full tool set back automatically.

**US-006.3** — As a developer, I want the backup file to include version information, so that I can track what was installed at a specific point in time.

---

## Acceptance Criteria

| ID | Criterion |
|----|-----------|
| AC-006.1 | `devmaid winget backup` creates `backup-winget.json` in the current directory (or `--output` directory) containing the list of installed packages with their IDs and versions. |
| AC-006.2 | The backup JSON includes a `CreationDate` (ISO 8601) timestamp. |
| AC-006.3 | `devmaid winget restore --input <file>` runs `winget import` from the specified JSON file. |
| AC-006.4 | If the backup file already exists during `backup`, the tool prompts for overwrite confirmation before proceeding. |
| AC-006.5 | If a package fails to restore, the tool logs the failure but continues restoring remaining packages. |
| AC-006.6 | If winget is not installed, both commands exit `3` with installation instructions. |

---

## CLI Interface

```bash
devmaid winget backup [--output <directory>]
devmaid winget restore --input <file>
```

### Options

#### Backup

| Option | Short | Required | Default | Description |
|--------|-------|----------|---------|-------------|
| `--output` | `-o` | No | `./` | Output directory for backup file |

#### Restore

| Option | Short | Required | Default | Description |
|--------|-------|----------|---------|-------------|
| `--input` | `-i` | Yes | — | Path to backup JSON file |

### Exit Codes

| Code | Scenario |
|------|----------|
| `0` | Operation completed successfully |
| `1` | File I/O error or partial restore failure |
| `2` | Missing required option |
| `3` | winget not found |

---

## Backup File Format

```json
{
  "CreationDate": "2026-04-01T10:30:00Z",
  "Packages": [
    { "Id": "Git.Git", "Version": "2.44.0" },
    { "Id": "Microsoft.VisualStudioCode", "Version": "1.88.0" }
  ]
}
```

---

## Error Scenarios

| Scenario | Expected Behavior |
|----------|------------------|
| winget not installed | Exit `3`, message: `"winget is not installed. Install App Installer from the Microsoft Store."` |
| No packages installed | Create backup with empty `Packages` array and `CreationDate` |
| Backup file exists | Prompt: `"File '<path>' already exists. Overwrite? [y/N]"`. Abort on `N`. |
| Restore file not found | Exit `1`, message: `"Backup file '<path>' not found."` |
| Package not available during restore | Log warning per package, continue with next, report summary at end |
| Network unavailable during restore | Exit `1`, message: `"Network unavailable. Connect to the internet and try again."` |

---

## Non-Functional Requirements

- Backup must complete in under **30 seconds** for environments with up to 200 packages.
- Restore operations should not require administrator privileges unless individual packages require elevation (in which case winget handles the UAC prompt).
