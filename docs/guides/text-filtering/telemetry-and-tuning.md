---
title: Telemetry and tuning
---

# Telemetry and tuning

A [`TextFilter`](xref:Bodu.Text.Filtering.TextFilter) is built for bulk work — 100k+ values against
tens to hundreds of patterns — so it ships with the observability needed to understand and tune a
filter at that volume: always-on counters, per-pattern hit attribution, and an optional
per-decision observer.

## Quick reference

| Surface | What it tells you | Cost |
|---|---|---|
| [`GetStatistics()`](xref:Bodu.Text.Filtering.TextFilter.GetStatistics) | Decision buckets, per-pattern hit counts, regex timeouts, optional timing | A few plain counter increments per value |
| [`Evaluate(value)`](xref:Bodu.Text.Filtering.TextFilter.Evaluate*) | The decision and the deciding pattern for one value | Same as `IsMatch` |
| [`GetMatchingPatterns(value)`](xref:Bodu.Text.Filtering.TextFilter.GetMatchingPatterns*) | *Every* matching pattern (no short-circuit) | Full scan; diagnostic use |
| [`Observer`](xref:Bodu.Text.Filtering.TextFilter.Observer) | Every decision as it happens, with the deciding pattern | One null check when unattached |

## The statistics snapshot

[`TextFilterStatistics`](xref:Bodu.Text.Filtering.TextFilterStatistics) is an immutable snapshot.
The buckets always reconcile — `ItemsEvaluated == ItemsAccepted + ItemsExcluded +
ItemsNotIncluded` — and per-pattern
[`HitCount`](xref:Bodu.Text.Filtering.TextFilterPatternStatistics) credits the **deciding** pattern
only: the include that admitted the value or the exclude that vetoed it. That is exactly the signal
selectivity tuning needs — a pattern with a near-zero hit count over a large corpus is either
redundant or shadowed.

Counters are deliberately plain (not interlocked): matching results are always exact under
concurrency (the compiled state is immutable), but counters may undercount when evaluating from
multiple threads. Single-threaded pipelines get exact statistics for free. Timing is opt-in via
[`TextFilterOptions.CaptureEvaluationTime`](xref:Bodu.Text.Filtering.TextFilterOptions) and
accumulates into `EvaluationTime`.

## The observer hook

Attach an [`ITextFilterObserver`](xref:Bodu.Text.Filtering.ITextFilterObserver) to see every
decision — the evaluated value, the [`TextFilterDecision`](xref:Bodu.Text.Filtering.TextFilterDecision),
and the deciding pattern — for sampling, audit logging, or debugging a surprising veto. The
callback runs synchronously on the evaluating thread; exceptions propagate to the caller; when no
observer is attached the evaluation path pays only a single null check.

## Cost tiers and what they mean for tuning

At build time each glob is classified into the cheapest strategy its shape permits, and in
`AnyMatch` mode each group runs cheapest-first:

| Tier | Pattern shape | Strategy |
|---|---|---|
| 0 | `*` | always true |
| 1 | `abc` | whole-string equality |
| 2 | `abc*`, `*abc`, `abc*def` | prefix / suffix comparison |
| 3 | `*abc*` | vectorized substring search |
| 4 | `?`, classes, multiple `*` segments | general wildcard matcher |
| 5 | regex | `NonBacktracking` regex engine |

Practical consequences:

- Prefer glob shapes over regexes when both can express the rule — `{error,warn}*` compiles to two
  tier-2 prefix checks, while `^(error|warn)` is tier 5.
- Order within a group does not matter in `AnyMatch` — write rules for readability; the engine
  sorts by cost.
- The indicative benchmark (100k values, 100 mixed patterns) runs ~3× faster than a compiled
  per-pattern-regex baseline, with zero allocations per value.

## Regex timeouts fail safe

Regexes prefer the linear-time `NonBacktracking` engine; patterns it cannot compile
(backreferences, lookarounds) fall back to the backtracking engine guarded by
[`RegexMatchTimeout`](xref:Bodu.Text.Filtering.TextFilterOptions). If a match times out, the
decision fails safe **by action**: a timed-out include does not admit the value, and a timed-out
exclude still vetoes it — the filter never accidentally lets a value through because its exclude
timed out. Timeouts are counted in
[`TextFilterStatistics.RegexTimeouts`](xref:Bodu.Text.Filtering.TextFilterStatistics) and visible
to the observer.

## Where to go next

- **[Patterns and globs](patterns-and-globs.md)** — shaping rules into cheap tiers.
- **[Evaluation modes](evaluation-modes.md)** — set vs ordered semantics.
- **[API reference](xref:Bodu.Text.Filtering.TextFilterStatistics)** — the statistics types.
