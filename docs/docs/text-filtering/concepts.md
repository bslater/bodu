---
title: Bodu.Text.Filtering — Core concepts
---

# Bodu.Text.Filtering — Core concepts

This page is the vocabulary the rest of the documentation assumes. Read it once before the
[getting-started samples](getting-started.md) or the [guides](../../guides/text-filtering/index.md),
and refer back whenever a term feels imprecise.

Part of the **[Text & Serialization](../topics/text-and-serialization.md)** topic.

For the high-level shape of the library and the type map, start with the [introduction](index.md).

## Pattern, action, kind

A **pattern** ([`TextFilterPattern`](xref:Bodu.Text.Filtering.TextFilterPattern)) is one immutable
rule. Its **action** ([`TextFilterAction`](xref:Bodu.Text.Filtering.TextFilterAction)) says what a
match means — `Include` admits matching values, `Exclude` rejects them. Its **kind**
([`TextFilterPatternKind`](xref:Bodu.Text.Filtering.TextFilterPatternKind)) says how the text is
interpreted — `Wildcard` (the glob grammar) or `Regex` (a .NET regular expression). A pattern can
also carry a per-pattern case override; otherwise it inherits the filter's default.

## Compiled filter

Patterns do nothing on their own. They compile — via
[`TextFilter.Build`](xref:Bodu.Text.Filtering.TextFilter), `TextFilter.Parse`, or a
[`TextFilterBuilder`](xref:Bodu.Text.Filtering.TextFilterBuilder) — into an immutable
[`TextFilter`](xref:Bodu.Text.Filtering.TextFilter). Grammar errors and regex syntax errors surface
at build time, not per value. The compiled matching state is immutable, so a filter is safe to use
from multiple threads.

## Evaluation mode

[`TextFilterEvaluationMode`](xref:Bodu.Text.Filtering.TextFilterEvaluationMode) selects the
combination semantics:

- **`AnyMatch`** — includes and excludes are unordered sets. Accepted ⇔ (include set empty OR ≥ 1
  include matches) AND no exclude matches. The Ant / MSBuild model.
- **`LastMatchWins`** — one ordered rule list; the last matching rule decides; unmatched values are
  included. The gitignore model, including `!` re-inclusion.

## Include-all default

In `AnyMatch` mode a filter with *no include patterns* accepts everything an exclude does not veto —
the way `.gitignore` and MSBuild `Remove` items behave. Declaring even one include flips the filter
into allowlist behavior: now a value must positively match an include.

## Cost tier

At build time each glob is classified into the cheapest strategy its shape permits — match-all,
literal, prefix, suffix, prefix-and-suffix, contains, or the general wildcard matcher — and regexes
form the most expensive tier. In `AnyMatch` mode each group is evaluated cheapest-first with
short-circuiting; this is purely an optimization, because set matching is an order-independent OR.

## Deciding pattern

Every evaluation reaches a [`TextFilterDecision`](xref:Bodu.Text.Filtering.TextFilterDecision) —
`IncludedByDefault`, `Included`, `Excluded`, or `NotIncluded` — and, when a pattern caused the
outcome, [`TextFilterResult.Pattern`](xref:Bodu.Text.Filtering.TextFilterResult) reports it: the
include that admitted the value or the exclude that vetoed it. In `AnyMatch` mode, when several
patterns could have matched, the reported pattern is one of them (the cheapest); in `LastMatchWins`
it is exactly the last matching rule. `GetMatchingPatterns` reports *every* matching pattern when
diagnostics need the full picture.

## Telemetry

A filter keeps always-on counters — evaluated / accepted / excluded / not-included, per-pattern
**hit counts** credited to the deciding pattern, and regex-timeout counts — exposed as an immutable
[`TextFilterStatistics`](xref:Bodu.Text.Filtering.TextFilterStatistics) snapshot. An optional
[`ITextFilterObserver`](xref:Bodu.Text.Filtering.ITextFilterObserver) sees every decision as it
happens; when unattached the evaluation path pays only a null check.

## Fail-safe timeout

Regular expressions always carry a match timeout (and prefer the linear-time `NonBacktracking`
engine, under which the timeout is effectively unreachable). If a pattern does time out, the
decision fails safe by action: a timed-out include does not admit the value; a timed-out exclude
still vetoes it. The event is counted and visible to the observer.

## Where to go next

- **[Getting started](getting-started.md)** — install and minimal samples.
- **[Introduction](index.md)** — the type map and scenario index.
- **[Guides](../../guides/text-filtering/index.md)** — deep dives on the grammar, modes, and telemetry.
- **[API reference](xref:Bodu.Text.Filtering)** — full type-by-type docs.
