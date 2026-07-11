# CI/CD policy pipeline — enforce, fix, and learn

A self-improving coding-policy pipeline for the CLAUDE.md conventions. It does three things:

- **Enforce** — block a PR that violates policy.
- **Fix** — bring a violating change back into conformance automatically.
- **Learn** — distil every catch back into a policy registry that upstream authors read, and
  promote recurring judgment catches into cheap deterministic checks, so the catch-point moves
  **upstream over time** (ideally to authoring time, before CI ever sees the violation).

Design principle: **don't spend a non-deterministic model on what a script can prove.** Each policy
is enforced at the cheapest tier that can express it; the AI is reserved for the judgment-based
residue; and the pipeline continuously tries to make itself unnecessary by pushing enforcement left.

```
                   ┌──────────────────────────────────────────────┐
  AUTHOR TIME      │ bld/policy/POLICIES.md   (source of truth)    │
  (upstream)       │  · read by authoring agents (CLAUDE.md links) │◀───┐
                   │  · enforced by bld/hooks/pre-push at push time │    │
                   └──────────────────────────────────────────────┘    │  LEARN:
        │ git push  →  pre-push runs check-policy.sh (caught here ideally)  append lesson;
        ▼                                                                │  promote Tier-2 → Tier-1
  CI GATE                                                                │
    ├─ Build and Test        (build-test.yml)         functional — required
    ├─ Policy Gate           (policy-gate.yml)         Tier-1 deterministic — required, blocking
    └─ Claude Policy Review  (claude-policy-review.yml) Tier-2 judgment — required, blocking
        │ any policy check fails                                         │
        ▼                                                                │
  FIX + LEARN                                                            │
    └─ Claude Policy Fix     (claude-policy-fix.yml) ─ fixes the violation, pushes,
                                                       and writes what it learned ──┘
```

## The tiers

| Tier | Enforcer | What it checks | Cost / determinism |
|---|---|---|---|
| **0** | Roslyn analyzers in-build (`EnforceCodeStyleInBuild`, `WarningsAsErrors` for the doc-comment family) | file-scoped ns, `var` cascade, missing XML doc (CS1591) | free, deterministic |
| **1** | **Policy Gate** — `check-folder-namespace-alignment.sh` + `check-policy.sh` | folder↔namespace; banner; file-scoped ns on new files; no literal exception messages | free, deterministic |
| **2** | **Claude Policy Review** — the AI agent | test partial placement; bespoke-vs-shared KAT; pass/fail split; doc summaries; `<returns>` on properties; resx key taxonomy; validation grouping | per-PR model cost, judgment |

Every policy has a stable ID (`BODU-P001` …) in `bld/policy/POLICIES.md`, so the checker, the
reviewer, the pre-push hook, and the fix-and-learn loop all refer to the same rule.

## The self-learning loop

This is the part that makes it more than a gate:

1. **Author time (shift-left).** `bld/hooks/pre-push` runs the deterministic Tier-1 checks over the
   range being pushed. An authoring session — human or agent — sees a violation *at push time* and
   fixes it before CI. Enabled with `git config core.hooksPath bld/hooks`; the Claude Code on the
   web SessionStart hook does this automatically in the remote environment.
2. **Catch.** Anything that slips through is blocked by Policy Gate (Tier-1) or Claude Policy Review
   (Tier-2).
3. **Fix + learn.** `claude-policy-fix.yml` fixes the violation, then writes a **Learning log** entry
   into `POLICIES.md` describing the anti-pattern and its conforming form. Upstream authors read that
   registry, so the same mistake is less likely next time.
4. **Promote.** When a fixed violation is deterministically checkable but only Tier-2 caught it, the
   fixer **adds a rule to `check-policy.sh`** — turning a judgment into a script. The catch migrates
   from Tier-2 (paid, in CI) to Tier-1 (free, at push). The registry records the promotion.

The flywheel: judgment → script → push-time. Enforcement gets cheaper and earlier the more the
pipeline runs.

## One-time setup

1. **Secrets:** `ANTHROPIC_API_KEY` (or `CLAUDE_CODE_OAUTH_TOKEN`, swapping the input). The
   fix-and-learn arm additionally needs a **GitHub App** (`CLAUDE_APP_ID`, `CLAUDE_APP_PRIVATE_KEY`,
   with Contents + Pull requests: write) — its push must re-trigger the checks, and a `GITHUB_TOKEN`
   push does not, so an App (or PAT) is required or the gate deadlocks against branch protection.
2. **Branch protection:** mark **Build and Test**, **Policy Gate**, and **Claude Policy Review** as
   required status checks. Keep auto-merge off — a human still reviews and merges.
3. **Local devs (optional but recommended):** `git config core.hooksPath bld/hooks` to get the same
   author-time enforcement the remote agent gets.

## Why diff-scoped

`check-policy.sh` judges only **added files and added lines**, not the whole tree, because a mature
tree carries grandfathered stragglers (7 `TestHelpers*.cs` without banners; 3 literal exception
messages in `Bodu.Text.Yaml`). A tree-wide blocking check would fail every PR until those are
cleaned; diff-scoping stops the bleeding now and fixes stragglers when their lines are next touched.
The folder↔namespace script *is* whole-tree — the tree is already clean under it.

## Failure modes handled

- **Tier-2 infra hiccup** (no/invalid findings file) fails **open** with a warning — a flaky agent
  run never wedges the merge queue; only real findings block.
- **Fork PRs** don't run the AI review or the fixer (no secret access) and are excluded by guard.
- **Runaway fixing** is bounded by `MAX_ATTEMPTS` (default 3) per PR, auto-reset when a human pushes.
- **False-positive blocking** is minimized by diff-scoping (Tier 1) and the conservative,
  high-confidence prompt (Tier 2).

## Adding a policy

1. Add a row to the table in `bld/policy/POLICIES.md` with a new `BODU-Pxxx` ID.
2. If deterministic: add a diff-scoped check to `bld/check-policy.sh` and **prove it is
   false-positive-free against the current tree** before it goes blocking. If judgment: add it to
   the `claude-policy-review.yml` prompt.
3. Document the prose rule in `CLAUDE.md` as usual.
