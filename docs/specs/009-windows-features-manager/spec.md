# Feature Spec: Windows Features Manager

**ID:** 009  
**Slug:** windows-features-manager  
**Status:** Implemented  
**Version:** 1.0  

---

## Purpose

Allow developers to export their currently activated Windows Optional Features to a JSON backup file, and later import/enable those features on a fresh Windows installation or new machine — eliminating the need to manually re-enable features through the Windows UI.

---

## User Stories

**US-009.1** — As a developer setting up a new machine, I want to restore all my previously activated Windows Optional Features from a backup file, so that my environment is consistent with my previous setup.

**US-009.2** — As a developer preparing to reinstall Windows, I want to export all enabled optional features to a file, so that I can restore them later without having to remember which features were active.

**US-009.3** — As a developer, I want to list all currently activated optional features, so that I can audit my environment before or after making changes.

---

## Acceptance Criteria

| ID | Criterion |
|----|-----------|
| AC-009.1 | `devmaid windowsfeatures list --enabled-only` prints all currently enabled Windows Optional Features, one per line. |
| AC-009.2 | `devmaid windowsfeatures list` (without `--enabled-only`) prints all features with their state (Enabled/Disabled). |
| AC-009.3 | `devmaid windowsfeatures export <path>` writes all enabled features to `<path>` as a JSON file. |
| AC-009.4 | `devmaid windowsfeatures import <path>` reads the JSON file and enables all listed features using `dism.exe`. |
| AC-009.5 | If `dism.exe` is not found, both `export` and `import` exit `3` with an error message. |
| AC-009.6 | During import, each feature is enabled independently; failure on one feature does not abort the remaining features. |
| AC-009.7 | The import command prints a per-feature result (success/failure) and a final summary. |

---

## CLI Interface

```bash
devmaid windowsfeatures list [--enabled-only]
devmaid windowsfeatures export <path>
devmaid windowsfeatures import <path>
```

### Arguments & Options

#### list

| Option | Required | Default | Description |
|--------|----------|---------|-------------|
| `--enabled-only` | No | `false` | Show only enabled features |

#### export

| Argument | Required | Default | Description |
|----------|----------|---------|-------------|
| `<path>` | Yes | — | Output JSON file path |

#### import

| Argument | Required | Default | Description |
|----------|----------|---------|-------------|
| `<path>` | Yes | — | JSON file to import |

### Exit Codes

| Code | Scenario |
|------|----------|
| `0` | Operation completed successfully |
| `1` | File I/O error or partial import failure |
| `2` | Missing required argument |
| `3` | `dism.exe` not found |

---

## Export File Format

```json
{
  "ExportDate": "2026-04-01T10:30:00Z",
  "Features": [
    { "FeatureName": "Microsoft-Windows-Subsystem-Linux", "State": "Enabled" },
    { "FeatureName": "VirtualMachinePlatform", "State": "Enabled" },
    { "FeatureName": "Containers", "State": "Enabled" }
  ]
}
```

---

## Error Scenarios

| Scenario | Expected Behavior |
|----------|------------------|
| `dism.exe` not found | Exit `3`, message: `"dism.exe not found. This command requires Windows with DISM installed."` |
| Export path not writable | Exit `1`, message: `"Cannot write to '<path>'. Check permissions."` |
| Import file not found | Exit `1`, message: `"Import file '<path>' not found."` |
| Feature not available on this Windows edition | Log warning per feature, continue with next |
| Feature requires restart | Log info: `"Some features require a system restart to take effect."` |
| No features enabled | Export writes JSON with empty `Features` array |

---

## Non-Functional Requirements

- Must run only on Windows. If invoked on another OS, exit `1` with message: `"This command requires Windows."`.
- Import must be idempotent — enabling an already-enabled feature must not produce an error.
- Export and list operations must complete in under **5 seconds**.
