# Feature Spec: Claude Code Integration

**ID:** 004  
**Slug:** claude-code-integration  
**Status:** Implemented  
**Version:** 1.0  

---

## Purpose

Automate the installation and configuration of Anthropic's Claude Code CLI tool on Windows, including MCP database integration and Windows-specific environment settings, reducing a multi-step manual setup to a single command.

---

## User Stories

**US-004.1** — As a developer setting up a new Windows machine, I want to install Claude Code with a single command, so that I don't have to look up the winget package name and flags.

**US-004.2** — As a developer, I want to configure Claude Code's MCP database tool automatically, so that Claude can query my local PostgreSQL databases during coding sessions.

**US-004.3** — As a developer, I want to configure Claude's Windows environment settings (shell, permissions), so that Claude runs correctly on Windows without manual JSON editing.

**US-004.4** — As a developer, I want to check whether Claude Code is installed and its current version, so that I know my environment is ready.

---

## Acceptance Criteria

| ID | Criterion |
|----|-----------|
| AC-004.1 | `devmaid claude install` invokes `winget install` for the Claude Code package and exits `0` on success. |
| AC-004.2 | If Claude Code is already installed, `install` skips installation, prints a status message, and exits `0`. |
| AC-004.3 | `devmaid claude status` prints the installed version (if found) or a "not installed" message. |
| AC-004.4 | `devmaid claude settings mcp-database` writes the MCP configuration block to the Claude user settings file. |
| AC-004.5 | `devmaid claude settings win-env` writes the Windows shell and permission settings to the Claude user settings file. |
| AC-004.6 | If winget is not available, `install` exits `3` with instructions to install winget. |
| AC-004.7 | If the Claude settings file does not exist, `settings` sub-commands create it. |

---

## CLI Interface

```bash
devmaid claude install
devmaid claude status
devmaid claude settings mcp-database
devmaid claude settings win-env
```

### Exit Codes

| Code | Scenario |
|------|----------|
| `0` | Operation completed successfully |
| `1` | Installation failed or settings write failed |
| `2` | Invalid subcommand |
| `3` | winget not found |

---

## Error Scenarios

| Scenario | Expected Behavior |
|----------|------------------|
| winget not installed | Exit `3`, print: `"winget is not installed. Install App Installer from the Microsoft Store."` |
| Installation fails (winget error) | Exit `1`, print winget error output |
| Settings file directory missing | Create directory, then write file |
| Already installed | Exit `0`, print current version |

---

## Non-Functional Requirements

- Must run only on Windows. If invoked on another OS, exit `1` with message: `"This command requires Windows."`.
- Settings modifications must be idempotent — running the same settings command twice must not duplicate entries.
