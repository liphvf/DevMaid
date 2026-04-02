# Feature Spec: Project Cleaner (.NET Clean)

**ID:** 008  
**Slug:** project-cleaner  
**Status:** Implemented  
**Version:** 1.0  

---

## Purpose

Free disk space and resolve .NET build cache corruption by recursively deleting all `bin` and `obj` output directories from a project tree — a common developer task that is tedious to perform manually across large solutions.

---

## User Stories

**US-008.1** — As a .NET developer, I want to clean all `bin` and `obj` folders from my solution with a single command, so that I can resolve build cache issues without navigating each project directory.

**US-008.2** — As a developer, I want to target a specific directory for cleaning, so that I can clean a project without changing my current working directory.

**US-008.3** — As a developer, I want to see how much disk space was freed, so that I know the clean operation was effective.

---

## Acceptance Criteria

| ID | Criterion |
|----|-----------|
| AC-008.1 | `devmaid clean` recursively deletes all `bin` and `obj` directories found under the current working directory. |
| AC-008.2 | `devmaid clean <path>` recursively deletes `bin` and `obj` directories under `<path>`. |
| AC-008.3 | After cleaning, the tool prints the number of directories deleted and the total disk space freed (in MB). |
| AC-008.4 | If no `bin` or `obj` directories are found, the tool exits `0` with message: `"Nothing to clean."` |
| AC-008.5 | If a directory cannot be deleted (e.g., files are locked), the tool logs a warning for that path and continues with the rest. |
| AC-008.6 | The provided path must exist; if it does not, exit `1` with a descriptive error. |

---

## CLI Interface

```bash
devmaid clean [<path>]
```

### Arguments

| Argument | Required | Default | Description |
|----------|----------|---------|-------------|
| `<path>` | No | `./` (current directory) | Root directory to clean |

### Exit Codes

| Code | Scenario |
|------|----------|
| `0` | Clean completed (even if nothing was found) |
| `1` | Specified path does not exist |
| `1` | Partial failure (some directories could not be deleted) |

---

## Output Example

```
Scanning: C:\Projects\MySolution
Found 12 directories to clean (bin: 8, obj: 4)

  Deleted: C:\Projects\MySolution\ProjectA\bin
  Deleted: C:\Projects\MySolution\ProjectA\obj
  Deleted: C:\Projects\MySolution\ProjectB\bin
  ...
  WARNING: Could not delete C:\Projects\MySolution\ProjectC\bin (files are in use)

Clean complete.
  Directories deleted: 11 / 12
  Disk space freed: 340.2 MB
```

---

## Error Scenarios

| Scenario | Expected Behavior |
|----------|------------------|
| Path does not exist | Exit `1`, message: `"Path '<path>' does not exist."` |
| Insufficient permissions | Log per-directory warning, continue with others |
| Files locked (e.g., running process) | Log per-directory warning, continue with others |
| Nothing to clean | Exit `0`, message: `"Nothing to clean in '<path>'."` |

---

## Non-Functional Requirements

- Must complete in under **10 seconds** for solutions with up to 50 projects.
- Must not follow symbolic links to avoid unintended deletions outside the target tree.
