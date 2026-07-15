---
title: Caching notable dates
---

# Caching notable dates

`Bodu.Globalization.Calendar.Caching` adds a read-through cache **in front of** the
notable-date service. The engine stays a pure computer that knows nothing of caching;
[`CachingNotableDateService`](xref:Bodu.Globalization.Calendar.Caching.CachingNotableDateService)
implements the same
[`INotableDateService`](xref:Bodu.Globalization.Calendar.INotableDateService) contract,
so it drops in transparently wherever the service is consumed.

## Concepts in one minute

- **The whole civil year is the cache unit.** Every query is answered per Gregorian
  year: a range is decomposed into the years it spans, each year is served from the
  cache or recomputed whole, and the result is clipped to the requested window. A later
  single-day query for a cached year never recomputes, and a query for exactly one whole
  civil year is served as the cached list itself with no copying.
- **Freshness has two independent triggers.** A time-to-live expires entries a fixed
  duration after computation (a safety net against drift), and a **resource-version
  token** invalidates every entry computed under a previous resource, so a data reload
  always forces a recompute regardless of the time-to-live.
- **Concurrent cold misses coalesce.** The first caller for a cold (territory, year)
  computes; concurrent callers join that single flight instead of stampeding the engine.
- **Storage is pluggable and best-effort.** The decorator works over any
  [`INotableDateCache`](xref:Bodu.Globalization.Calendar.Caching.INotableDateCache);
  the shipped backends degrade gracefully on storage failure — a failed read is a miss,
  a failed write is skipped — so a broken disk never breaks date resolution.

## Quickstart

Wrap the service in the decorator over any backend. The in-memory cache needs no
configuration:

<!-- compile -->
```csharp
INotableDateService engine = AmericasCalendarData.CreateService("US");

using var service = new CachingNotableDateService(
    engine,
    new InMemoryNotableDateCache(),
    new NotableDateCachingOptions { Ttl = TimeSpan.FromDays(7) });

// First query computes and caches the whole of 2026; the second is a pure cache hit.
IReadOnlyList<NotableDate> july = service.Resolve(
    new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31)), "US");
IReadOnlyList<NotableDate> newYear = service.Resolve(new DateOnly(2026, 1, 1), "US");
```

Under dependency injection, `AddCachedNotableDateService` decorates the registered
service in place, so consumers keep injecting `INotableDateService` and transparently
gain caching:

```csharp
builder.Services.AddNotableDateService(AmericasCalendarData.LoadResource("US"));
builder.Services.AddCachedNotableDateService(configure: o => o.Ttl = TimeSpan.FromDays(7));
```

## Cache backends

The decorator is storage-agnostic; pick the backend at the composition root:

| Backend | Storage | Survives restart | Shared across processes | Package |
|---|---|---|---|---|
| [`InMemoryNotableDateCache`](xref:Bodu.Globalization.Calendar.Caching.InMemoryNotableDateCache) | process memory | no | no | core |
| [`TomlNotableDateCache`](xref:Bodu.Globalization.Calendar.Caching.TomlNotableDateCache) | one TOML file per territory | yes | same machine | core |
| [`JsonNotableDateCache`](xref:Bodu.Globalization.Calendar.Caching.JsonNotableDateCache) | one JSON file per territory | yes | same machine | core |
| `SqliteNotableDateCache` | one SQLite database | yes | same machine | `…Caching.Sqlite` |
| `DistributedNotableDateCache` | any `IDistributedCache` (e.g. Redis) | yes | yes | `…Caching.Distributed` |
| [`NullNotableDateCache`](xref:Bodu.Globalization.Calendar.Caching.NullNotableDateCache) | none (always misses) | — | — | core |

Construct one by hand, or supply it through the registration's `cacheFactory`:

<!-- compile -->
```csharp
var toml = new TomlNotableDateCache(
    new FileNotableDateCacheOptions { CacheDirectory = "/var/cache/notable-dates" });

var json = new JsonNotableDateCache(
    new FileNotableDateCacheOptions { CacheDirectory = "/var/cache/notable-dates" });
```

The durable add-ons ship their own registrations, which replace the default TOML file
cache inside `AddCachedNotableDateService`:

```csharp
// SQLite: one database file, WAL-enabled, best-effort.
builder.Services.AddNotableDateService(AmericasCalendarData.LoadResource("US"));
builder.Services.AddSqliteNotableDateCache(o => o.DatabaseFilePath = "/var/cache/notable-dates.db");

// Distributed: over the registered IDistributedCache (AddRedisNotableDateCache wires Redis directly).
builder.Services.AddStackExchangeRedisCache(o => o.Configuration = "localhost:6379");
builder.Services.AddDistributedNotableDateCache();
```

Every backend enforces the same ordering contract (occurrences round-trip in the
service's date-then-identity order) and the same merge policy, so switching backends
never changes served results — only where they live.

## Freshness: time-to-live, version invalidation, jitter, and refresh-ahead

[`NotableDateCachingOptions`](xref:Bodu.Globalization.Calendar.Caching.NotableDateCachingOptions)
carries the freshness policy:

- **`Ttl`** (default 30 days) expires an entry a fixed duration after it was computed.
  Notable-date resolution is deterministic for a given resource version, so this is a
  coarse safety net; the version trigger below handles data changes.
- **`ResourceVersion`** keys every cache entry. When the decorator observes an
  [`INotableDateResourceProvider`](xref:Bodu.Globalization.Calendar.INotableDateResourceProvider),
  the token is derived from the resource identity and a reload generation instead — so
  a reload invalidates every cached year on the next query:

<!-- compile -->
```csharp
var resourceProvider = new MutableNotableDateResourceProvider(AmericasCalendarData.LoadResource("US"));

using var service = new CachingNotableDateService(
    AmericasCalendarData.CreateService("US"),
    new InMemoryNotableDateCache(),
    new NotableDateCachingOptions(),
    resourceProvider);

_ = service.Resolve(new DateOnly(2026, 1, 1), "US");     // computes and caches under version 1

resourceProvider.Reload(AmericasCalendarData.LoadResource("US"));

_ = service.Resolve(new DateOnly(2026, 1, 1), "US");     // version changed: recomputes
```

- **`TtlJitter`** (opt-in, default `0`) deterministically shaves up to that fraction off
  each territory's effective time-to-live, keyed by a stable hash of the normalized
  territory, so territories warmed together do not all expire — and recompute — at the
  same instant. Jitter only ever shortens the time-to-live.
- **`RefreshAheadFraction`** (opt-in, default `0`) turns an aged hit into a
  stale-while-revalidate serve: when a hit's entry is older than this fraction of the
  effective time-to-live, the caller is still served instantly and **one** background
  recompute of the year is scheduled. The recompute shares the single-flight guard with
  genuine misses, so a miss arriving mid-refresh is served the refreshed value; a
  failing recompute is logged (`EventId 4606`) and swallowed, and the next aged hit
  retries. Because the fraction is below `1`, a continuously hot territory never
  surfaces a miss.

<!-- compile -->
```csharp
var options = new NotableDateCachingOptions
{
    Ttl = TimeSpan.FromDays(30),
    TtlJitter = 0.1,              // spread expiries by up to 10% per territory
    RefreshAheadFraction = 0.75,  // recompute in the background after 75% of the TTL
};
options.Validate();
```

On the distributed backend, `EntryExpirationMargin` (default one hour) additionally
stamps each territory blob with a server-side lifetime of the time-to-live plus the
margin, so a territory that stops being queried self-evicts from Redis; `null` disables
server-side expiry.

## Warming the cache

A cold cache pays its year computations on the first user requests. `Warm` pre-pays
them: each territory's whole span goes through the normal read-through path, cold years
compute and cache, and already cached years cost only a cache read. A failing territory
is logged (`EventId 4607`) and skipped; the returned count is the territories warmed.

<!-- compile -->
```csharp
using var service = new CachingNotableDateService(
    AmericasCalendarData.CreateService("US"),
    new InMemoryNotableDateCache(),
    new NotableDateCachingOptions());

int warmed = service.Warm(new[] { "US", "US-CA" }, 2026, 2028);
```

Under dependency injection, register the warm-up as a hosted service and let it run
when the application starts. The span defaults to a rolling window around the current
civil year (`YearsBehind` = 0, `YearsAhead` = 1), with optional fixed
`FirstYear`/`LastYear` overrides; the run never blocks or crashes the host, and it
no-ops with a logged warning when the registered service is not the caching decorator:

```csharp
builder.Services.AddNotableDateService(AmericasCalendarData.LoadResource("US"));
builder.Services.AddCachedNotableDateService();
builder.Services.AddNotableDateCacheWarmup(configure: warmup =>
{
    warmup.Territories.Add("US");
    warmup.Territories.Add("US-CA");
    warmup.YearsAhead = 2;
});
```

Or bind the same options from configuration (section `Calendar:NotableDateCacheWarmup`):

```json
{
  "Calendar": {
    "NotableDateCacheWarmup": {
      "Territories": [ "US", "US-CA" ],
      "YearsAhead": 2
    }
  }
}
```

## Observability

**Logs.** The decorator logs each hit and miss at levels set on the options
(`CacheHitLogLevel` / `CacheMissLogLevel`, both defaulting to `Trace`), and every
caching-layer message carries a stable `EventId`:

| EventId | Event | Level |
|---|---|---|
| 4601 / 4602 | Year served from cache / year recomputed on a miss | option-set |
| 4603 / 4604 | File-cache storage failure swallowed / corrupt cache file treated as empty | `Warning` |
| 4605 / 4606 | Refresh-ahead recomputed / refresh-ahead failed | option-set / `Warning` |
| 4607 | Warm-up territory failed and skipped | `Warning` |
| 4611 | SQLite storage failure swallowed | `Warning` |
| 4621 | Distributed storage failure swallowed | `Warning` |
| 4622–4625 | Startup warm-up started / completed / failed / service not caching | `Information` / `Warning` |

Storage-failure warnings are rate-limited to at most one per minute, each carrying the
count suppressed since the previous warning, so a sustained outage is visible without
flooding the log.

**Metrics.** The caching layer publishes `System.Diagnostics.Metrics` counters through
the process-wide meter **`Bodu.Globalization.Calendar.Caching`** (the SQLite and
distributed add-ons publish their storage-failure counters through their own meters);
with no listener attached, a counter add is a no-op branch:

| Instrument | Tags | Counts |
|---|---|---|
| `bodu.calendar.notable_date_cache.hits` | `territory` | Years served from the cache |
| `bodu.calendar.notable_date_cache.misses` | `territory` | Years recomputed on a miss |
| `bodu.calendar.notable_date_cache.coalesced_flights` | `territory` | Callers that joined an in-flight computation |
| `bodu.calendar.notable_date_cache.refresh_ahead` | `territory`, `outcome` (`success`/`failed`) | Background refresh-ahead recomputes |
| `bodu.calendar.notable_date_cache.storage_failures` | `operation` | Swallowed best-effort storage failures |

Subscribe with any OpenTelemetry metrics exporter, or with an in-process
`MeterListener`:

<!-- compile -->
```csharp
using var listener = new System.Diagnostics.Metrics.MeterListener
{
    InstrumentPublished = (instrument, l) =>
    {
        if (instrument.Meter.Name == "Bodu.Globalization.Calendar.Caching")
            l.EnableMeasurementEvents(instrument);
    },
};
listener.SetMeasurementEventCallback<long>((instrument, value, tags, state) =>
    Console.WriteLine($"{instrument.Name} +{value}"));
listener.Start();
```

## Troubleshooting

**The cache is cold after every restart.** Only the in-memory cache loses state on
restart; use a file, SQLite, or distributed backend to survive restarts, and register
the startup warm-up so the first requests never pay the computation.

**A data update is not being picked up.** With a fixed `ResourceVersion`, bump the
token after a data change — or register a reloadable resource provider so a
`Reload` invalidates the cache automatically.

**The warm-up logs "service is not the caching decorator".** Register
`AddCachedNotableDateService` **before** `AddNotableDateCacheWarmup`; the warm-up
resolves `INotableDateService` and can only warm the caching decorator.

**I can't tell whether the cache is degrading.** Watch `EventId 4603` / `4611` /
`4621` at `Warning`, or the `storage_failures` counter — it increments on every
swallow, outside the log rate limiting.

## See also

- [Working with notable dates](../notable-dates.md) — the service contract the cache wraps.
- [Dependency injection](../dependency-injection.md) — the wider calendar registration surface.
- [`CachingNotableDateService` API reference](xref:Bodu.Globalization.Calendar.Caching.CachingNotableDateService)
- [`NotableDateCachingOptions` API reference](xref:Bodu.Globalization.Calendar.Caching.NotableDateCachingOptions)
- [`INotableDateCache` API reference](xref:Bodu.Globalization.Calendar.Caching.INotableDateCache)
