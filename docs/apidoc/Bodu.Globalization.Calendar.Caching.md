---
uid: Bodu.Globalization.Calendar.Caching
---

![Bodu.Globalization.Calendar.Caching](~/images/hero-calendar-caching.svg)

## Purpose

**Bodu.Globalization.Calendar.Caching** is the read-through caching layer for the
[`Bodu.Globalization.Calendar`](Bodu.Globalization.Calendar.md) notable-date engine. Rather than building caching into
the engine, it ships <xref:Bodu.Globalization.Calendar.Caching.CachingNotableDateService>, a decorator that implements
the same <xref:Bodu.Globalization.Calendar.INotableDateService> contract and serves each requested civil year from an
<xref:Bodu.Globalization.Calendar.Caching.INotableDateCache>, recomputing a year through the wrapped service only on a
miss. Consumers hold the same interface either way; only *when* a year is computed changes.

The cache unit is one territory's occurrences for one whole civil year. A range is decomposed into the years it spans,
each year is served from the cache when a fresh, version-matching entry exists, and the assembled occurrences are
clipped to the requested window. Freshness has two independent triggers the cache evaluates on every call: a
time-to-live (default 30 days, with optional deterministic jitter and access-triggered refresh-ahead), and a
resource-version token that invalidates every year computed under a previous resource — derived automatically from an
observed <xref:Bodu.Globalization.Calendar.INotableDateResourceProvider>, so a reload forces a recompute regardless of
the time-to-live. Concurrent cold misses for the same year coalesce onto one computation.

The core package ships the in-memory and per-territory TOML / JSON file backends; two add-on packages supply durable,
shareable storage over the same contract and namespace — `Bodu.Globalization.Calendar.Caching.Sqlite`
(<xref:Bodu.Globalization.Calendar.Caching.SqliteNotableDateCache>) and
`Bodu.Globalization.Calendar.Caching.Distributed` (<xref:Bodu.Globalization.Calendar.Caching.DistributedNotableDateCache>
over any `IDistributedCache`, Redis included). Every backend applies the same freshness, validity, version-matching,
merge, and ordering rules, and every backend is best-effort by default: a storage fault surfaces as an empty read or a
skipped write, never as an exception that breaks date resolution.

All dependency-injection registration lives in the `Bodu.Globalization.Calendar` namespace, so a single
`using Bodu.Globalization.Calendar;` makes `AddCachedNotableDateService`, `AddSqliteNotableDateCache`,
`AddDistributedNotableDateCache`, `AddRedisNotableDateCache`, and `AddNotableDateCacheWarmup` available. There are no
separate `*.DependencyInjection` packages.

## Static documentation

- **[Introduction](~/docs/calendar-caching/index.md)** — the package family and which backend to pick, the decorator, the cache unit and freshness model, the storage contract, headline types, and the DI registrations.
- **[Core concepts](~/docs/calendar-caching/concepts.md)** — decorator vs service, cache key and unit, hit / miss / coalesced flight / refresh-ahead, time-to-live vs resource version, warm-up, write status, storage failure policy, backend classes, observability, thread safety and lifetime.
- **[Getting started](~/docs/calendar-caching/getting-started.md)** — install and minimal samples: in-memory, TOML file, DI with `appsettings.json`, SQLite, Redis, warm-up, and a custom `INotableDateCache`.
- **[Caching notable dates guide](~/guides/calendar/caching/notable-date-caching.md)** — worked patterns, freshness tuning, observability, and troubleshooting.
- **[Calendar dependency injection guide](~/guides/calendar/dependency-injection.md)** — the `AddNotableDateService` / `AddReloadableNotableDateService` registrations the decorator wraps.

## Key types

**Service and decorator**

- <xref:Bodu.Globalization.Calendar.Caching.CachingNotableDateService> — the decorator. Constructed over an inner `INotableDateService`, an `INotableDateCache`, and a `NotableDateCachingOptions`, with an optional `INotableDateResourceProvider` version source, `TimeProvider`, `ILoggerFactory`, and `ownsCache` flag. Implements every `INotableDateService` member (the filtered overloads apply the filter after assembly; discovery methods delegate) and adds `Warm(territories, firstYear, lastYear)`. Disposable; disposes the cache only when it owns it.

**Contract**

- <xref:Bodu.Globalization.Calendar.Caching.INotableDateCache> — the storage contract: `GetYear(territory, year, resourceVersion, ttl, asOf)` returns a fresh, version-matching entry or `null`; `StoreYear(entry, ttl, asOf)` merges a computed year, prunes stale and superseded-version entries, and reports a write status; `Clear()` empties the cache best-effort. Implementations preserve occurrence order and degrade rather than throw on storage faults.
- <xref:Bodu.Globalization.Calendar.Caching.NotableDateCacheEntry> — the cache unit: `Territory`, `Year`, `ResourceVersion`, the ordered `Occurrences`, `ComputedAtUtc`, and `IsFresh(asOf, ttl)`. An empty occurrence list is a valid, cacheable result.
- <xref:Bodu.Globalization.Calendar.Caching.NotableDateCacheWriteStatus> — `Stored` (persisted), `Skipped` (a deliberate no-op cache), `Failed` (a swallowed storage error; nothing persisted).

**Options**

- <xref:Bodu.Globalization.Calendar.Caching.NotableDateCachingOptions> — the decorator's options: `Ttl` (30 days), `TtlJitter` and `RefreshAheadFraction` (both `0`, range `[0, 1)`), `ResourceVersion` (the fixed token used when no provider is observed), `CacheDirectory` (for the default file cache), `CacheHitLogLevel` / `CacheMissLogLevel` (`Information`); `Validate()` throws, `TryValidate(out error)` backs `ValidateOnStart`.
- <xref:Bodu.Globalization.Calendar.Caching.NotableDateCacheOptions> — the storage-agnostic base of every backend's options: `ThrowOnStorageFailure` (rethrow `IOException` / `UnauthorizedAccessException` instead of degrading) and `ValidateStorageOnStart` (probe the store at construction or host start), both `false` by default; virtual `Validate()` / `TryValidate(out error)`.
- <xref:Bodu.Globalization.Calendar.Caching.FileNotableDateCacheOptions> — adds `CacheDirectory` (`null` → `bodu-notable-dates` under the system temporary path).
- <xref:Bodu.Globalization.Calendar.Caching.SqliteNotableDateCacheOptions> — `DatabaseFilePath` or a full `ConnectionString` (precedence; at least one required), `UseWriteAheadLogging` (`true`), `BusyTimeout` (five seconds).
- <xref:Bodu.Globalization.Calendar.Caching.DistributedNotableDateCacheOptions> — `KeyPrefix` (keys are `<prefix>notable-dates:<TERRITORY>`) and `EntryExpirationMargin` (one hour, added to the time-to-live as each blob's server-side absolute expiration; `null` disables it).
- <xref:Bodu.Globalization.Calendar.Caching.NotableDateCacheWarmupOptions> — the startup warm-up: `Territories`, a rolling `YearsBehind` (`0`) / `YearsAhead` (`1`) window around the current UTC year, or pinned `FirstYear` / `LastYear`.

**Backends**

- <xref:Bodu.Globalization.Calendar.Caching.NotableDateCacheBase`1> — the storage-agnostic mechanism behind every shipped backend: territory normalization, read-time freshness and version filtering, and write-time merge-and-prune under a per-territory lock, over a `protected internal` `ReadEntries` / `WriteEntries` seam opened to the companion packages. A third-party store implements `INotableDateCache` directly.
- <xref:Bodu.Globalization.Calendar.Caching.FileNotableDateCacheBase> — the file mechanism: one file per territory under `CacheDirectory`, atomic temp-and-move writes, a last-write-time parse memo, rate-limited degradation warnings.
- <xref:Bodu.Globalization.Calendar.Caching.InMemoryNotableDateCache> — process memory; nothing persisted.
- <xref:Bodu.Globalization.Calendar.Caching.TomlNotableDateCache>, <xref:Bodu.Globalization.Calendar.Caching.JsonNotableDateCache> — one `<TERRITORY>.toml` / `.json` file per territory; malformed content reads as empty and is repaired by the next write.
- <xref:Bodu.Globalization.Calendar.Caching.SqliteNotableDateCache> — a `notable_dates` table keyed by `(territory, year, version)` with a single-row `GetYear` override and a keep-alive connection; disposable. Registered with `AddSqliteNotableDateCache`.
- <xref:Bodu.Globalization.Calendar.Caching.DistributedNotableDateCache> — one JSON blob per territory in any `IDistributedCache`, stamped with a server-side expiration; `Clear` removes only the keys the instance wrote. Registered with `AddDistributedNotableDateCache` or the Redis convenience `AddRedisNotableDateCache`.
- <xref:Bodu.Globalization.Calendar.Caching.NullNotableDateCache> — the no-op cache (`NullNotableDateCache.Instance`), for when caching is disabled.

**Dependency injection and warm-up** (namespace `Bodu.Globalization.Calendar`)

- <xref:Bodu.Globalization.Calendar.NotableDateCachingExtensions> — `AddCachedNotableDateService(configure?, cacheFactory?)` and `AddCachedNotableDateService(configuration, sectionName = "Calendar:NotableDateCache", configure?, cacheFactory?)`: decorates the registered `INotableDateService` in place (the previous registration becomes the inner service), builds and owns a `TomlNotableDateCache` when no `cacheFactory` is supplied, observes a registered `INotableDateResourceProvider`, `TimeProvider`, and `ILoggerFactory`, and validates the options at host start. Throws `InvalidOperationException` when no service is registered.
- <xref:Bodu.Globalization.Calendar.SqliteNotableDateCacheExtensions> — `AddSqliteNotableDateCache(configuration?, sectionName = "Calendar:NotableDateCache:Sqlite", configure?)`: a singleton `SqliteNotableDateCache` also exposed as `INotableDateCache`, with the storage probe wired into `ValidateOnStart` when `ValidateStorageOnStart` is set.
- <xref:Bodu.Globalization.Calendar.DistributedNotableDateCacheExtensions> — `AddDistributedNotableDateCache(configuration?, sectionName = "Calendar:NotableDateCache:Distributed", configure?)` over the container's `IDistributedCache`, and `AddRedisNotableDateCache(configureRedis, configuration?, sectionName, configure?)`, which registers the Redis cache first.
- <xref:Bodu.Globalization.Calendar.NotableDateCacheWarmupExtensions> — `AddNotableDateCacheWarmup(configuration?, sectionName = "Calendar:NotableDateCacheWarmup", configure?)`: a hosted service that warms the configured territories after the host starts without blocking it; no-ops with a warning when the registered service is not the caching decorator.

**On-disk schema**

- <xref:Bodu.Globalization.Calendar.Caching.NotableDateCacheFile> — the document the file caches and the distributed blob serialize: `Territory`, an `Entries` array, and a flat `Occurrences` array.
- <xref:Bodu.Globalization.Calendar.Caching.NotableDateCacheYearRow> — one row per cached year: `Year`, `Version`, `ComputedAtUtc`; present even for a year with no occurrences.
- <xref:Bodu.Globalization.Calendar.Caching.NotableDateCacheOccurrenceRow> — one flat scalar row per occurrence, carrying its `Year` and `Version` plus every <xref:Bodu.Globalization.Calendar.NotableDate> field, with the rule identity flattened to `ResourceId` / `NotableDateId` / `RuleId` and the category as its enum name. The SQLite backend stores the same rows as a JSON blob per year.

## Example

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Caching;

// Wrap any INotableDateService — here a regional data pack — in the decorator over a per-territory TOML cache.
INotableDateService engine = AmericasCalendarData.CreateService("US");

using var calendar = new CachingNotableDateService(
    engine,
    new TomlNotableDateCache(new FileNotableDateCacheOptions { CacheDirectory = "/var/cache/notable-dates" }),
    new NotableDateCachingOptions
    {
        Ttl = TimeSpan.FromDays(30),
        RefreshAheadFraction = 0.75,               // recompute a hot year in the background after 75% of the TTL
        ResourceVersion = "us-holidays-2026.1",    // bump after a data update; a resource provider derives this automatically
    },
    ownsCache: true);

// The first query computes and caches all of 2026; the next two are cache hits.
IReadOnlyList<NotableDate> year = calendar.Resolve(2026, "US");
IReadOnlyList<NotableDate> day = calendar.Resolve(new DateOnly(2026, 7, 4), "US");
IReadOnlyList<NotableDate> closures = calendar.Resolve(2026, "US-CA", NotableDateFilter.IsNonWorkingDay());

// Pre-pay the computations for next year as well.
int warmed = calendar.Warm(new[] { "US", "US-CA" }, 2026, 2027);
```

Under dependency injection, register the service first and decorate it in place; add the durable backends through their
own registrations and hand them to the decorator through `cacheFactory`:

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Caching;
using Microsoft.Extensions.DependencyInjection;

builder.Services.AddReloadableNotableDateService(AmericasCalendarData.LoadResource("US"));
builder.Services.AddSqliteNotableDateCache(builder.Configuration);           // Calendar:NotableDateCache:Sqlite
builder.Services.AddCachedNotableDateService(
    builder.Configuration,                                                    // Calendar:NotableDateCache
    cacheFactory: sp => sp.GetRequiredService<INotableDateCache>());
builder.Services.AddNotableDateCacheWarmup(builder.Configuration);           // Calendar:NotableDateCacheWarmup
```

## Notes

- **Order matters under DI.** `AddCachedNotableDateService` decorates whatever `INotableDateService` is registered at
  that point and throws when there is none; `AddNotableDateCacheWarmup` resolves the service at run time and no-ops
  (with `EventId 4625`) unless it is the caching decorator. Register service → cache backend → decorator → warm-up.
- **The add-on registrations do not replace the default cache.** `AddSqliteNotableDateCache` and
  `AddDistributedNotableDateCache` register an `INotableDateCache`; `AddCachedNotableDateService` still builds a
  TOML file cache unless a `cacheFactory` resolves the registered one.
- **Results never change with the backend.** Every backend applies the shared freshness (strict less-than),
  clock-skew (one minute), version-matching, merge, and ordering rules; only durability and sharing differ.
- **Best-effort by default, strict on request.** Storage faults degrade to misses and skipped writes with rate-limited
  `Warning` logs (`EventId 4603` file, `4611` SQLite, `4621` distributed) and always-incrementing `storage_failures`
  counters on the `Bodu.Globalization.Calendar.Caching[.Sqlite|.Distributed]` meters. `ThrowOnStorageFailure`
  rethrows; `ValidateStorageOnStart` fails the host start on an unusable store.
- **Distributed writes are last-write-wins across processes**, since `IDistributedCache` has no atomic
  read-modify-write; entries are recomputable, so this is acceptable for a best-effort cache.
