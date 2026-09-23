# Bodu.Globalization.Calendar.Caching

> **API stability — Preview.** The public API surface is largely settled but is still being finalized ahead of the 1.0 release and may change; breaking changes can land in a minor version until then.

A read-through cache that sits **in front of** the notable-date service. The engine stays a pure
computer that knows nothing of caching: `CachingNotableDateService` implements the same
`INotableDateService` contract, so it drops in transparently wherever the service is consumed.

> For the full walkthrough — quickstart, freshness, warm-up, and observability — see the
> [Caching notable dates guide](../docs/guides/calendar/caching/notable-date-caching.md); the
> per-backend surface is in [Backends and options](../docs/guides/calendar/caching/backends-and-options.md),
> and [Writing a custom backend](../docs/guides/calendar/caching/custom-backend.md) covers the
> `INotableDateCache` contract.

## Installation

```shell
dotnet add package Bodu.Globalization.Calendar.Caching
```

Targets `net8.0`. All types live in the `Bodu.Globalization.Calendar.Caching` namespace.

```csharp
services.AddCachedNotableDateService(/* … */);
```

## How it caches

- **The whole civil year is the cache unit.** Every query is answered per Gregorian year: a range is
  decomposed into the years it spans, each year is served from the cache or recomputed whole, and the
  result is clipped to the requested window. A later single-day query for a cached year never
  recomputes.
- **Freshness has two independent triggers.** A time-to-live expires entries a fixed duration after
  computation, and a **resource-version token** invalidates every entry computed under a previous
  resource — so a data reload always forces a recompute regardless of the time-to-live.
- **Concurrent cold misses coalesce.** The first caller for a cold (territory, year) computes;
  concurrent callers join that single flight instead of stampeding the engine.
- **Storage is best-effort.** The shipped backends degrade gracefully: a failed read is a miss, a
  failed write is skipped. A broken disk never breaks date resolution.

## Backends in this package

| Type | Storage | Notes |
|---|---|---|
| `InMemoryNotableDateCache` | Process memory | No configuration required; the default for a single process |
| `TomlNotableDateCache` | One TOML file per territory | Human-readable and diffable on disk |
| `JsonNotableDateCache` | One JSON file per territory | Same file layout, JSON encoding |
| `NullNotableDateCache` | Nothing | Always misses; for disabling caching without changing wiring |

Durable and shared backends ship separately:
[`…Caching.Sqlite`](https://www.nuget.org/packages/Bodu.Globalization.Calendar.Caching.Sqlite) and
[`…Caching.Distributed`](https://www.nuget.org/packages/Bodu.Globalization.Calendar.Caching.Distributed).
Both file backends derive from `FileNotableDateCacheBase` / `FileNotableDateCacheOptions`; a custom
backend implements `INotableDateCache` (or extends `NotableDateCacheBase<TOptions>`).

## Warm-up

`AddNotableDateCacheWarmup` registers a hosted `NotableDateCacheWarmupService` that drives
`CachingNotableDateService.Warm` over a configurable rolling territory/year window, so the first
real request does not pay the cold-compute cost.

## Out of scope

Computing the dates themselves — that is
[`Bodu.Globalization.Calendar`](https://www.nuget.org/packages/Bodu.Globalization.Calendar), which
this package decorates. The rules come from the regional data packs
(`Bodu.Globalization.Calendar.{Americas,AsiaPacific,Europe,MiddleEast,Africa}`).

Part of the [Bodu](https://github.com/bslater/bodu) utility library.
