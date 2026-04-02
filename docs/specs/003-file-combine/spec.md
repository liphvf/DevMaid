# Feature Spec: File Utilities — Combine

**ID:** 003  
**Slug:** file-combine  
**Status:** Implemented  
**Version:** 1.0  

---

## Purpose

Allow developers to merge multiple text-based files matching a glob pattern into a single output file, preserving content order and encoding. Primarily used for consolidating SQL scripts, log files, and other plain-text artifacts.

---

## User Stories

**US-003.1** — As a developer, I want to combine all `.sql` files in a directory into one file, so that I can run a migration bundle as a single script.

**US-003.2** — As a developer, I want the combined file to respect alphabetical order, so that file sequence is predictable and deterministic.

**US-003.3** — As a developer, I want a default output file name to be created when I don't specify `--output`, so that I don't have to think about naming for quick operations.

---

## Acceptance Criteria

| ID | Criterion |
|----|-----------|
| AC-003.1 | Given a valid glob pattern matching at least one file, the tool creates an output file containing the concatenated content of all matched files. |
| AC-003.2 | Files are combined in **case-insensitive alphabetical order** by filename. |
| AC-003.3 | When `--output` is not specified, the output file is named `CombineFiles.<ext>` (using the extension of the matched files) in the same directory as the matched files. |
| AC-003.4 | The output is written in **UTF-8** encoding, regardless of input file encoding. |
| AC-003.5 | If the output file already exists, it is **overwritten** without confirmation. |
| AC-003.6 | When no files match the pattern, the tool exits with code `1` and prints `"No files matching '<pattern>' were found."` |
| AC-003.7 | When the pattern is empty or missing, the tool exits with code `2` and prints a usage hint. |

---

## CLI Interface

```bash
devmaid file combine --input <pattern> [--output <file>]
```

### Options

| Option | Short | Required | Default | Description |
|--------|-------|----------|---------|-------------|
| `--input` | `-i` | Yes | — | Glob pattern for input files (e.g., `C:\temp\*.sql`) |
| `--output` | `-o` | No | `CombineFiles.<ext>` | Output file path |

### Exit Codes

| Code | Scenario |
|------|----------|
| `0` | Files combined successfully |
| `1` | No files matched the pattern |
| `2` | Invalid or empty pattern |

---

## Error Scenarios

| Scenario | Expected Behavior |
|----------|------------------|
| Empty `--input` | Exit `2`, message: `"Input pattern is required."` |
| Invalid pattern syntax | Exit `2`, message: `"Input pattern '<pattern>' is invalid."` |
| No files match | Exit `1`, message: `"No files matching '<pattern>' were found."` |
| Output path inaccessible | Exit `1`, message: `"Cannot write to '<path>'. Check permissions."` |

---

## Non-Functional Requirements

- Must handle files of any size without loading all content into memory at once (streaming write).
- Must complete in under **5 seconds** for up to 500 files of average 100 KB each.
