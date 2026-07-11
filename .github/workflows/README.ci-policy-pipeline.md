# CI/CD policy pipeline

These workflows enforce the CLAUDE.md coding policies as commits flow through the pipeline.
The design principle is **don't spend a non-deterministic model on what a script can prove** —
so policies are enforced at the cheapest tier that can express them, and the AI is reserved for
the genuinely judgment-based residue.

```
 PR ─▶ Build and Test          (build-test.yml)        functional gate — required
    ─▶ Policy Gate             (policy-gate.yml)        Tier-1 deterministic — required, blocking
    ─▶ Claude Policy Review    (claude-policy-review.yml) Tier-2 judgment — required, blocking
         (optional remediation) Claude CI Fixer         (claude-ci-fixer.yml)  see its own README
```

## The three tiers

| Tier | Enforcer | What it checks | Cost / determinism |
|---|---|---|---|
| **0** | Roslyn analyzers in-build (`EnforceCodeStyleInBuild`, `WarningsAsErrors` for the doc-comment family) | file-scoped ns, `var` cascade, missing XML doc (CS1591), etc. | free, deterministic |
| **1** | **Policy Gate** — `bld/check-folder-namespace-alignment.sh` + `bld/check-policy.sh` | folder↔namespace; copyright banner; file-scoped namespace on new files; no string-literal exception messages | free, deterministic |
| **2** | **Claude Policy Review** — the AI agent | test partial placement; bespoke-vs-shared KAT; pass/fail split; "Verifies that…" summaries; `<returns>` on properties; resx key taxonomy; validation grouping | per-PR model cost, judgment |

Both Tier-1 and Tier-2 are **blocking** (you chose "everything blocks"). The Tier-2 *blocking
decision* is still deterministic: the model writes findings, a plain parse step fails the check
iff any finding is `severity: "blocking"`.

## Why diff-scoped

`bld/check-policy.sh` judges only **added files and added lines**, not the whole tree. The tree
carries a few grandfathered stragglers today — the `TestHelpers*.cs` files without a banner and
three literal exception messages in `Bodu.Text.Yaml`. A tree-wide blocking check would fail every
PR until those are cleaned; diff-scoping stops the bleeding now and lets the stragglers be fixed
when their lines are next touched. The folder↔namespace script *is* run whole-tree because the
tree is already clean under it.

## One-time setup

1. **Secrets:** `ANTHROPIC_API_KEY` (or `CLAUDE_CODE_OAUTH_TOKEN`, swapping the input in
   `claude-policy-review.yml`). The fixer additionally needs a GitHub App — see its README.
2. **Branch protection:** mark **Build and Test**, **Policy Gate**, and **Claude Policy Review**
   as required status checks on your PR target branches.
3. Keep auto-merge off; a human still reviews and merges.

## Tuning

- **Add a Tier-1 rule:** extend `bld/check-policy.sh`. Keep it diff-scoped and *prove it is
  false-positive-free against the current tree* before making it blocking (that's how the three
  existing rules were validated). Anything a script can decide belongs here, not in Tier 2.
- **Adjust Tier-2 scope:** edit the prompt in `claude-policy-review.yml`. Keep it conservative —
  it can block a merge, so it should only flag high-confidence violations and grandfather
  untouched code.
- **Promote analyzers to errors (Tier-0 hardening, follow-up):** flipping select IDE rules
  (IDE0007, IDE0130, IDE0161) to `error` in `.editorconfig` would make the existing build enforce
  them. Do it behind a validated full-solution build — some legacy files may still have stragglers,
  and a tree-wide severity bump is *not* diff-scoped, so it can break the build until they're cleaned.

## Failure modes handled

- **Tier-2 infra hiccup** (no/invalid findings file) fails **open** with a warning — a flaky agent
  run never wedges the merge queue; only real findings block.
- **Fork PRs** don't run the AI review (no secret access) and are excluded from the fixer.
- **False-positive blocking** is minimized by diff-scoping (Tier 1) and the conservative,
  high-confidence prompt (Tier 2).

## Relationship to Claude CI Fixer

`claude-ci-fixer.yml` is a separate, optional **remediation** arm — it reacts to a red *Build and
Test* by pushing a fix. The policy pipeline here is about **conformance verification**, not
fixing. They compose (a fixer commit still passes back through both policy gates) but are
independent; adopt either without the other.
