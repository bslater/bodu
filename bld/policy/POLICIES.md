# Bodu coding-policy registry

Single source of truth for the coding policies enforced through the CI/CD pipeline. Every policy
has a stable ID so the deterministic checker, the AI reviewer, the author-time hook, and the
fix-and-learn loop can all refer to the same rule.

**This file is consumed by machines and agents, not just humans.** Keep entries terse and
ID-stable. The authoritative *prose* for each policy lives in `CLAUDE.md`; this registry is the
index and the learning surface.

## How the tiers relate

| Tier | Enforced by | When it catches |
|---|---|---|
| **0** | Roslyn analyzers in-build | at compile |
| **1** | `bld/check-policy.sh` + `bld/check-folder-namespace-alignment.sh` (diff-scoped, deterministic) | at push (hook) and in CI (Policy Gate) |
| **2** | `claude-policy-review.yml` (AI, judgment) | in CI (Claude Policy Review) |

The pipeline's goal is to **migrate catches upstream over time**: a Tier-2 judgment that recurs
should be promoted to a Tier-1 script rule (cheaper, deterministic, caught at push), and every
catch should be distilled into the Learning log below so the next author applies it proactively.

## Policies

| ID | Policy | Tier | Enforced by | Conforming form |
|---|---|---|---|---|
| BODU-P001 | Every `.cs` file starts with the copyright banner | 1 | `check-policy.sh` (added files) | The `<copyright file=…>` banner block from CLAUDE.md > File Header |
| BODU-P002 | File-scoped namespaces (`namespace X;`) | 0/1 | IDE0161 · `check-policy.sh` (added files) | `namespace Bodu.X;` — never the `{ }` block form |
| BODU-P003 | No string-literal exception messages | 1 | `check-policy.sh` (added src lines) | `throw new ArgumentException(ResourceStrings.Arg_Invalid_X, nameof(x))` |
| BODU-P004 | Folder ↔ namespace alignment (flat, dotted) | 1 | `check-folder-namespace-alignment.sh` | Folder = namespace − RootNamespace, dots preserved |
| BODU-P005 | `var` per the CLAUDE.md cascade | 0 | IDE0007 | `var` for built-ins / apparent types; explicit otherwise |
| BODU-P006 | XML docs on every member | 0 | CS1591 (WarningsAsErrors) | `<summary>` on all members; `<value>` not `<returns>` on properties |
| BODU-P007 | Test lives in the correct member/subject partial | 2 | Claude Policy Review | Route by member/subject per CLAUDE.md > Test File Organisation |
| BODU-P008 | Reuse a shared KAT generic where it fits | 2 | Claude Policy Review | `ValidKat<,>` / `RoundTripKat<,>` / `BinaryKat<,>` / `InvalidKat<>` over a bespoke record |
| BODU-P009 | Pass and fail are separate `[TestMethod]`s | 2 | Claude Policy Review | `_WhenValid_…` / `_WhenInvalid_…` over filtered sources; never a branch on an outcome flag |
| BODU-P010 | Test naming + "Verifies that …" summary | 2 | Claude Policy Review | `<Member>_When…_Should…` with a "Verifies that …" `<summary>` |
| BODU-P011 | `<returns>` not used on a property | 2 | Claude Policy Review | Use `<value>` for the property's value |
| BODU-P012 | resx key follows the taxonomy | 2 | Claude Policy Review | `Arg_Invalid_*` / `Arg_Null_*` / `Op_Invalid_*` / `Format_Invalid_*` / … |
| BODU-P013 | Validation grouped at the top of the member | 2 | Claude Policy Review | All guards contiguous before the body, then a blank line |

## Learning log

Append-only. Each downstream catch is distilled here so upstream authors (human or agent) apply
the rule *before* CI catches it. The fix-and-learn workflow (`claude-policy-fix.yml`) writes an
entry every time it remediates a violation. When an entry's rule is deterministically checkable,
promote it into `check-policy.sh` and note the promotion here — that is how a Tier-2 judgment
becomes a Tier-1 script and the catch-point moves upstream.

Entry format:

```
### <date> · <policy-id> · <one-line title>
- Caught by: <Policy Gate | Claude Policy Review | pre-push hook>
- Symptom:  <the exact anti-pattern that slipped through>
- Fix:      <the conforming form applied>
- Promoted: <none | added rule N to check-policy.sh | analyzer bumped to error>
```

<!-- LEARNING-LOG:START -->
_No entries yet. The first policy violation the pipeline fixes will be recorded here._
<!-- LEARNING-LOG:END -->
