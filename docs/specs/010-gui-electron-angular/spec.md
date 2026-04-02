# Feature Spec: GUI — Electron + Angular Interface

**ID:** 010  
**Slug:** gui-electron-angular  
**Status:** Planned  
**Version:** 0.1 — DRAFT  

---

## Purpose

Provide a modern graphical desktop interface for DevMaid's capabilities, targeting developers who prefer a visual workflow for tasks like database backup/restore, query execution, and environment management — while preserving full CLI parity.

---

## User Stories

**US-010.1** — As a developer who prefers GUI tools, I want to perform database backups and restores through a visual form, so that I don't have to remember CLI flags and options.

**US-010.2** — As a developer, I want to see real-time progress feedback during long-running operations (backup, restore, query execution) in the GUI, so that I know the operation is progressing.

**US-010.3** — As a developer, I want to manage server configurations (host, port, credentials) through a GUI settings screen, so that I don't have to manually edit `appsettings.json`.

**US-010.4** — As a developer, I want to launch the DevMaid GUI with a single command (`devmaid gui`) from the terminal, so that I can switch between CLI and GUI seamlessly.

**US-010.5** — As a developer, I want all GUI features to remain accessible from the CLI, so that automation and scripting workflows are never broken.

---

## Acceptance Criteria

| ID | Criterion |
|----|-----------|
| AC-010.1 | `devmaid gui` launches the Electron desktop application and exits `0`. |
| AC-010.2 | The GUI exposes all features covered by the CLI: database, query, winget, file, claude, opencode, clean, windowsfeatures. |
| AC-010.3 | Long-running operations display a progress bar updated in real time via SignalR. |
| AC-010.4 | The configuration screen reads from and writes to `appsettings.json` using the same format as the CLI. |
| AC-010.5 | The GUI communicates with a `DevMaid.Api` REST + SignalR backend started automatically when the GUI launches. |
| AC-010.6 | Closing the GUI also terminates the background API server. |
| AC-010.7 | The GUI ships as a distributable Windows installer (NSIS). |

---

## CLI Interface

```bash
devmaid gui
```

### Exit Codes

| Code | Scenario |
|------|----------|
| `0` | GUI launched and closed normally |
| `1` | API server failed to start |
| `1` | GUI binary not found |

---

## Architecture Overview

```
User
 └── devmaid gui (CLI)
      ├── Starts DevMaid.Api (ASP.NET Core + SignalR) in background
      └── Launches DevMaid.Gui (Electron)
           └── Angular App (HTTP + SignalR) ──► DevMaid.Api
                                                 └── DevMaid.Core (business logic)
```

### Key Technical Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Frontend framework | Angular 18+ | Type-safe, component-driven, well-suited for form-heavy UIs |
| Desktop shell | Electron | Cross-platform, integrates well with web technologies |
| API transport | REST + SignalR | REST for operations, SignalR for real-time progress |
| UI component library | Angular Material | Consistent design, accessible, well-maintained |

---

## Phased Rollout

This feature is delivered in phases. Each phase must be independently releasable.

| Phase | Scope | Status |
|-------|-------|--------|
| 1 | Core layer extraction (`DevMaid.Core`) | [NEEDS CLARIFICATION: current state] |
| 2 | CLI refactoring to use Core layer | [NEEDS CLARIFICATION: current state] |
| 3 | `DevMaid.Api` REST + SignalR backend | Planned |
| 4 | Angular frontend (all feature screens) | Planned |
| 5 | Electron integration and packaging | Planned |
| 6 | `devmaid gui` hybrid CLI command | Planned |

---

## Constraints

- The GUI is Windows-only for the initial release (following the CLI's platform scope).
- The GUI must never expose functionality that is not also accessible via CLI.
- No new business logic may be introduced in the GUI layer — all logic lives in `DevMaid.Core`.

---

## Open Questions

- [NEEDS CLARIFICATION: Should the API server bind to localhost only, or support configurable bind address?]
- [NEEDS CLARIFICATION: Is authentication required for the API (e.g., token-based), or is localhost-only binding sufficient?]
- [NEEDS CLARIFICATION: Should the GUI remember the last-used configuration per command, or always start fresh?]
- [NEEDS CLARIFICATION: What is the target Windows version minimum for Electron packaging?]

---

## Non-Functional Requirements

- GUI startup (from `devmaid gui` to interactive window) must complete in under **5 seconds** on target hardware.
- The Angular application must score ≥ 90 on Lighthouse accessibility audit.
- The packaged installer must be under 200 MB.
