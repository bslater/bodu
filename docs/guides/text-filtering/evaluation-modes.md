---
title: Evaluation modes
---

# Evaluation modes

[`TextFilterEvaluationMode`](xref:Bodu.Text.Filtering.TextFilterEvaluationMode) selects how a
filter's rules combine into a decision. The two modes are the two models established tools already
use — pick the one matching the mental model your users bring.

## Quick reference

| | `AnyMatch` (default) | `LastMatchWins` |
|---|---|---|
| Model | Ant / MSBuild include-exclude sets | gitignore ordered rules |
| Order matters? | No — sets are order-independent | Yes — the last matching rule decides |
| Unmatched value | Included when the include set is empty; otherwise rejected | Always included |
| Exclude vs include conflict | Exclude always vetoes | Whichever matched **later** wins |
| Cost-tier reordering | Full (cheapest-first) | Per-rule matchers only; order is semantic |

## `AnyMatch` — include/exclude sets

A value is accepted when *(the include set is empty OR at least one include matches)* AND *no
exclude matches*.

```csharp
var filter = TextFilter.Build(
[
    TextFilterPattern.Include("error*"),
    TextFilterPattern.Exclude("*debug*"),
]);
```

With no includes at all, everything passes unless vetoed — the exclude-only shape `.gitignore`
users expect. Declaring any include flips the filter into allowlist behavior. Because set matching
is an order-independent OR, the engine evaluates each group cheapest-strategy-first and
short-circuits — reordering can never change the outcome, only which of several matching patterns
gets *reported* as the deciding one.

## `LastMatchWins` — ordered rules

Rules form one ordered list; evaluation conceptually walks it and the **last** matching rule's
action decides. Unmatched values are included, exactly as in gitignore.

```csharp
var ordered = new TextFilterOptions { Mode = TextFilterEvaluationMode.LastMatchWins };
var filter = TextFilter.Parse(["!*.log", "important.log"], ordered);
// app.log → excluded; important.log → re-included by the later rule; readme.txt → included
```

Re-inclusion is the point of this mode: a later include re-admits what an earlier exclude
rejected. Express an **allowlist** with a leading exclude-everything rule:

```csharp
var allow = TextFilter.Parse(["!*", "error*", "!*debug*"], ordered);
// only error* values pass, except those containing "debug"
```

## Parsing raw lines

[`TextFilter.Parse`](xref:Bodu.Text.Filtering.TextFilter) reads raw pattern lines with the
gitignore file conventions in either mode: a bare line is an include, a leading `!` makes it an
exclude, `#` starts a comment line, blank lines are skipped, and `\!` / `\#` escape a literal
leading character. Lines always parse as wildcard patterns; declare regexes through
[`TextFilterPattern`](xref:Bodu.Text.Filtering.TextFilterPattern) or the
[`TextFilterBuilder`](xref:Bodu.Text.Filtering.TextFilterBuilder).

## Choosing a mode

- Configuration that reads like *"take these, but not those"* — `AnyMatch`.
- Configuration users will edit like a `.gitignore` file, with later lines overriding earlier
  ones — `LastMatchWins`.
- Need maximum throughput over huge corpora with many patterns — `AnyMatch`, which gets the full
  cost-tier reordering.

## Where to go next

- **[Patterns and globs](patterns-and-globs.md)** — the grammar the rules are written in.
- **[Telemetry and tuning](telemetry-and-tuning.md)** — seeing which rules decide at volume.
- **[API reference](xref:Bodu.Text.Filtering.TextFilterEvaluationMode)** — the mode enumeration.
