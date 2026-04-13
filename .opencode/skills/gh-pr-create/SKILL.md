---
name: gh-pr-create
description: Create a GitHub pull request using the gh CLI. Infers title, body, base branch, and head branch from git context and commit history.
license: MIT
compatibility: Requires gh CLI authenticated and a git repository with a remote on GitHub.
metadata:
  author: furlab
  version: "1.0"
---

Create a GitHub pull request using the `gh` CLI.

---

## Steps

### 1. Gather git context

Run all of these in parallel:

```bash
git status
git log --oneline <base>..<head>
git diff <base>...<head> --stat
git branch --show-current
gh repo view --json defaultBranchRef --jq '.defaultBranchRef.name'
```

From this, determine:
- **head**: current branch
- **base**: default branch (usually `main` or `master`) — or use what the user specified
- **commits**: full list of commits being introduced by this PR
- **files changed**: summary of affected files

### 2. Check for an existing PR

```bash
gh pr list --head <head> --json number,url,title --limit 1
```

If a PR already exists for this branch, inform the user and ask if they want to:
- Update the existing PR (edit title/body with `gh pr edit`)
- Cancel

Do NOT create a duplicate.

### 3. Draft title and body

**Title**: Derive from the commits. If there is one commit, use its message. If there are multiple, write a concise summary of the set (e.g., `feat: multi-server query execution and SettingsCommand`). Follow the existing commit message style of the repo (check with `git log --oneline -10`).

**Body**: Write in Markdown. Structure:

```markdown
## Summary

- Bullet points describing WHAT changed and WHY — one per logical area of change
- Focus on user-visible behavior and architectural decisions, not file names

## Test coverage

- What tests were added/changed and what they cover
- Test count if notable (e.g., "140 tests passing")
```

Rules for the body:
- Keep it factual and concise — no filler phrases
- Do NOT list every file changed; describe intent and impact
- If the diff is large (>20 files), group changes by theme
- If there is a `pr.md` at the repo root, use it as the source of truth for the body content — do not rewrite from scratch

### 4. Write body to a temp file

PowerShell does not support bash heredocs. Always write the body to a temp file and pass it via `--body-file`:

```powershell
$body = @"
<body content here>
"@
$body | Out-File -FilePath "$env:TEMP\pr_body.md" -Encoding utf8
```

### 5. Create the PR

```bash
gh pr create \
  --title "<title>" \
  --body-file "$env:TEMP\pr_body.md" \
  --base <base> \
  --head <head>
```

Do NOT use `--body` with inline string — it breaks in PowerShell due to quoting rules.

### 6. Return the PR URL

Output the URL returned by `gh pr create` so the user can open it directly.

---

## Edge Cases

| Situation | Action |
|---|---|
| Branch not pushed to remote | Run `git push -u origin <head>` first |
| `pr.md` exists at repo root | Use it as the source for the body |
| User specifies a base branch | Use it instead of the default |
| User specifies a title | Use it verbatim, skip title derivation |
| PR already exists for this branch | Ask: update or cancel — never duplicate |
| `gh` not authenticated | Show: `gh auth login` and stop |

---

## Guardrails

- NEVER force-push to the remote
- NEVER create a PR targeting `main` from `main`
- Always write body via `--body-file` (never `--body` with inline string in PowerShell)
- If the branch has no commits ahead of base, stop and inform the user
- Do not invent test counts or file lists — only report what git/gh actually returns
