# DevMaid — Spec Index

This document is the master index of all feature specifications for DevMaid. Each spec follows the [Spec-Driven Development](https://github.com/github/spec-kit) methodology and is governed by the [Project Constitution](../CONSTITUTION.md).

---

## Active Features

| ID | Slug | Name | Status | Spec |
|----|------|------|--------|------|
| 001 | `table-parser` | Table Parser | Implemented | [spec.md](./001-table-parser/spec.md) |
| 002 | `database-utilities` | Database Utilities (Backup & Restore) | Implemented | [spec.md](./002-database-utilities/spec.md) |
| 003 | `file-combine` | File Utilities — Combine | Implemented | [spec.md](./003-file-combine/spec.md) |
| 004 | `claude-code-integration` | Claude Code Integration | Implemented | [spec.md](./004-claude-code-integration/spec.md) |
| 005 | `opencode-integration` | OpenCode Integration | Implemented | [spec.md](./005-opencode-integration/spec.md) |
| 006 | `winget-package-manager` | Winget Package Manager | Implemented | [spec.md](./006-winget-package-manager/spec.md) |
| 007 | `sql-query-csv-export` | SQL Query & CSV Export | Implemented | [spec.md](./007-sql-query-csv-export/spec.md) |
| 008 | `project-cleaner` | Project Cleaner (.NET Clean) | Implemented | [spec.md](./008-project-cleaner/spec.md) |
| 009 | `windows-features-manager` | Windows Features Manager | Implemented | [spec.md](./009-windows-features-manager/spec.md) |

---

## Planned Features

| ID | Slug | Name | Status | Spec |
|----|------|------|--------|------|
| 010 | `gui-electron-angular` | GUI — Electron + Angular Interface | Planned (Draft) | [spec.md](./010-gui-electron-angular/spec.md) |

---

## Spec Lifecycle

```
Draft → Review → Approved → Implemented → Deprecated
```

| State | Meaning |
|-------|---------|
| **Draft** | Work in progress; open questions remain (`[NEEDS CLARIFICATION]`) |
| **Review** | Ready for review; all questions resolved |
| **Approved** | Accepted; implementation may begin |
| **Implemented** | Feature is shipped and matches the spec |
| **Deprecated** | Feature removed or superseded |

---

## Adding a New Spec

1. Assign the next sequential ID (e.g., `011`)
2. Create `docs/specs/<ID>-<slug>/spec.md` using the template structure below
3. Add an entry to this index
4. Mark status as **Draft** until all `[NEEDS CLARIFICATION]` markers are resolved

### Spec Template Structure

```markdown
# Feature Spec: <Name>

**ID:** <NNN>
**Slug:** <slug>
**Status:** Draft | Review | Approved | Implemented | Deprecated
**Version:** <semver>

## Purpose
## User Stories
## Acceptance Criteria
## CLI Interface
## Error Scenarios
## Non-Functional Requirements
```
