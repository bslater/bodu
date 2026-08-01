---
uid: Bodu.Text.Filtering
---

![Bodu.Text.Filtering](~/images/hero-text-filtering.svg)

## Purpose

**Bodu.Text.Filtering** filters lists of text values through include/exclude pattern sets — glob
(wildcard) and regular-expression patterns compiled once into an immutable
<xref:Bodu.Text.Filtering.TextFilter> that classifies every pattern by evaluation cost and runs the
cheapest strategies first. The design deliberately adopts the models users already know: Ant /
MSBuild-style unordered include/exclude sets, gitignore-style ordered last-match-wins rules with
`!` negation, and `globset`-style strategy extraction. Built-in telemetry reports what matched,
what was vetoed, and by which pattern.

Sibling text libraries cover adjacent jobs: <xref:Bodu.Text.Encoding> for binary-to-text codecs and
<xref:Bodu.Text.Configuration> for configuration documents.

## Static documentation

- **[Bodu.Text.Filtering introduction](~/docs/text-filtering/index.md)** — the mental model, evaluation modes, and type map.
- **[Bodu.Text.Filtering core concepts](~/docs/text-filtering/concepts.md)** — vocabulary: actions, pattern kinds, modes, cost tiers, deciding patterns.
- **[Bodu.Text.Filtering getting started](~/docs/text-filtering/getting-started.md)** — install and minimal samples.
- **[Bodu.Text.Filtering guides](~/guides/text-filtering/index.md)** — pattern grammar, evaluation modes, and telemetry deep dives.

## Key types

- <xref:Bodu.Text.Filtering.TextFilter> — the compiled engine: `Build` / `Parse` factories, `IsMatch`, `Evaluate`, `Filter`, statistics, and the observer hook.
- <xref:Bodu.Text.Filtering.TextFilterBuilder> — fluent assembly (`AddInclude` / `AddExclude` / `AddParsed`) in the `Microsoft.Extensions.FileSystemGlobbing` style.
- <xref:Bodu.Text.Filtering.TextFilterPattern> — one immutable rule: pattern text, include/exclude action, wildcard/regex kind, optional case override.
- <xref:Bodu.Text.Filtering.TextFilterOptions> — evaluation mode, case default, regex match timeout, opt-in timing capture.
- <xref:Bodu.Text.Filtering.TextFilterEvaluationMode> — `AnyMatch` sets (Ant / MSBuild) or `LastMatchWins` ordered rules (gitignore).
- <xref:Bodu.Text.Filtering.TextFilterResult> / <xref:Bodu.Text.Filtering.TextFilterDecision> — the outcome of one evaluation and the pattern that decided it.
- <xref:Bodu.Text.Filtering.TextFilterStatistics> / <xref:Bodu.Text.Filtering.TextFilterPatternStatistics> — the counters snapshot: decision buckets, per-pattern hit counts, regex timeouts, optional timing.
- <xref:Bodu.Text.Filtering.ITextFilterObserver> — the per-decision callback hook; a single null check when unattached.

## Example

```csharp
using Bodu.Text.Filtering;

var filter = TextFilter.Parse(new[]
{
    "error*",        // include everything starting with "error"
    "warn*",         // ... or "warn"
    "!*debug*",      // but exclude anything containing "debug"
});

foreach (var line in filter.Filter(lines))
    Console.WriteLine(line);

var stats = filter.GetStatistics();   // evaluated / accepted / excluded, hits per pattern
```

## Notes

- **Whole-string matching.** Globs match the entire value; use `*abc*` for contains-style matching. Comparison is ordinal, case-insensitive by default, and overridable per filter and per pattern.
- **Cost-tiered evaluation.** At build time each glob is classified — literal, prefix, suffix, contains, general wildcard — and regexes compile preferring the linear-time `NonBacktracking` engine; groups evaluate cheapest-first, which cannot change the outcome because set matching is an order-independent OR.
- **Fail-safe regex timeouts.** A timed-out include does not admit the value; a timed-out exclude still vetoes it — and the event is visible in the statistics and to the observer.
- **See also:** the [introduction](~/docs/text-filtering/index.md) and the [guides](~/guides/text-filtering/index.md).
