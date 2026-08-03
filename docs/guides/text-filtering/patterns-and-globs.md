---
title: Patterns and globs
---

# Patterns and globs

A [`TextFilterPattern`](xref:Bodu.Text.Filtering.TextFilterPattern) is one immutable rule: pattern
text, an include/exclude action, a wildcard/regex kind, and an optional case override. This guide
covers the wildcard grammar in detail and when to reach for a regex instead.

## Quick reference

| Syntax | Meaning | Example |
|---|---|---|
| `*` | zero or more characters | `error*` matches `error: disk full` |
| `?` | exactly one character | `job-?` matches `job-7`, not `job-42` |
| `[abc]`, `[a-z]` | one character from a set / range | `job-[0-9][0-9]` matches `job-42` |
| `[!abc]`, `[^abc]` | one character not in the set | `[!x]y` matches `ay`, not `xy` |
| `{a,b}` | alternation, expanded at build time | `{error,warn}*` |
| `\x` | literal `x` | `a\*b` matches only the text `a*b` |

Globs match the **whole value** — there are no path semantics, and `*` crosses every character
equally. Use `*abc*` for contains-style matching.

## Case sensitivity

Matching is ordinal and case-insensitive by default. Set
[`TextFilterOptions.IgnoreCase`](xref:Bodu.Text.Filtering.TextFilterOptions) to `false` for the
whole filter, or override per pattern via the `ignoreCase` argument — a pattern-level setting wins
in both directions. Comparison is always ordinal; culture-sensitive comparison is deliberately not
supported, because the optimized matchers operate on raw character values.

## Character classes

Classes support single members, `lo-hi` ranges, and negation with a leading `!` (or `^`). A `-`
that is the first or last member is a literal; a `]` can only be a member when escaped (`[\]]`).
An empty class (`[]`) and an unterminated class are build-time errors — grammar problems surface
from `TextFilter.Build`, never per value.

## Brace alternation

`{a,b}` expands **at build time** into one compiled matcher per alternative, so
`{error,warn,fatal}*` costs the same at evaluation time as three separate prefix patterns.
Alternatives may be empty (`a{b,}` matches `ab` and `a`), groups nest, and expansion is capped
(nesting depth 32, at most 10 000 alternatives per pattern) so a hostile pattern cannot demand
unbounded build work. All expanded alternatives report as their single declared pattern in results
and statistics.

## Escapes

`\` makes the next character literal — `\*`, `\?`, `\[`, `\{`, `\\`. A trailing `\` is a
build-time error. In lines parsed by `TextFilter.Parse`, `\!` and `\#` additionally escape a
leading `!` or `#` that would otherwise negate or comment the line.

## When to use a regex instead

Anything the glob grammar cannot express — anchored alternation inside a value, repetition counts,
digit classes — is a `TextFilterPatternKind.Regex` pattern:

```csharp
TextFilterPattern.Include(@"^metric\.[a-z]+\.p\d{2}$", TextFilterPatternKind.Regex)
```

Regexes always sit in the most expensive cost tier, compile preferring the linear-time
`NonBacktracking` engine, and carry a match timeout
([`TextFilterOptions.RegexMatchTimeout`](xref:Bodu.Text.Filtering.TextFilterOptions), default
100 ms) as the ReDoS guard for patterns that fall back to the backtracking engine (backreferences
and lookarounds). See [Telemetry and tuning](telemetry-and-tuning.md) for the timeout fail-safe
rule.

## Where to go next

- **[Evaluation modes](evaluation-modes.md)** — how patterns combine into a decision.
- **[Telemetry and tuning](telemetry-and-tuning.md)** — cost tiers and observability.
- **[API reference](xref:Bodu.Text.Filtering.TextFilterPattern)** — the pattern type.
