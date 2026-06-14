---
title: Caching exchange rates
---

# Caching exchange rates

`Bodu.Financial.ExchangeRates.Caching` adds a caching layer **in front of**
the exchange-rate providers. The concrete providers (Yahoo, RBA, ECB, BoE)
stay pure fetchers that know nothing of caching; the caching provider
implements the same [`IDatedExchangeRateProvider`](xref:Bodu.Financial.IDatedExchangeRateProvider)
contract the caller already resolves, so it drops in transparently:

```text
Caller
  │  IDatedExchangeRateProvider
  ▼
CachingDatedExchangeRateProvider   ── returns the rate when a fresh one is cached;
  │                                    otherwise delegates downstream and caches it
  ▼
concrete provider(s)  (Yahoo / RBA / ECB / BoE)
```

Rates are persisted as **TOML**, one file per `(provider, currency pair)`, so a
fresh rate survives process restarts.

## Concepts in one minute

- **Caching provider** — [`CachingDatedExchangeRateProvider`](xref:Bodu.Financial.ExchangeRates.Caching.CachingDatedExchangeRateProvider)
  wraps **one or more named** sources supplied at construction. It serves fresh
  cached rates and delegates to a source only on a miss, then caches what the
  source returns.
- **Source name** — each wrapped source is paired with a name. That name is the
  cache key segment (the TOML file prefix) and the key into the per-provider
  expiry map.
- **Options** — [`CachingExchangeRateOptions`](xref:Bodu.Financial.ExchangeRates.Caching.CachingExchangeRateOptions)
  carries the cache **location** (`CacheDirectory`), the **default expiry**
  (`DefaultExpiry`), and **per-provider overrides** (`ProviderExpiry`).
- **Cache** — [`IExchangeRateCache`](xref:Bodu.Financial.ExchangeRates.Caching.IExchangeRateCache)
  owns expiry: callers pass a duration, and the cache returns only fresh rows and
  prunes stale ones on write. [`TomlFileSystemExchangeRateCache`](xref:Bodu.Financial.ExchangeRates.Caching.TomlFileSystemExchangeRateCache)
  is the on-disk implementation; [`NullExchangeRateCache`](xref:Bodu.Financial.ExchangeRates.Caching.NullExchangeRateCache)
  is the no-op.
- **Entry** — [`CachedExchangeRate`](xref:Bodu.Financial.ExchangeRates.Caching.CachedExchangeRate)
  is one cached row: the observation `Date`, the `Rate`, and the `CachedAtUtc`
  instant that drives expiry.

## Wrapping one or more providers

Pass the sources as ordered `(name, provider)` pairs. With several sources the
caching provider behaves as a **caching composite**: it consults them in order
and returns the first that can satisfy the request — from that source's fresh
cache or, failing that, by fetching and caching.

```csharp
using Bodu.Financial;
using Bodu.Financial.ExchangeRates.Caching;

var options = new CachingExchangeRateOptions
{
    CacheDirectory = "/var/cache/fx",       // null/blank → a bodu-exchange-rates temp folder
    DefaultExpiry = TimeSpan.FromHours(12), // applies to any source without an override
};
options.ProviderExpiry["RBA"] = TimeSpan.FromDays(7);   // RBA publishes daily; cache longer

var caching = new CachingDatedExchangeRateProvider(
    new[]
    {
        new KeyValuePair<string, IDatedExchangeRateProvider>("Yahoo", yahoo),
        new KeyValuePair<string, IDatedExchangeRateProvider>("RBA", rba),
    },
    options);
```

The provider names live at the composition root — the provider classes never
learn they are being cached. A second constructor accepts an explicit
`IExchangeRateCache` for testing or to share one cache across providers; when one
is supplied, `CacheDirectory` is ignored.

## Per-provider expiry and the global default

`GetExpiry(name)` returns a source's specific override when present and
`DefaultExpiry` otherwise:

```csharp
options.GetExpiry("RBA");     // 7 days   (override)
options.GetExpiry("Yahoo");   // 12 hours (the default)
```

## Single-date lookups

`GetRate` / `TryGetRate` flow through the cache per source. On a hit the cached
rows are reconstructed into a [`FixedDatedExchangeRateProvider`](xref:Bodu.Financial.FixedDatedExchangeRateProvider),
so date-resolution policy, inverse pairs, and same-currency identity all behave
exactly as the underlying stack would:

```csharp
// Miss → fetched from the first source that has it, then cached.
ExchangeRateLookupResult r1 = caching.GetRate("AUD", "USD", new DateOnly(2024, 1, 3));

// Repeat within the expiry window → served from disk, no source call.
ExchangeRateLookupResult r2 = caching.GetRate("AUD", "USD", new DateOnly(2024, 1, 3));

// Resolution policies are honoured against the cached rows.
caching.TryGetRate("AUD", "USD", new DateOnly(2024, 1, 5),
    ExchangeRateLookupOptions.PreviousWithin(7), out ExchangeRateLookupResult r3);
```

## Range lookups

`GetRatesAsync` returns every rate whose date falls in the inclusive window. The
cache serves a range only when the source's fresh cached rows **span** the
requested window (their earliest date is on or before `start` and their latest is
on or after `end`); otherwise the whole range is refetched and re-cached.

```csharp
IReadOnlyList<ExchangeRate> january =
    await caching.GetRatesAsync("AUD", "USD", new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 31));
```

> [!NOTE]
> Because the cache stores only the dates that actually had rates, a range whose
> edge falls on a non-trading day (for example a weekend) reads as "not spanned"
> and triggers a refetch. This keeps the design free of extra coverage metadata.

## The on-disk TOML format

A store under provider `Yahoo` for `AUD/USD` writes `Yahoo_AUDUSD.toml`. Each
dated rate is a TOML table; the `decimal` rate is written as a **quoted string**
so its full precision and scale round-trip exactly, and the dates use TOML's
native RFC 3339 forms:

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
problem never breaks rate retrieval.

You can point the cache at any directory, or use it directly:

```csharp
var cache = new TomlFileSystemExchangeRateCache(
    new FileSystemExchangeRateCacheOptions { CacheDirectory = "/var/cache/fx" });

var now = DateTimeOffset.UtcNow;
cache.Store("Yahoo", new ExchangeRatePair("AUD", "USD"),
    new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now) },
    TimeSpan.FromHours(24), now);

IReadOnlyList<CachedExchangeRate> fresh =
    cache.GetRates("Yahoo", new ExchangeRatePair("AUD", "USD"), TimeSpan.FromHours(24), now);
```

## Custom cache stores

Implement [`IExchangeRateCache`](xref:Bodu.Financial.ExchangeRates.Caching.IExchangeRateCache)
to back the cache with something other than the file system. The easiest path is
to derive from [`ExchangeRateCacheBase`](xref:Bodu.Financial.ExchangeRates.Caching.ExchangeRateCacheBase),
which provides the merge-by-date, read-time freshness filter, and write-time
prune; you supply only the raw entry persistence:

```csharp
public sealed class InMemoryExchangeRateCache : ExchangeRateCacheBase
{
    private readonly Dictionary<string, IReadOnlyList<CachedExchangeRate>> _store = new(StringComparer.Ordinal);

    protected override IReadOnlyList<CachedExchangeRate> ReadEntries(string provider, ExchangeRatePair pair) =>
        _store.TryGetValue($"{provider}_{pair.FromIsoCode}{pair.ToIsoCode}", out var rows)
            ? rows : Array.Empty<CachedExchangeRate>();

    protected override void WriteEntries(string provider, ExchangeRatePair pair, IReadOnlyList<CachedExchangeRate> entries) =>
        _store[$"{provider}_{pair.FromIsoCode}{pair.ToIsoCode}"] = entries;
}
```

## Dependency injection

The companion package `Bodu.Financial.ExchangeRates.Caching.DependencyInjection`
registers the caching provider as the `IDatedExchangeRateProvider`, wrapping
named sources resolved from the container. Single-date **and** range lookups flow
through the cache for anything that resolves the interface:

```csharp
using Bodu.Financial.DependencyInjection;
using Bodu.Financial.ExchangeRates.Caching.DependencyInjection;

services.AddBoduFinancial()
        .AddYahooExchangeRates()
        .AddRbaHistoricalRates()
        .AddCachedExchangeRateProvider(
            sources => sources
                .AddSource<YahooExchangeRateProvider>(YahooExchangeRateProvider.ProviderName)
                .AddSource<RbaExchangeRateProvider>(RbaExchangeRateProvider.ProviderName),
            configure: o =>
            {
                o.CacheDirectory = "/var/cache/fx";
                o.DefaultExpiry = TimeSpan.FromHours(12);
                o.ProviderExpiry[RbaExchangeRateProvider.ProviderName] = TimeSpan.FromDays(7);
            });
```

`.AddSource<TProvider>(name)` resolves the concrete provider from the container;
`.AddSource(name, sp => …)` takes a factory. Bind `CachingExchangeRateOptions`
from configuration by passing an `IConfiguration` (default section
`Financial:ExchangeRateCache`). A raw-factory overload
(`Func<IServiceProvider, IEnumerable<KeyValuePair<string, IDatedExchangeRateProvider>>>`)
is available for full control.

## How staleness works

- A cached row is fresh while `asOf - CachedAtUtc < duration`, where `duration`
  is the source's resolved expiry. Single-date serving filters per row.
- A write merges new rows with existing ones (latest `CachedAtUtc` wins per date)
  and prunes rows that are no longer fresh, so files self-clean over time.
- Range serving uses the min/max dates of the fresh rows as a coverage proxy and
  refetches the whole range when they do not span the request.

## See also

- [Working with exchange rates](exchange-rates.md) — the provider contracts the
  cache wraps.
- [Exchange-rate types catalogue](exchange-types.md) — every FX type mapped to a
  scenario.
- [Dependency injection](dependency-injection.md) — the wider financial
  registration surface.
- [`CachingDatedExchangeRateProvider` API reference](xref:Bodu.Financial.ExchangeRates.Caching.CachingDatedExchangeRateProvider)
- [`CachingExchangeRateOptions` API reference](xref:Bodu.Financial.ExchangeRates.Caching.CachingExchangeRateOptions)
- [`TomlFileSystemExchangeRateCache` API reference](xref:Bodu.Financial.ExchangeRates.Caching.TomlFileSystemExchangeRateCache)
