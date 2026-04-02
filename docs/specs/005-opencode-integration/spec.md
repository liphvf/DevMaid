# Feature Spec: OpenCode Integration

**ID:** 005  
**Slug:** opencode-integration  
**Status:** Implemented  
**Version:** 1.0  

---

## Purpose

Automate the installation and configuration of the OpenCode CLI tool, enabling developers to set up their OpenCode environment with a single DevMaid command instead of navigating external documentation.

---

## User Stories

**US-005.1** — As a developer, I want to install OpenCode through DevMaid, so that I don't have to find and follow external installation instructions.

**US-005.2** — As a developer, I want to check whether OpenCode is installed and what version is active, so that I can verify my environment is ready.

**US-005.3** — As a developer, I want to configure OpenCode through DevMaid, so that I can apply standard settings for my workflow without editing config files manually.

---

## Acceptance Criteria

| ID | Criterion |
|----|-----------|
| AC-005.1 | `devmaid opencode install` installs OpenCode via the available package manager and exits `0` on success. |
| AC-005.2 | If OpenCode is already installed, `install` skips installation, prints the installed version, and exits `0`. |
| AC-005.3 | `devmaid opencode status` prints the installed version or `"OpenCode is not installed."` |
| AC-005.4 | `devmaid opencode config` applies the default DevMaid-recommended OpenCode configuration. |
| AC-005.5 | If OpenCode is not found in PATH after installation, the tool prints a PATH update suggestion. |

---

## CLI Interface

```bash
devmaid opencode install
devmaid opencode status
devmaid opencode config
```

### Exit Codes

| Code | Scenario |
|------|----------|
| `0` | Operation completed successfully |
| `1` | Installation or configuration failed |
| `2` | Invalid subcommand |
| `3` | Required package manager not found |

---

## Error Scenarios

| Scenario | Expected Behavior |
|----------|------------------|
| Installation fails | Exit `1`, print error from package manager |
| Already installed | Exit `0`, print version info |
| Not found in PATH after install | Exit `0` with warning: `"OpenCode installed but not found in PATH. You may need to restart your terminal or update your PATH."` |
| Config file missing | Create config file with defaults |

---

## Non-Functional Requirements

- Installation must be idempotent — running `install` multiple times must not create side effects.
- `status` must complete in under **1 second**.
