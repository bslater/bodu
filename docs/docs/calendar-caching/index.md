---
title: Bodu.Globalization.Calendar.Caching — Introduction
---

# Bodu.Globalization.Calendar.Caching

![Bodu.Globalization.Calendar.Caching](../../images/hero-calendar-caching.svg)

**Bodu.Globalization.Calendar.Caching** puts a read-through cache in front of the
[`Bodu.Globalization.Calendar`](../calendar/index.md) notable-date engine. Its centerpiece,
<xref:Bodu.Globalization.Calendar.Caching.CachingNotableDateService>, is a **decorator**: it implements the same
<xref:Bodu.Globalization.Calendar.INotableDateService> contract as the engine, wraps any existing service, and
serves each requested civil year from a cache instead of recomputing it. The engine itself stays a pure computer
that knows nothing of caching. Part of the **[Globalization & Calendars](../topics/globalization-and-calendars.md)**
topic.

The core package ships the decorator, the storage contract, and three backends — in-memory, and one TOML or JSON
file per territory. Two add-on packages supply durable, shareable storage over the same contract: a SQLite database
and any `IDistributedCache` (Redis included). All three packages register through `IServiceCollection` extensions
declared in the `Bodu.Globalization.Calendar` namespace, so a single `using Bodu.Globalization.Calendar;` brings
`AddCachedNotableDateService`, `AddSqliteNotableDateCache`, `AddDistributedNotableDateCache`,
`AddRedisNotableDateCache`, and `AddNotableDateCacheWarmup` into scope.

## The package family

| Package | Storage | Survives restart | Shared across processes | Pick it when |
|---|---|---|---|---|
| **`Bodu.Globalization.Calendar.Caching`** | Process memory (<xref:Bodu.Globalization.Calendar.Caching.InMemoryNotableDateCache>), or one TOML / JSON file per territory (<xref:Bodu.Globalization.Calendar.Caching.TomlNotableDateCache> / <xref:Bodu.Globalization.Calendar.Caching.JsonNotableDateCache>) | In-memory: no. Files: yes | Files: same machine only | A single process, or a single machine whose cache directory can be a local folder. The default DI registration uses the TOML file cache. |
| `Bodu.Globalization.Calendar.Caching.Sqlite` | One SQLite database file (<xref:Bodu.Globalization.Calendar.Caching.SqliteNotableDateCache>) | Yes | Same machine (WAL-enabled, busy-timeout aware) | Many territories on one machine, where one keyed row per year beats re-parsing a whole territory file on every lookup. |
| `Bodu.Globalization.Calendar.Caching.Distributed` | Any `IDistributedCache` — Redis, SQL Server, or the in-memory distributed cache (<xref:Bodu.Globalization.Calendar.Caching.DistributedNotableDateCache>) | Yes | Yes | Several application instances that should share one warm cache. `AddRedisNotableDateCache` wires Redis and the cache in one call. |

Every backend applies the same freshness, validity, version-matching, and merge rules, so switching backends never
changes what the decorator serves — only where it lives.

## Core mental model

```
INotableDateService (engine)               INotableDateCache (storage)
        ▲                                          ▲
        │ miss: Resolve(whole civil year)          │ GetYear / StoreYear / Clear
        │                                          │
        └────────── CachingNotableDateService ─────┘
                    ▲
                    │ Resolve(date | range, territory[, filter])
               consumer
```

### The decorator

<xref:Bodu.Globalization.Calendar.Caching.CachingNotableDateService> takes the wrapped service, an
<xref:Bodu.Globalization.Calendar.Caching.INotableDateCache>, and a
<xref:Bodu.Globalization.Calendar.Caching.NotableDateCachingOptions>. Because it implements
`INotableDateService`, it drops in anywhere the engine is consumed — a hand-built `NotableDateService`, a data pack's
`CreateService(...)`, or a <xref:Bodu.Globalization.Calendar.ReloadableNotableDateService>. The filtered
`Resolve` overloads apply the <xref:Bodu.Globalization.Calendar.NotableDateFilter> *after* the cached result is
assembled, exactly as the engine does, so a filter never participates in the cache key. `GetSupportedTerritories`
and `GetSupportedCalendars` delegate straight to the wrapped service.

### The cache unit: one territory, one civil year

The engine resolves per Gregorian year, so a **whole civil year for one territory** is the reusable unit. Every
query is answered per year: a range is decomposed into the years it spans, each year is served from the cache when a
fresh, version-matching entry exists (or recomputed whole and written back), and the assembled occurrences are
clipped to the requested window. A later single-day query for a cached year never recomputes, and a query for
exactly one whole civil year — the `Resolve(year, territory)` extension shape — is served as the cached list
itself with no copying. Concurrent cold misses for the same year coalesce onto one computation instead of stampeding
the engine.

Territory keys are normalized case-insensitively (`us` and `US` share an entry); a subdivision (`AU-NSW`) and its
parent (`AU`) are distinct keys, matching the engine's own resolution.

### Freshness: time-to-live and resource version

A cached year stays fresh under two independent triggers, both evaluated by the cache on every call:

- **Time-to-live.** `Ttl` (default 30 days) expires an entry a fixed duration after it was computed — a coarse
  safety net, because resolution is deterministic for a given resource. Optional `TtlJitter` spreads per-territory
  expiries, and optional `RefreshAheadFraction` turns an aged hit into a served-now, recomputed-in-the-background
  entry so a hot territory never surfaces a miss.
- **Resource version.** Every entry is keyed by a version token. When the decorator observes an
  <xref:Bodu.Globalization.Calendar.INotableDateResourceProvider> (registered automatically by
  `AddReloadableNotableDateService`), the token is derived from the resource identity and a reload generation, so a
  `Reload(...)` invalidates every cached year on the next query regardless of the time-to-live. Without a provider,
  the fixed `ResourceVersion` from the options is used — bump it after a data update.

### The storage contract

<xref:Bodu.Globalization.Calendar.Caching.INotableDateCache> has three members:

| Member | Contract |
|---|---|
| `GetYear(territory, year, resourceVersion, ttl, asOf)` | Returns the <xref:Bodu.Globalization.Calendar.Caching.NotableDateCacheEntry> for that territory, year, and version **only while it is fresh** at `asOf`; otherwise `null`. |
| `StoreYear(entry, ttl, asOf)` | Merges a computed year into the territory's entries (most recent wins per year), prunes stale and superseded-version entries, and reports a <xref:Bodu.Globalization.Calendar.Caching.NotableDateCacheWriteStatus> — `Stored`, `Skipped` (a deliberate no-op cache), or `Failed` (a swallowed storage error; nothing persisted). |
| `Clear()` | Removes every cached entry, best-effort. |

Two rules bind every implementation: **ordering** — an entry's occurrences round-trip in the order supplied (the
engine's date-then-identity order), so the decorator assembles ranges without re-sorting — and **resilience** — a
storage fault surfaces as an empty read or a skipped write, never as an exception that breaks date resolution
(unless `ThrowOnStorageFailure` is set). Argument validation always throws.

## Headline types

### Service, contract, and entries

| Type | Purpose |
|---|---|
| <xref:Bodu.Globalization.Calendar.Caching.CachingNotableDateService> | The decorator. Constructor `(inner, cache, options, versionSource?, timeProvider?, loggerFactory?, ownsCache)`; the `INotableDateService` surface plus `Warm(territories, firstYear, lastYear)` to pre-pay year computations. Disposable — disposes the cache only when `ownsCache` is `true`. |
| <xref:Bodu.Globalization.Calendar.Caching.INotableDateCache> | The storage contract: `GetYear`, `StoreYear`, `Clear`. |
| <xref:Bodu.Globalization.Calendar.Caching.NotableDateCacheEntry> | The cache unit: `Territory`, `Year`, `ResourceVersion`, the ordered `Occurrences`, and `ComputedAtUtc`; `IsFresh(asOf, ttl)` evaluates the time-to-live. An empty occurrence list is a valid, cacheable result. |
| <xref:Bodu.Globalization.Calendar.Caching.NotableDateCacheWriteStatus> | `Stored` / `Skipped` / `Failed` — the outcome of a `StoreYear`. |

### Options

| Type | Purpose |
|---|---|
| <xref:Bodu.Globalization.Calendar.Caching.NotableDateCachingOptions> | Decorator options: `Ttl` (30 days), `TtlJitter` (`0`), `RefreshAheadFraction` (`0`), `ResourceVersion` (`null` → a built-in token), `CacheDirectory` (`null` → `bodu-notable-dates` under the temp path; used by the default file cache), `CacheHitLogLevel` / `CacheMissLogLevel` (`Information`). `Validate()` throws; `TryValidate(out error)` is what the DI registration wires into `ValidateOnStart`. |
| <xref:Bodu.Globalization.Calendar.Caching.NotableDateCacheOptions> | The storage-agnostic base every backend's options derive from: `ThrowOnStorageFailure` (`false` — rethrow `IOException` / `UnauthorizedAccessException` instead of degrading) and `ValidateStorageOnStart` (`false` — probe the store at construction or host start). |
| <xref:Bodu.Globalization.Calendar.Caching.FileNotableDateCacheOptions> | Adds `CacheDirectory` for the TOML and JSON file caches. |
| <xref:Bodu.Globalization.Calendar.Caching.SqliteNotableDateCacheOptions> | `DatabaseFilePath` or a full `ConnectionString` (the latter wins; at least one is required), `UseWriteAheadLogging` (`true`), `BusyTimeout` (5 s). |
| <xref:Bodu.Globalization.Calendar.Caching.DistributedNotableDateCacheOptions> | `KeyPrefix` (`null`; keys are `<prefix>notable-dates:<TERRITORY>`) and `EntryExpirationMargin` (1 hour — added to the time-to-live as each blob's server-side absolute expiration; `null` disables server-side expiry). |
| <xref:Bodu.Globalization.Calendar.Caching.NotableDateCacheWarmupOptions> | Startup warm-up: `Territories` (required), a rolling window of `YearsBehind` (`0`) / `YearsAhead` (`1`) around the current UTC year, or pinned `FirstYear` / `LastYear`. |

### Backends

| Type | Purpose |
|---|---|
| <xref:Bodu.Globalization.Calendar.Caching.NotableDateCacheBase`1> | The storage-agnostic mechanism behind every shipped backend: territory normalization, read-time freshness and version filtering, write-time merge-and-prune under a per-territory lock. Derived types persist only a territory's entry list. |
| <xref:Bodu.Globalization.Calendar.Caching.FileNotableDateCacheBase> | The file mechanism: one file per territory under `CacheDirectory`, atomic temp-and-move writes, a last-write-time parse memo, rate-limited degradation warnings. |
| <xref:Bodu.Globalization.Calendar.Caching.InMemoryNotableDateCache> | Process memory; empty on every start. |
| <xref:Bodu.Globalization.Calendar.Caching.TomlNotableDateCache> / <xref:Bodu.Globalization.Calendar.Caching.JsonNotableDateCache> | One `<TERRITORY>.toml` / `.json` file per territory; malformed content reads as empty and is repaired by the next write. |
| <xref:Bodu.Globalization.Calendar.Caching.SqliteNotableDateCache> | One `notable_dates` table keyed by `(territory, year, version)`; a single-row `GetYear`; a keep-alive connection for the instance lifetime. Disposable. |
| <xref:Bodu.Globalization.Calendar.Caching.DistributedNotableDateCache> | One JSON blob per territory in any `IDistributedCache`; `Clear` removes only the keys this instance wrote. |
| <xref:Bodu.Globalization.Calendar.Caching.NullNotableDateCache> | `NullNotableDateCache.Instance` — stores nothing; every write reports `Skipped`. |

### On-disk schema

The file caches and the distributed blob share one document shape; the SQLite backend stores the same occurrence rows
as a JSON blob per year.

| Type | Purpose |
|---|---|
| <xref:Bodu.Globalization.Calendar.Caching.NotableDateCacheFile> | The root: `Territory`, an `Entries` array, and a flat `Occurrences` array. |
| <xref:Bodu.Globalization.Calendar.Caching.NotableDateCacheYearRow> | One row per cached year — `Year`, `Version`, `ComputedAtUtc` — present even for a year that yielded no occurrences. |
| <xref:Bodu.Globalization.Calendar.Caching.NotableDateCacheOccurrenceRow> | One flat row per occurrence, carrying its `Year` and `Version` plus every <xref:Bodu.Globalization.Calendar.NotableDate> field (`Date`, `ActualDate`, `IsObserved`, the `ResourceId` / `NotableDateId` / `RuleId` identity, `DisplayName`, `TerritoryCode`, `Category`, `Priority`, `DurationDays`, `IsNonWorkingDay`, `Tags`, `AdjustmentPolicyId`, `AdjustmentReason`). |

### Dependency injection

All registrations live in the `Bodu.Globalization.Calendar` namespace.

| Method (static class) | Registers |
|---|---|
| `AddCachedNotableDateService(configure?, cacheFactory?)` and `AddCachedNotableDateService(configuration, sectionName = "Calendar:NotableDateCache", configure?, cacheFactory?)` (<xref:Bodu.Globalization.Calendar.NotableDateCachingExtensions>) | Decorates the already-registered `INotableDateService` **in place**: the previous registration becomes the inner service, consumers keep injecting `INotableDateService`. Without a `cacheFactory` a `TomlNotableDateCache` under `CacheDirectory` is created and owned. Throws `InvalidOperationException` when no service is registered. |
| `AddSqliteNotableDateCache(configuration?, sectionName = "Calendar:NotableDateCache:Sqlite", configure?)` (<xref:Bodu.Globalization.Calendar.SqliteNotableDateCacheExtensions>) | A singleton `SqliteNotableDateCache`, also exposed as `INotableDateCache`; validates options (and, with `ValidateStorageOnStart`, the database) at host start. |
| `AddDistributedNotableDateCache(configuration?, sectionName = "Calendar:NotableDateCache:Distributed", configure?)` and `AddRedisNotableDateCache(configureRedis, configuration?, sectionName, configure?)` (<xref:Bodu.Globalization.Calendar.DistributedNotableDateCacheExtensions>) | A singleton `DistributedNotableDateCache` over the container's `IDistributedCache`; the Redis form first registers the Redis cache. |
| `AddNotableDateCacheWarmup(configuration?, sectionName = "Calendar:NotableDateCacheWarmup", configure?)` (<xref:Bodu.Globalization.Calendar.NotableDateCacheWarmupExtensions>) | A hosted service that warms the configured territories after the host starts, without blocking it. |

> [!IMPORTANT]
> The SQLite and distributed registrations add an `INotableDateCache` to the container; they do **not** change what
> `AddCachedNotableDateService` builds by default. Hand the registered cache to the decorator explicitly:
> `AddCachedNotableDateService(cacheFactory: sp => sp.GetRequiredService<INotableDateCache>())`. See
> [Getting started](getting-started.md).

## Common scenarios

| Scenario | Reach for |
|---|---|
| Wrap a service in code, no files | `new CachingNotableDateService(engine, new InMemoryNotableDateCache(), new NotableDateCachingOptions())` |
| Persist across restarts on one machine | `new TomlNotableDateCache(new FileNotableDateCacheOptions { CacheDirectory = … })` or the SQLite backend |
| Decorate the DI-registered service | `services.AddCachedNotableDateService()` after `AddNotableDateService(...)` |
| Bind options from `appsettings.json` | `services.AddCachedNotableDateService(configuration)` — section `Calendar:NotableDateCache` |
| Invalidate automatically on reload | Register with `AddReloadableNotableDateService`; the decorator observes the resource provider |
| Invalidate after a data update without a provider | Bump `NotableDateCachingOptions.ResourceVersion` |
| Share one warm cache across instances | `AddRedisNotableDateCache(...)` + `AddCachedNotableDateService(cacheFactory: …)` |
| Never pay the first-request computation | `AddNotableDateCacheWarmup(...)`, or `service.Warm(territories, firstYear, lastYear)` |
| Fail the host when the store is broken | `ValidateStorageOnStart = true` (and `ThrowOnStorageFailure = true` for run-time faults) |
| Disable caching without changing wiring | `cacheFactory: _ => NullNotableDateCache.Instance` |

## Where to go next

- **[Core concepts](concepts.md)** — vocabulary: decorator vs service, cache key and unit, hit / miss / refresh, TTL vs resource version, warm-up, write status, storage failure policy, backend classes, observability, thread safety and lifetime.
- **[Getting started](getting-started.md)** — install and minimal samples: in-memory, TOML file, DI with `appsettings.json`, SQLite, Redis, warm-up, a custom `INotableDateCache`.
- **[Caching notable dates guide](../../guides/calendar/caching/notable-date-caching.md)** — worked patterns, freshness tuning, observability, troubleshooting.
- **[Calendar dependency injection](../../guides/calendar/dependency-injection.md)** and the **[DependencyInjection package](../calendar-di/index.md)** — the registrations the caching decorator wraps.
- **[Bodu.Globalization.Calendar.Caching API reference](xref:Bodu.Globalization.Calendar.Caching)** — full type-by-type docs.
- **[Runnable samples](../../samples/calendar.md)** — offline calendar sample projects.
- **[Globalization & Calendars topic](../topics/globalization-and-calendars.md)** — the runtime with its companion packages and data packs.
