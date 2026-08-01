# Bodu.Text.Filtering.Samples.FilteringTour

A four-scenario tour of the `Bodu.Text.Filtering` engine: compiling include/exclude pattern sets,
parsing gitignore-style rule lines with ordered last-match-wins evaluation, the glob grammar with
its cost-tier classification and deciding-pattern diagnostics, and the built-in telemetry with a
per-decision observer.

Everything runs offline against small in-code corpora, so the output is deterministic.

```bash
dotnet run --project samples/Text.Filtering/Bodu.Text.Filtering.Samples.FilteringTour
```

## Scenario 1 — IncludeExcludeBasics

**Intent.** Show the default `AnyMatch` semantics — the Ant / MSBuild include-exclude set model —
and the include-all default when a filter has no include patterns.

**What it does.** Builds a filter from two include globs and one exclude glob, streams a small
log-line corpus through `Filter`, then builds an exclude-only filter to show that everything
passes unless an exclude vetoes it. Matching is ordinal and case-insensitive by default, so
`WARN:` matches `warn*`.

**What to expect.**

```text
--- Include/exclude sets (AnyMatch) ---
kept    -> error: disk full
kept    -> warn: retrying request
kept    -> WARN: cache miss

report.txt  with exclude-only filter -> True
scratch.tmp with exclude-only filter -> False
```

**APIs demonstrated.** `TextFilter.Build`, `TextFilterPattern.Include` / `Exclude`,
`TextFilter.Filter`, `TextFilter.IsMatch`.

## Scenario 2 — ParseAndOrderedRules

**Intent.** Show `TextFilter.Parse` reading raw lines with the gitignore file conventions, and the
`LastMatchWins` mode where the last matching rule decides — so a later include re-admits a value an
earlier exclude rejected, and an allowlist is expressed with a leading exclude-everything rule.

**What it does.** Parses a comment-bearing rule list under
`TextFilterEvaluationMode.LastMatchWins`, probes the re-inclusion and unmatched-default behaviors,
then parses the `["!*", "error*", "!*debug*"]` allowlist shape.

**What to expect.**

```text
--- Parse + gitignore-style ordered rules (LastMatchWins) ---
app.log       -> False
important.log -> True (re-included by the later rule)
readme.txt    -> True (unmatched values are included)

error1      -> True
error-debug -> False
info        -> False
```

**APIs demonstrated.** `TextFilter.Parse`, `TextFilterOptions.Mode`,
`TextFilterEvaluationMode.LastMatchWins`.

## Scenario 3 — GlobsAndCostTiers

**Intent.** Tour the glob grammar — `{a,b}` alternation, character classes, escapes — alongside a
regex pattern, and show the diagnostic surfaces that reveal which pattern decided each outcome.

**What it does.** Builds a mixed filter (`{error,warn}*` expands at build time into two cheap
prefix matchers; the class pattern routes through the general matcher; the regex sits in the most
expensive tier), evaluates five values with `Evaluate`, lists every matching pattern for an
overlapping value with `GetMatchingPatterns`, and matches an escaped-metacharacter literal.

**What to expect.**

```text
--- Glob grammar + cost tiers ---
warn: slow disk  -> Included     decided by +wildcard:{error,warn}*
job-42           -> Included     decided by +wildcard:job-[0-9][0-9]
job-7x           -> NotIncluded  decided by (no pattern)
metric.http.p99  -> Included     decided by +regex:^metric\.[a-z]+\.p\d{2}$
error-retry-8    -> Excluded     decided by -wildcard:*retry*

error-retry-8 matches 2 pattern(s): +wildcard:{error,warn}*, -wildcard:*retry*
literal 'a*b' -> True, 'axb' -> False
```

**APIs demonstrated.** `TextFilter.Evaluate`, `TextFilterResult.Decision` / `Pattern`,
`TextFilter.GetMatchingPatterns`, `TextFilterPatternKind.Regex`.

## Scenario 4 — TelemetryAndObserver

**Intent.** Show the always-on statistics counters and the optional per-decision observer hook.

**What it does.** Filters a deterministic 200-value corpus, prints the reconciled decision buckets
(`Evaluated == Accepted + Excluded + NotIncluded`) and the per-pattern hit counts (credited to the
*deciding* pattern), then attaches an `ITextFilterObserver` that logs each value an exclude vetoed.

**What to expect.**

```text
--- Telemetry + observer ---
evaluated 200, accepted 100, excluded 50, not-included 50 (kept 100)
  +wildcard:{error,warn}* decided 100 outcomes
  -wildcard:*debug*  decided 50 outcomes

observer: 'warn-debug-10' vetoed by -wildcard:*debug*
observer: 'error-debug-11' vetoed by -wildcard:*debug*
```

**APIs demonstrated.** `TextFilter.GetStatistics`, `TextFilterStatistics`,
`TextFilterPatternStatistics.HitCount`, `TextFilter.ResetStatistics`, `TextFilter.Observer`,
`ITextFilterObserver`.
