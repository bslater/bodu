---
title: Runnable samples
---

# Runnable samples

The repository ships a runnable, self-contained sample project for `Bodu.Text.Filtering` under
[`samples/Text.Filtering/`](https://github.com/bslater/bodu/tree/master/samples/Text.Filtering).
The sample is **pure computation over fixed in-code corpora** — offline, deterministic, no data
files — and is a member of `bodu.slnx`, built and executed by CI. Its README documents every
scenario individually: its intent, what the code does, the output to expect, and the APIs
demonstrated.

Run it from the repository root:

```bash
dotnet run --project samples/Text.Filtering/Bodu.Text.Filtering.Samples.FilteringTour
```

## The sample

### Bodu.Text.Filtering.Samples.FilteringTour

The engine end to end, in four scenarios: include/exclude sets under the default `AnyMatch`
semantics — two include globs, one exclude veto, and the include-all default when no includes are
declared; gitignore-convention parsing (`!` negation, `#` comments) evaluated under
<xref:Bodu.Text.Filtering.TextFilterEvaluationMode>.`LastMatchWins`, showing re-inclusion of
`important.log` past an earlier `!*.log` and the leading-`!*` allowlist shape; the glob grammar —
`{error,warn}*` brace alternation expanding into cheap prefix matchers, character classes, and
escaped metacharacters — inspected through <xref:Bodu.Text.Filtering.TextFilter>.`Evaluate` (which
reports the deciding pattern) and `GetMatchingPatterns` (which reports every match); and the
telemetry surface — the reconciling decision buckets and per-pattern hit counts from
<xref:Bodu.Text.Filtering.TextFilter>.`GetStatistics`, then an
<xref:Bodu.Text.Filtering.ITextFilterObserver> that logs each value an exclude vetoed. *Package:
`Bodu.Text.Filtering`.*

## Where to go next

- **[Bodu.Text.Filtering introduction](../docs/text-filtering/index.md)** — the mental model and type map.
- **[Getting started](../docs/text-filtering/getting-started.md)** — install and minimal samples.
- **[Guides](../guides/text-filtering/index.md)** — the grammar, evaluation modes, and telemetry in depth.
