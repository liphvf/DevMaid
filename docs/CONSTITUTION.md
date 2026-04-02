# DevMaid — Project Constitution

> **Version:** 1.0  
> **Date:** April 2026  
> **Status:** Active  
> **Author:** Filiphe Vilar Figueiredo

---

## Preamble

DevMaid is a .NET-based CLI tool whose purpose is to automate and simplify recurring development tasks. This constitution establishes the governing principles and development guidelines that every feature, architectural decision, and contribution must respect. It is the single source of truth for *how* the project is built — independently of which technology, framework version, or team member is involved.

Specifications define the **what** and the **why**. This constitution governs the **how**.

---

## Article I — Purpose & Scope

### I.1 Mission

DevMaid exists to eliminate repetitive friction in developer workflows — particularly around databases, file operations, Windows package management, and AI tool configuration — through a unified, composable CLI interface.

### I.2 Target User

The primary user is a Windows developer who:
- Works daily with PostgreSQL databases
- Manages Windows environments and packages
- Uses AI coding assistants (Claude Code, OpenCode)
- Values productivity and automation over manual steps

### I.3 Out of Scope

The following are explicitly out of scope unless a formal spec is approved:
- Web-only features with no CLI equivalent
- Non-Windows-primary functionality in the near term
- Business logic that belongs in a separate domain tool

---

## Article II — Architecture Principles

### II.1 CLI-First

DevMaid's primary interface is the CLI. Every feature **must** be fully operable via the command line. GUI surfaces (when they exist) are secondary projections of CLI-accessible functionality.

### II.2 Modular Command Architecture

Each capability **must** be implemented as an isolated command module:

```
Commands/
├── <Feature>Command.cs     ← CLI binding
├── CommandOptions/
│   └── <Feature>Options.cs ← Strongly-typed DTOs
└── Services/
    └── <Feature>Service.cs ← Business logic
```

Business logic must never live inside the `Command` class. Commands are thin wrappers that parse input, delegate to services, and report output.

### II.3 Layered Project Structure

Projects are organized as follows (in order of dependency):

| Project | Responsibility |
|---------|----------------|
| `DevMaid.Core` | All business logic, services, interfaces, models |
| `DevMaid.CLI` | CLI parsing, command definitions, output formatting |
| `DevMaid.Api` | REST/SignalR API (future GUI bridge) |
| `DevMaid.Gui` | Electron + Angular GUI (future) |
| `DevMaid.Tests` | All test projects |

> **Rule:** Higher-level projects may depend on lower-level ones. The inverse is forbidden.

### II.4 Interface-Driven Design

Every service **must** be defined by an interface in `DevMaid.Core/Interfaces/`. Concrete implementations are in `DevMaid.Core/Services/`. This enables testing without infrastructure and future substitution.

### II.5 No Shell Execution

Processes spawned by DevMaid **must** use `UseShellExecute = false`. Output must be captured via `RedirectStandardOutput` and `RedirectStandardError`. No command shall invoke `cmd.exe` or `powershell.exe` as a shell wrapper.

---

## Article III — Command Design Standards

### III.1 Command Naming

Commands use `kebab-case` nouns and verbs following this pattern:

```
devmaid <noun> <verb> [arguments] [options]
devmaid <noun> [options]        ← for single-action commands
```

Examples:
- `devmaid database backup`
- `devmaid query run`
- `devmaid winget restore`
- `devmaid clean`

### III.2 Option Conventions

| Convention | Rule |
|-----------|------|
| Short flags | Single character, `-x` |
| Long flags | Full word, `--option-name` |
| Required options | Must be documented; use argument when positional is cleaner |
| Password options | Never required; prompt interactively if not provided |
| Output paths | Default to current directory or sensible convention when omitted |

### III.3 Exit Codes

| Code | Meaning |
|------|---------|
| `0` | Success |
| `1` | General error |
| `2` | Invalid arguments / missing required option |
| `3` | External dependency not found (e.g., psql, pg_dump) |

### III.4 Output Standards

- Progress feedback is written to **stdout** during long operations
- Errors are written to **stderr**
- CSV/file outputs go to the path specified by `--output`
- No ANSI color codes that are not guarded by a terminal-detection check

---

## Article IV — Configuration Management

### IV.1 Configuration Sources (Precedence, highest first)

1. Command-line options
2. Environment variables
3. `appsettings.<environment>.json`
4. `appsettings.json`
5. User secrets (development only)

### IV.2 Sensitive Data

- Passwords are **never** stored in `appsettings.json` committed to version control
- Passwords must be prompted interactively when not provided via CLI flag
- User secrets are acceptable for local development
- For CI/CD and production environments, environment variables are required

### IV.3 appsettings.json Structure

The configuration file must follow the established schema. New configuration sections require a documented `[NEEDS CLARIFICATION]` pass before implementation.

---

## Article V — Error Handling

### V.1 Errors Must Be Actionable

Every error message shown to the user must:
1. State **what** failed (specific resource, operation, or parameter)
2. Suggest **what** the user should do to resolve it

Bad: `"Error: connection failed"`  
Good: `"Connection to postgres@localhost:5432 failed. Check that PostgreSQL is running and credentials are correct."`

### V.2 No Silent Failures

Operations that partially succeed must report both the successful and failed parts. Never return exit code `0` when any part of the requested work failed.

### V.3 External Dependency Errors

When an external binary (e.g., `psql`, `pg_dump`, `winget`) is not found, the error must:
- Name the missing binary
- Provide installation instructions or a link to them
- Exit with code `3`

---

## Article VI — Testing

### VI.1 Test-First on Core Logic

Business logic in `DevMaid.Core` must have unit tests written **before** or **alongside** implementation. No service method may be merged to main without at least one passing unit test covering the primary success path.

### VI.2 Integration Tests for External Dependencies

Operations that touch the filesystem, PostgreSQL, or external processes must have integration tests that run against real (or containerized) infrastructure. Mocks are acceptable in unit tests but must not replace integration tests.

### VI.3 Test Project Structure

```
DevMaid.Tests/
├── Core/           ← Unit tests for Core services
├── CLI/            ← Unit tests for command parsing
└── Integration/    ← Integration tests (database, file system, processes)
```

### VI.4 Test Naming Convention

```
<MethodName>_<StateUnderTest>_<ExpectedBehavior>
BackupAsync_WithValidOptions_ShouldCreateDumpFile
BackupAsync_WithInvalidHost_ShouldThrowConnectionException
```

---

## Article VII — Simplicity Gate

### VII.1 No Speculative Architecture

Features must not introduce abstractions, layers, or patterns for hypothetical future use cases. Every design decision must be justified by a current, documented requirement.

### VII.2 Maximum Complexity Budget

New features may introduce at most **one new project** to the solution. Introduction of additional projects requires explicit approval and a documented rationale in the feature spec.

### VII.3 Dependency Discipline

Before adding a new NuGet package:
1. Verify no existing package already covers the need
2. Verify it is actively maintained (last release < 18 months)
3. Document the rationale in the feature spec's "Technical Decisions" section

---

## Article VIII — Cross-Cutting Concerns

### VIII.1 Logging

All operations **must** use the `ILogger` interface from `DevMaid.Core`. Console output for the user is distinct from structured logging for diagnostics.

### VIII.2 Progress Reporting

Long-running operations (> 2 seconds expected) **must** emit incremental progress via `IProgress<OperationProgress>`. Commands display this progress on the terminal. Future API/GUI layers consume it via SignalR.

### VIII.3 Cancellation

All async operations **must** accept and respect `CancellationToken`. CLI commands should bind `CancellationToken` to `Ctrl+C`.

### VIII.4 Security

- Input paths must be validated against path-traversal attacks using `SecurityUtils.IsValidPath()`
- PostgreSQL identifiers must be validated before interpolation into any query
- Passwords must never appear in logs, stack traces, or CLI help text

---

## Article IX — Documentation

### IX.1 Docs Location

| Document | Location |
|---------|----------|
| Architecture | `docs/en/ARCHITECTURE.md` |
| Feature Specifications | `docs/specs/<NNN>-<slug>/spec.md` |
| Implementation Plans | `docs/specs/<NNN>-<slug>/plan.md` |
| Command Reference | `docs/en/FEATURE_SPECIFICATION.md` |
| This Constitution | `docs/CONSTITUTION.md` |

### IX.2 Feature Spec Requirement

Every new feature **must** have a spec file in `docs/specs/` before any implementation begins. The spec defines:
- Purpose and user stories
- Acceptance criteria
- Error scenarios
- CLI interface contract (options, exit codes)

### IX.3 Documentation Language

- Primary documentation: **English** (`docs/en/`)
- Secondary documentation: **Portuguese (pt-BR)** (`docs/pt-BR/`)
- Specs and constitution: **English only**

---

## Article X — Amendment Process

This constitution may be amended when:
1. A recurring implementation problem reveals an underspecified principle
2. A new architectural direction (e.g., GUI, cross-platform) requires new governing rules
3. A majority of active contributors agree the change improves quality or clarity

Amendments must:
- State the **rationale** for the change
- Update the **version** and **date** of this document
- Be reviewed in a pull request before merging

---

## Glossary

| Term | Definition |
|------|-----------|
| CLI | Command-Line Interface |
| DTO | Data Transfer Object |
| MCP | Model Context Protocol |
| SDD | Specification-Driven Development |
| Winget | Windows Package Manager |
| Feature Spec | A `spec.md` file in `docs/specs/` describing a single feature |
| Constitution | This document — the governing principles of DevMaid |
