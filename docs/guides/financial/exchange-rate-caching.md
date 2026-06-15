---
title: Caching and aggregating exchange rates
---

# Caching and aggregating exchange rates

`Bodu.Financial.ExchangeRates.Caching` adds two pieces **in front of** the
exchange-rate providers. The concrete providers (Yahoo, RBA, ECB, BoE) stay pure
fetchers that know nothing of caching; each piece implements the same
[`IDatedExchangeRateProvider`](xref:Bodu.Financial.IDatedExchangeRateProvider)
contract (and the timeless [`IExchangeRateProvider`](xref:Bodu.Financial.IExchangeRateProvider)),
so they drop in transparently:

```text
Caller
  │  IDatedExchangeRateProvider / IExchangeRateProvider
  ▼
AggregatingExchangeRateProvider     ── groups named children; routes per FX pair and
  │                                    combines them with a strategy (priority / average)
  ├── CachingExchangeRateProvider("RBA")  ── read-through cache over ONE source + ONE cache
  │        └── RbaExchangeRateProvider
  └── CachingExchangeRateProvider("ECB")
           └── EcbExchangeRateProvider
```

The two pieces are orthogonal: use the cache alone to add read-through caching to
a single source, the aggregator alone to group already-cached (or uncached)
providers, or compose them as above.

## Concepts in one minute

- **Caching provider** — [`CachingExchangeRateProvider`](xref:Bodu.Financial.ExchangeRates.Caching.CachingExchangeRateProvider)
  wraps **one** inner source over **one** single-provider cache. It serves fresh
  cached rates and delegates to the source only on a miss, then caches what the
  source returns.
- **Cache** — a cache is **bound to one provider**.
  [`IExchangeRateCache`](xref:Bodu.Financial.ExchangeRates.Caching.IExchangeRateCache)
  owns expiry: callers pass a duration, and the cache returns only fresh rows and
  prunes stale ones on write. Shipped stores are
  [`TomlFileExchangeRateCache`](xref:Bodu.Financial.ExchangeRates.Caching.TomlFileExchangeRateCache)
  (on disk), [`InMemoryExchangeRateCache`](xref:Bodu.Financial.ExchangeRates.Caching.InMemoryExchangeRateCache),
  and the no-op [`NullExchangeRateCache`](xref:Bodu.Financial.ExchangeRates.Caching.NullExchangeRateCache).
- **Options** — [`CachingExchangeRateOptions`](xref:Bodu.Financial.ExchangeRates.Caching.CachingExchangeRateOptions)
  carries the cache **location** (`CacheDirectory`), the **default expiry**
  (`DefaultExpiry`), **per-provider overrides** (`ProviderExpiry`), the per-event
  log levels, and `DefaultLookupOptions` for the timeless surface.
- **Aggregator** — [`AggregatingExchangeRateProvider`](xref:Bodu.Financial.ExchangeRates.Caching.AggregatingExchangeRateProvider)
  groups named children behind one entry point, combining them with a pluggable
  [`IExchangeRateAggregationStrategy`](xref:Bodu.Financial.ExchangeRates.Caching.IExchangeRateAggregationStrategy)
  and optional **per-FX-pair routing**.
- **Entry** — [`CachedExchangeRate`](xref:Bodu.Financial.ExchangeRates.Caching.CachedExchangeRate)
  is one cached row: the observation `Date`, the `Rate`, and the `CachedAtUtc`
  instant that drives expiry.

## Caching one provider

`CachingExchangeRateProvider` caches exactly one source. The convenience
constructor builds a TOML file cache for you from the options; the provider name
is the subdirectory the source's files land under.

```csharp
using Bodu.Financial;
using Bodu.Financial.ExchangeRates.Caching;

var options = new CachingExchangeRateOptions
{
    CacheDirectory = "/var/cache/fx",       // null/blank → a bodu-exchange-rates temp folder
    DefaultExpiry = TimeSpan.FromHours(12),
};
options.ProviderExpiry["RBA"] = TimeSpan.FromDays(7);   // RBA publishes daily; cache longer

// Wrap the RBA source. Cache files land under /var/cache/fx/RBA/.
IDatedExchangeRateProvider cachedRba = new CachingExchangeRateProvider("RBA", rba, options);
```

The provider name lives at the composition root — the provider classes never learn
they are being cached. A second constructor accepts an explicit
[`IExchangeRateCache`](xref:Bodu.Financial.ExchangeRates.Caching.IExchangeRateCache),
so you can choose the in-memory store or supply your own:

```csharp
IDatedExchangeRateProvider cachedEcb =
    new CachingExchangeRateProvider(ecb, new InMemoryExchangeRateCache("ECB"), options);
```

The decorator also implements the timeless surface, which resolves the current UTC
date under `CachingExchangeRateOptions.DefaultLookupOptions`:

```csharp
decimal todayRate = ((IExchangeRateProvider)cachedRba).GetRate("AUD", "USD");
```

### Per-provider expiry and the global default

`GetExpiry(name)` returns a provider's specific override when present and
`DefaultExpiry` otherwise:

```csharp
options.GetExpiry("RBA");     // 7 days   (override)
options.GetExpiry("ECB");     // 12 hours (the default)
```

### Single-date lookups

`GetRate` / `TryGetRate` flow through the cache. On a hit the cached rows are
reconstructed into a [`FixedDatedExchangeRateProvider`](xref:Bodu.Financial.FixedDatedExchangeRateProvider),
so date-resolution policy, inverse pairs, and same-currency identity all behave
exactly as the underlying stack would:

```csharp
// Miss → fetched from the source, then cached.
ExchangeRateLookupResult r1 = cachedRba.GetRate("AUD", "USD", new DateOnly(2024, 1, 3));

// Repeat within the expiry window → served from cache, no source call.
ExchangeRateLookupResult r2 = cachedRba.GetRate("AUD", "USD", new DateOnly(2024, 1, 3));

// Resolution policies are honoured against the cached rows.
cachedRba.TryGetRate("AUD", "USD", new DateOnly(2024, 1, 5),
    ExchangeRateLookupOptions.PreviousWithin(7), out ExchangeRateLookupResult r3);
```

### Range lookups

`GetRatesAsync` returns every rate whose date falls in the inclusive window. The
cache serves a range only when the fresh cached rows **span** the requested window
(their earliest date is on or before `start` and their latest is on or after
`end`); otherwise the whole range is refetched and re-cached.

```csharp
IReadOnlyList<ExchangeRate> january =
    await cachedRba.GetRatesAsync("AUD", "USD", new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 31));
```

> [!NOTE]
> Because the cache stores only the dates that actually had rates, a range whose
> edge falls on a non-trading day (for example a weekend) reads as "not spanned"
> and triggers a refetch. This keeps the design free of extra coverage metadata.

## The cache cascade

The cache is deliberately layered so you can plug in at whichever level fits:

| Layer | Type | Responsibility |
|---|---|---|
| Contract | [`IExchangeRateCache`](xref:Bodu.Financial.ExchangeRates.Caching.IExchangeRateCache) | Single-provider store: a bound `Provider`; `GetRates`/`Store` by pair. |
| Core | [`ExchangeRateCacheBase<TOptions>`](xref:Bodu.Financial.ExchangeRates.Caching.ExchangeRateCacheBase`1) | Freshness filtering + merge/prune. **No physical layout.** |
| File seam | [`IFileExchangeRateCache`](xref:Bodu.Financial.ExchangeRates.Caching.IFileExchangeRateCache) / [`FileExchangeRateCacheBase<TOptions>`](xref:Bodu.Financial.ExchangeRates.Caching.FileExchangeRateCacheBase`1) | Directory + per-pair file-name resolution, best-effort IO. |
| Leaf | [`TomlFileExchangeRateCache`](xref:Bodu.Financial.ExchangeRates.Caching.TomlFileExchangeRateCache) | The TOML serialization format only. |

### The on-disk TOML format

A cache bound to provider `RBA` stores `AUD/USD` as `<directory>/RBA/AUDUSD.toml`
— a per-provider subdirectory with one file per pair. Each dated rate is a TOML
table; the `decimal` rate is written as a **quoted string** so its full precision
and scale round-trip exactly, and the dates use TOML's native RFC 3339 forms:

```toml
[[Entries]]
Date = 2023-01-03
Rate = "0.5000"
CachedAtUtc = 2023-01-04T09:15:00+00:00

[[Entries]]
Date = 2023-01-06
Rate = "0.5100"
CachedAtUtc = 2023-01-04T09:15:00+00:00
```

The serializer is [`Bodu.Text.Toml`](xref:Bodu.Text.Toml.TomlSerializer) with
`TomlDecimalHandling.String`. The file is **best-effort**: any I/O or TOML error
on read yields an empty result, and a failed write is swallowed, so a cache
problem never breaks rate retrieval. You can use a cache directly — note there is
no provider argument; the cache is bound to its provider at construction:

```csharp
var cache = new TomlFileExchangeRateCache(
    new FileExchangeRateCacheOptions { Provider = "RBA", CacheDirectory = "/var/cache/fx" });

var now = DateTimeOffset.UtcNow;
cache.Store(new ExchangeRatePair("AUD", "USD"),
    new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now) },
    TimeSpan.FromHours(24), now);

IReadOnlyList<CachedExchangeRate> fresh =
    cache.GetRates(new ExchangeRatePair("AUD", "USD"), TimeSpan.FromHours(24), now);
```

### Custom cache stores

To back the cache with something other than the file system, derive from
[`ExchangeRateCacheBase<TOptions>`](xref:Bodu.Financial.ExchangeRates.Caching.ExchangeRateCacheBase`1).
It provides the merge-by-date, read-time freshness filter, and write-time prune;
you supply only the raw per-pair persistence:

```csharp
public sealed class DictionaryExchangeRateCache : ExchangeRateCacheBase<ExchangeRateCacheOptions>
{
    private readonly Dictionary<ExchangeRatePair, IReadOnlyList<CachedExchangeRate>> _store = new();

    public DictionaryExchangeRateCache(string provider)
        : base(new ExchangeRateCacheOptions { Provider = provider }) { }

    protected override IReadOnlyList<CachedExchangeRate> ReadEntries(ExchangeRatePair pair) =>
        _store.TryGetValue(pair, out var rows) ? rows : Array.Empty<CachedExchangeRate>();

    protected override void WriteEntries(ExchangeRatePair pair, IReadOnlyList<CachedExchangeRate> entries) =>
        _store[pair] = entries;
}
```

For a new **file format**, derive from
[`FileExchangeRateCacheBase<TOptions>`](xref:Bodu.Financial.ExchangeRates.Caching.FileExchangeRateCacheBase`1)
instead and override only `FileExtension`, `Serialize`, and `Deserialize` — the
base handles the directory layout and best-effort IO.

## Grouping providers with the aggregator

[`AggregatingExchangeRateProvider`](xref:Bodu.Financial.ExchangeRates.Caching.AggregatingExchangeRateProvider)
groups several named children behind one entry point and resolves each request
through a strategy. Build the children (typically each wrapped in its own cache),
then group them:

```csharp
var rba = new CachingExchangeRateProvider("RBA", rbaSource, options);
var ecb = new CachingExchangeRateProvider("ECB", ecbSource, options);

IDatedExchangeRateProvider provider = new AggregatingExchangeRateProvider(
    new[]
    {
        new NamedDatedExchangeRateProvider("RBA", rba),
        new NamedDatedExchangeRateProvider("ECB", ecb),
    });
```

### Strategies

The combination is a pluggable
[`IExchangeRateAggregationStrategy`](xref:Bodu.Financial.ExchangeRates.Caching.IExchangeRateAggregationStrategy):

- [`PriorityFallbackStrategy`](xref:Bodu.Financial.ExchangeRates.Caching.PriorityFallbackStrategy)
  (the default) returns the first child that resolves — the successor to the
  former `CompositeDatedExchangeRateProvider`.
- [`AverageStrategy`](xref:Bodu.Financial.ExchangeRates.Caching.AverageStrategy)
  returns the arithmetic mean of every child that resolves, tagged with a
  synthetic provider label (`Average` by default).
- Implement the interface for anything else (weighted, median, first-non-stale).

```csharp
var options = new ExchangeRateAggregationOptions { DefaultStrategy = new AverageStrategy() };
```

### Per-FX-pair routing

[`ExchangeRateAggregationOptions.Routes`](xref:Bodu.Financial.ExchangeRates.Caching.ExchangeRateAggregationOptions)
maps a pair to an ordered child list and an optional pair-specific strategy, so
each pair can prefer a different source — `AUD/USD` via `[RBA, ECB]` while
`USD/GBP` prefers `[ECB, RBA]`:

```csharp
var aggregation = new ExchangeRateAggregationOptions();
aggregation.Routes[new ExchangeRatePair("AUD", "USD")] = new ExchangeRatePairRoute(new[] { "RBA", "ECB" });
aggregation.Routes[new ExchangeRatePair("USD", "GBP")] = new ExchangeRatePairRoute(new[] { "ECB", "RBA" });
aggregation.Routes[new ExchangeRatePair("EUR", "USD")] = new ExchangeRatePairRoute(new[] { "ECB", "RBA" }, new AverageStrategy());

var provider = new AggregatingExchangeRateProvider(children, aggregation);
```

A pair without a route uses `DefaultProviderOrder` (or the supplied child order)
and `DefaultStrategy`. When inversion is allowed, an inverse-pair route is also
consulted.

### Reaching a specific source

The lookup methods always apply the configured strategy and routing. When you need
one source's answer specifically, resolve it by name — without bypassing the
contract:

```csharp
if (((AggregatingExchangeRateProvider)provider).TryGetProvider("RBA", out IDatedExchangeRateProvider rbaOnly))
{
    ExchangeRateLookupResult rbaRate = rbaOnly.GetRate("AUD", "USD", new DateOnly(2024, 1, 3));
}
```

Under dependency injection the same access is available through a keyed service
(below).

## Dependency injection

The companion package `Bodu.Financial.ExchangeRates.Caching.DependencyInjection`
registers either shape on the `IFinancialServiceBuilder`. Both resolve as the
dated **and** timeless surfaces.

A single cached provider:

```csharp
using Bodu.Financial.DependencyInjection;
using Bodu.Financial.ExchangeRates.Caching.DependencyInjection;

services.AddBoduFinancial()
        .AddRbaHistoricalRates()
        .AddCachedExchangeRateProvider<RbaExchangeRateProvider>("RBA",
            configure: o => o.DefaultExpiry = TimeSpan.FromHours(12));
```

A group of cached providers with per-pair routing. Each child is **also**
registered as a keyed `IDatedExchangeRateProvider`, so a specific source is
resolvable by name:

```csharp
services.AddBoduFinancial()
        .AddRbaHistoricalRates()
        .AddEcbHistoricalRates()
        .AddAggregatedExchangeRateProvider(agg => agg
            .AddCachedChild<RbaExchangeRateProvider>("RBA")
            .AddCachedChild<EcbExchangeRateProvider>("ECB")
            .MapPair(new ExchangeRatePair("AUD", "USD"), "RBA", "ECB")
            .MapPair(new ExchangeRatePair("USD", "GBP"), "ECB", "RBA"));

// Later: the aggregate, or a specific source.
var aggregate = provider.GetRequiredService<IDatedExchangeRateProvider>();
var rbaOnly = provider.GetRequiredKeyedService<IDatedExchangeRateProvider>("RBA");
```

`UseDefaultStrategy(...)` overrides the default `PriorityFallbackStrategy`, and
`MapPair(pair, strategy, order)` overrides the strategy for a single pair. Bind
`CachingExchangeRateOptions` from configuration by passing an `IConfiguration`
(default section `Financial:ExchangeRateCache`).

## How staleness works

- A cached row is fresh while `asOf - CachedAtUtc < duration`, where `duration` is
  the provider's resolved expiry. Single-date serving filters per row.
- A write merges new rows with existing ones (latest `CachedAtUtc` wins per date)
  and prunes rows that are no longer fresh, so the store self-cleans over time.
- Range serving uses the min/max dates of the fresh rows as a coverage proxy and
  refetches the whole range when they do not span the request.

## See also

- [Working with exchange rates](exchange-rates.md) — the provider contracts the
  cache and aggregator wrap.
- [Exchange-rate types catalogue](exchange-types.md) — every FX type mapped to a
  scenario.
- [Dependency injection](dependency-injection.md) — the wider financial
  registration surface.
- [`CachingExchangeRateProvider` API reference](xref:Bodu.Financial.ExchangeRates.Caching.CachingExchangeRateProvider)
- [`AggregatingExchangeRateProvider` API reference](xref:Bodu.Financial.ExchangeRates.Caching.AggregatingExchangeRateProvider)
- [`TomlFileExchangeRateCache` API reference](xref:Bodu.Financial.ExchangeRates.Caching.TomlFileExchangeRateCache)
