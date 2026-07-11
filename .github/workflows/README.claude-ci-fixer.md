# Claude CI Fixer

An autonomous agent that closes the loop your `build-test.yml` already opens: when CI
goes red on a pull request, it launches Claude Code to diagnose and fix the failure,
commits the fix, and lets CI re-judge — repeating until the gate is green or a bounded
attempt cap is hit.

## The two-level loop

```
 commit / PR ─▶ Build and Test (build-test.yml)  ◀─────────────────┐
                        │                                           │
                 green ─┴─ red                                      │ app-token push
                        │                                           │ re-triggers CI
                        ▼                                           │
              Claude CI Fixer (this workflow)                       │
                 • bounded attempt check                            │
                 • pack local analyzer feed                         │
                 • Claude Code: build → test → fix → commit ────────┘
                        │
                 cap hit ─▶ escalate to human (state comment on PR)
```

- **Inner loop (one run):** Claude Code iterates `build → test → edit` internally until
  `dotnet test --settings test.runsettings` passes or it runs out of turns.
- **Outer loop (across runs):** each fixer push re-runs Build and Test. Still red → the
  fixer fires again, up to `MAX_ATTEMPTS`.
- **The judge is never the agent.** Success is the deterministic `test.runsettings` gate
  plus your branch-protection required check. Green tests still get a human merge.

## One-time setup

1. **Create a GitHub App** (or reuse the Claude GitHub App) installed on this repo with
   **Contents: write** and **Pull requests: write**. Add its credentials as secrets:
   - `CLAUDE_APP_ID`
   - `CLAUDE_APP_PRIVATE_KEY`

   > Why an App and not `GITHUB_TOKEN`? A push made with the default `GITHUB_TOKEN` does
   > **not** trigger new workflow runs. If the fixer pushed with it, Build and Test would
   > never re-run, the new commit would carry no status check, and branch protection would
   > deadlock. The App token (or a PAT) is what makes the fix re-validate. A fine-grained
   > PAT with the same scopes works too — swap the `app-token` step for the PAT secret.

2. **Add the Anthropic credential** as a secret: `ANTHROPIC_API_KEY`
   (or `CLAUDE_CODE_OAUTH_TOKEN` — then swap the input in the workflow).

3. **Make `Build and Test` a required status check** in branch protection for your PR
   target branches. This is the real gate — red code cannot merge no matter how optimistic
   the agent is.

4. **Keep auto-merge off.** The fixer produces a green branch; a human reviews the diff and
   merges.

## Guardrails baked in

| Concern | Mitigation |
|---|---|
| Runaway cost / infinite loops | `MAX_ATTEMPTS` (default 3) per PR. At the cap it posts an escalation comment and stops. |
| Stale counter across real work | Any commit **not** tagged `[claude-ci-fixer]` (i.e. a human push) resets the counter. |
| Prompt injection from forks | Only same-repo PR branches run — fork PRs are excluded entirely. |
| Self-satisfying "fixes" | The prompt forbids deleting/`[Ignore]`-ing tests, loosening assertions, or suppressing analyzers. The human reviewing the diff is the backstop. |
| Touching sensitive infra | The agent is told not to edit `.github/workflows/**`, signing files, `*.snk`, `NuGet.config`, or `global.json`. |
| Overlapping runs on one branch | `concurrency` group per head branch, `cancel-in-progress: false`. |

## Tuning

- **Attempt cap:** change `MAX_ATTEMPTS` in the workflow `env`.
- **Turn budget:** change `--max-turns` in `claude_args`.
- **Gate tier:** the prompt targets `test.runsettings` to match `build-test.yml`. Point it
  at `bvt.runsettings` for a cheaper/faster loop, or `regression.runsettings` for a
  stricter (slower) one — but keep it aligned with whatever CI actually enforces.

## What it deliberately does NOT do

- It does not merge. It does not approve. It does not touch CI or release infrastructure.
- It does not run on fork PRs.
- It does not act as a code reviewer — this is the **fixer**. A read-only reviewer agent on
  green PRs is a separate, additive workflow if you want one.
