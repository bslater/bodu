---
title: Bodu.Text.Filtering — Guides
---

# Bodu.Text.Filtering — Guides

These guides cover the `Bodu.Text.Filtering` engine in depth. If the library is new to you, start
with the **[Introduction](../../docs/text-filtering/index.md)** and the
**[Core concepts](../../docs/text-filtering/concepts.md)** pages first — the guides assume that
vocabulary.

Part of the **[Text & Serialization](../topics/text-and-serialization.md)** topic.

## How the library works

Patterns — glob or regex, include or exclude — compile once into an immutable
[`TextFilter`](xref:Bodu.Text.Filtering.TextFilter). At build time every glob is classified into
the cheapest strategy its shape permits (literal, prefix, suffix, contains, general), brace
alternations expand into separate matchers, and regexes compile preferring the linear-time
`NonBacktracking` engine. Evaluation then runs cheapest-first with short-circuiting, updates the
built-in counters, and optionally notifies an observer with the deciding pattern.

## At a glance

| Guide | Covers |
|---|---|
| [Patterns and globs](patterns-and-globs.md) | The full glob grammar, escapes, brace expansion, when to reach for regex |
| [Evaluation modes](evaluation-modes.md) | `AnyMatch` sets vs `LastMatchWins` ordered rules, allowlists, parsing raw lines |
| [Telemetry and tuning](telemetry-and-tuning.md) | Statistics, hit counts, the observer hook, cost tiers, timeout fail-safety |

## Where to go next

- **[Getting started](../../docs/text-filtering/getting-started.md)** — install and minimal samples.
- **[Runnable samples](../../samples/text-filtering.md)** — the FilteringTour console sample.
- **[API reference](xref:Bodu.Text.Filtering)** — full type-by-type docs.
