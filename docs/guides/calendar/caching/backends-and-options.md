---
title: Cache backends and options
---

# Cache backends and options

[Caching notable dates](notable-date-caching.md) explains what the read-through decorator does. This page is the reference underneath it: every option type with its defaults, what each backend writes to disk, to SQLite, or to a distributed store, the write-status contract, the observability surface, and the composition rule that wires an add-on backend into `AddCachedNotableDateService`.

Every backend shares one policy — freshness, version matching, and merge-and-prune are applied identically by the core — so the choice between them is only about *where* entries live and *who* can see them.

## Options reference

### `NotableDateCachingOptions` — the decorator

<xref:Bodu.Globalization.Calendar.Caching.NotableDateCachingOptions> configures <xref:Bodu.Globalization.Calendar.Caching.CachingNotableDateService> itself. Under dependency injection it binds from the `Calendar:NotableDateCache` section.

| Property | Type | Default | Meaning |
|---|---|---|---|
| `Ttl` | `TimeSpan` | 30 days | A cached year expires this long after it was computed. Must be positive. |
| `TtlJitter` | `double` | `0` | Fraction in `[0, 1)` by which each territory's effective time-to-live is deterministically *shortened* (hash of the territory), so territories warmed together do not expire together. `0` disables. |
| `RefreshAheadFraction` | `double` | `0` | Fraction in `[0, 1)` of the effective time-to-live after which a hit is still served but one background recompute is scheduled. `0` disables. |
| `ResourceVersion` | `string?` | `null` | Fixed version token keying every entry; `null` uses a built-in default. Ignored when the decorator is given an `INotableDateResourceProvider`, which derives the token from the resource identity and reload generation. |
| `CacheDirectory` | `string?` | `null` | Directory for the **default** TOML file cache that `AddCachedNotableDateService` builds when no `cacheFactory` is supplied; `null` means `<temp>/bodu-notable-dates`. |
| `CacheHitLogLevel` | `LogLevel` | `Information` | Level of the per-year hit log (`EventId 4601`). |
| `CacheMissLogLevel` | `LogLevel` | `Information` | Level of the per-year miss log (`EventId 4602`). |

`Validate()` throws <xref:System.ArgumentException> and `TryValidate(out string? error)` reports the first violation; the DI registration validates on start.

<!-- compile -->
```csharp
var options = new NotableDateCachingOptions
{
    Ttl = TimeSpan.FromDays(14),
    TtlJitter = 0.1,
    RefreshAheadFraction = 0.75,
    ResourceVersion = "holidays-2026.1",
};

if (!options.TryValidate(out string? error))
    throw new InvalidOperationException(error);
```

### `NotableDateCacheOptions` — every backend

<xref:Bodu.Globalization.Calendar.Caching.NotableDateCacheOptions> is the base for every backend's options and the options type of `InMemoryNotableDateCache`.

| Property | Default | Meaning |
|---|---|---|
| `ThrowOnStorageFailure` | `false` | `false` keeps the contract's best-effort behaviour — a failed read is an empty read, a failed write is skipped and reported as `Failed`. `true` rethrows the storage exception as the store produced it (<xref:System.IO.IOException> or `UnauthorizedAccessException` for files; which one varies by platform). Argument validation always throws regardless. |
| `ValidateStorageOnStart` | `false` | `true` probes the backing store when the cache is constructed and throws if it is unusable, instead of discovering the fault on the first read or write. The SQLite and distributed registrations run this probe through options `ValidateOnStart`, so a misconfigured store fails host start-up. |

### `FileNotableDateCacheOptions` — TOML and JSON files

<xref:Bodu.Globalization.Calendar.Caching.FileNotableDateCacheOptions> adds one property to the base:

| Property | Default | Meaning |
|---|---|---|
| `CacheDirectory` | `null` → `<temp>/bodu-notable-dates` | Directory holding one file per territory. Created on first write. |

### `SqliteNotableDateCacheOptions`

`SqliteNotableDateCacheOptions` (in `Bodu.Globalization.Calendar.Caching.Sqlite`) binds from `Calendar:NotableDateCache:Sqlite` by default:

| Property | Default | Meaning |
|---|---|---|
| `DatabaseFilePath` | `null` | Path of the database file; created on first use. |
| `ConnectionString` | `null` | Full SQLite connection string; takes precedence over `DatabaseFilePath` and enables shapes such as a shared in-memory database (`Data Source=name;Mode=Memory;Cache=Shared`). At least one of the two is required. |
| `UseWriteAheadLogging` | `true` | Switches a file database to `PRAGMA journal_mode = WAL` on open, so readers do not block the writer. |
| `BusyTimeout` | 5 seconds | Applied as `PRAGMA busy_timeout`; must not be negative. |

### `DistributedNotableDateCacheOptions`

`DistributedNotableDateCacheOptions` (in `Bodu.Globalization.Calendar.Caching.Distributed`) binds from `Calendar:NotableDateCache:Distributed` by default:

| Property | Default | Meaning |
|---|---|---|
| `KeyPrefix` | `null` | Prepended verbatim to every key so several logical caches can share one store; `null` for no prefix. White-space-only is rejected. |
| `EntryExpirationMargin` | 1 hour | Each territory blob is written with an absolute server-side expiration of `Ttl + margin`, so an unqueried territory self-evicts. `null` disables server-side expiry; negative is rejected. |

### `NotableDateCacheWarmupOptions` — start-up warm-up

<xref:Bodu.Globalization.Calendar.Caching.NotableDateCacheWarmupOptions> binds from `Calendar:NotableDateCacheWarmup`:

| Property | Default | Meaning |
|---|---|---|
| `Territories` | empty | The territories to warm. Empty fails validation, so an unconfigured warm-up cannot register. |
| `YearsBehind` | `0` | Rolling window: first year = current UTC year − `YearsBehind`. Must not be negative. |
| `YearsAhead` | `1` | Rolling window: last year = current UTC year + `YearsAhead`. Must not be negative. |
| `FirstYear` | `null` | Pins the first year, replacing the rolling default independently of `LastYear`. |
| `LastYear` | `null` | Pins the last year. An inverted window (`LastYear` < `FirstYear`) fails validation. |

## The on-disk file schema (TOML and JSON)

<xref:Bodu.Globalization.Calendar.Caching.TomlNotableDateCache> and <xref:Bodu.Globalization.Calendar.Caching.JsonNotableDateCache> write **one file per territory** — `<CacheDirectory>/<TERRITORY>.toml` or `.json`, the territory upper-cased and any character other than an ASCII letter, digit, or `-` replaced by `_`. Both serialize the same shallow document, <xref:Bodu.Globalization.Calendar.Caching.NotableDateCacheFile>: a `Territory`, an `Entries` array of <xref:Bodu.Globalization.Calendar.Caching.NotableDateCacheYearRow> (one per cached year, *including* a year that yielded nothing), and a flat `Occurrences` array of <xref:Bodu.Globalization.Calendar.Caching.NotableDateCacheOccurrenceRow>, each row carrying its own `Year` and `Version` so it can be associated back to its entry without nesting.

The file below was produced by resolving July 2026 for `US` with `ResourceVersion = "us-2026.1"` (it continues for all 29 occurrences of the year):

```toml
Territory = "US"

[[Entries]]
Year = 2026
Version = "us-2026.1"
ComputedAtUtc = 2026-09-09T06:16:45.4371656Z

[[Occurrences]]
Year = 2026
Version = "us-2026.1"
Date = 2026-01-01
ActualDate = 2026-01-01
IsObserved = false
ResourceId = "data.us"
NotableDateId = "new-years-day"
RuleId = "default"
DisplayName = "New Year's Day"
TerritoryCode = "US"
Category = "PublicHoliday"
Priority = 0
DurationDays = 1
IsNonWorkingDay = true
Tags = []

[[Occurrences]]
Year = 2026
Version = "us-2026.1"
Date = 2026-01-19
ActualDate = 2026-01-19
IsObserved = false
ResourceId = "data.us"
NotableDateId = "mlk-day"
RuleId = "us"
DisplayName = "Birthday of Martin Luther King, Jr."
TerritoryCode = "US"
Category = "PublicHoliday"
Priority = 0
DurationDays = 1
IsNonWorkingDay = true
Tags = []
```

The JSON backend writes the identical structure with `System.Text.Json` web defaults (camel-case names, `null` for absent optional fields):

```json
{
  "territory": "US",
  "entries": [
    { "year": 2026, "version": "us-2026.1", "computedAtUtc": "2026-09-09T06:16:45.611935+00:00" }
  ],
  "occurrences": [
    {
      "year": 2026, "version": "us-2026.1",
      "date": "2026-01-01", "actualDate": "2026-01-01", "isObserved": false,
      "resourceId": "data.us", "notableDateId": "new-years-day", "ruleId": "default",
      "displayName": "New Year's Day", "territoryCode": "US", "category": "PublicHoliday",
      "priority": 0, "durationDays": 1, "isNonWorkingDay": true, "tags": [],
      "adjustmentPolicyId": null, "adjustmentReason": null
    }
  ]
}
```

Occurrence rows flatten the rule identity (`ResourceId`, `NotableDateId`, `RuleId`) and store the category by enum name; `AdjustmentPolicyId` / `AdjustmentReason` appear only when an adjustment applied (TOML omits them, JSON writes `null`). A corrupt or unreadable file is treated as empty and logged once (`EventId 4604`), and a whole-file rewrite happens on every store, so the file is always internally consistent.

## The SQLite table

`SqliteNotableDateCache` creates its single table on first open and keys it by the same triple the contract uses:

```sql
CREATE TABLE IF NOT EXISTS notable_dates (
    territory     TEXT NOT NULL,
    year          INTEGER NOT NULL,
    version       TEXT NOT NULL,
    computed_at   TEXT NOT NULL,
    occurrences   TEXT NOT NULL,
    PRIMARY KEY (territory, year, version)
);
```

`computed_at` is the invariant round-trip text of the UTC instant and `occurrences` is the JSON array of occurrence rows for that year (the same row shape as the file schema, without the redundant `year`/`version` columns). A store deletes the territory's rows and re-inserts the merged set inside one transaction; a single-year read (`GetYear`) is answered from one keyed row without loading the territory. `Clear()` runs `DELETE FROM notable_dates`.

## The distributed key format

`DistributedNotableDateCache` stores **one blob per territory** under the key `{KeyPrefix}notable-dates:{TERRITORY}` — with no prefix, `notable-dates:US`; with `KeyPrefix = "app1:"`, `app1:notable-dates:US`. The blob is the JSON form of the file schema above, written with an absolute expiration of `Ttl + EntryExpirationMargin` when the margin is non-`null`. `Clear()` removes the keys *this instance* has written; it cannot enumerate an `IDistributedCache`, so keys written by other processes are left to expire.

## `NotableDateCacheWriteStatus`

Every `StoreYear` answers a <xref:Bodu.Globalization.Calendar.Caching.NotableDateCacheWriteStatus> so a caller can tell whether the computed year actually landed:

| Value | Meaning |
|---|---|
| `Stored` | Persisted; durable for the backend's lifetime (memory: the process; files/SQLite: the disk; distributed: until server-side expiry). |
| `Skipped` | The cache deliberately stores nothing — <xref:Bodu.Globalization.Calendar.Caching.NullNotableDateCache> — and nothing was expected to be stored. |
| `Failed` | A storage error was swallowed (`ThrowOnStorageFailure = false`); the next lookup of that year recomputes rather than trusting a write that never happened. |

<!-- compile -->
```csharp
INotableDateService engine = AmericasCalendarData.CreateService("US");
var cache = new InMemoryNotableDateCache();

var entry = new NotableDateCacheEntry("US", 2027, "us-2026.1", engine.Resolve(2027, "US"), DateTimeOffset.UtcNow);
NotableDateCacheWriteStatus status = cache.StoreYear(entry, TimeSpan.FromDays(30), DateTimeOffset.UtcNow);   // Stored

NotableDateCacheWriteStatus skipped = NullNotableDateCache.Instance.StoreYear(entry, TimeSpan.FromDays(30), DateTimeOffset.UtcNow);   // Skipped
```

## Observability

Logging and metrics are described in full in [Caching notable dates — Observability](notable-date-caching.md#observability); the per-backend identifiers are:

| Backend | Storage-failure `EventId` | Meter | Instrument |
|---|---|---|---|
| File (TOML/JSON) | 4603 (swallowed) / 4604 (corrupt file treated as empty) | `Bodu.Globalization.Calendar.Caching` | `bodu.calendar.notable_date_cache.storage_failures` (tag `operation`) |
| SQLite | 4611 | `Bodu.Globalization.Calendar.Caching.Sqlite` | `bodu.calendar.notable_date_cache.sqlite.storage_failures` |
| Distributed | 4621 | `Bodu.Globalization.Calendar.Caching.Distributed` | `bodu.calendar.notable_date_cache.distributed.storage_failures` |

The decorator's own events are 4601 (hit) and 4602 (miss) at the configured levels, 4605/4606 (refresh-ahead completed/failed), 4607 (warm-up territory skipped), and 4622–4625 for the hosted warm-up service. Storage-failure warnings are rate-limited to one per minute with the suppressed count attached; the counters are not rate-limited.

## Composition: wiring an add-on backend

`AddCachedNotableDateService` wraps the already registered `INotableDateService` in the decorator. With no `cacheFactory` it constructs a `TomlNotableDateCache` in `CacheDirectory` and owns its lifetime. The SQLite and distributed registrations each add an `INotableDateCache` singleton to the container **but do not replace that default** — the decorator only uses them when you point it at the registered service:

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Caching;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddNotableDateService(AmericasCalendarData.LoadResource("US"));

// One of:
services.AddSqliteNotableDateCache(configure: o => o.DatabaseFilePath = "/var/cache/notable-dates.db");
// services.AddDistributedNotableDateCache(configure: o => o.KeyPrefix = "app1:");
// services.AddRedisNotableDateCache(redis => redis.Configuration = "localhost:6379");

// The composition rule: hand the registered cache to the decorator.
services.AddCachedNotableDateService(
    configure: o => o.Ttl = TimeSpan.FromDays(7),
    cacheFactory: sp => sp.GetRequiredService<INotableDateCache>());
```

The same rule applies to a cache you construct yourself or register by hand — `cacheFactory: _ => new JsonNotableDateCache(new FileNotableDateCacheOptions { CacheDirectory = "/var/cache/notable-dates" })` — and to the custom backends described in [Writing a cache backend](custom-backend.md). When a factory is supplied the container does not dispose the cache; register it as a singleton (as the add-ons do) so its lifetime is managed.

Binding the same wiring from configuration:

```json
{
  "Calendar": {
    "NotableDateCache": {
      "Ttl": "7.00:00:00",
      "TtlJitter": 0.1,
      "Sqlite": { "DatabaseFilePath": "/var/cache/notable-dates.db", "BusyTimeout": "00:00:10" },
      "Distributed": { "KeyPrefix": "app1:", "EntryExpirationMargin": "02:00:00" }
    },
    "NotableDateCacheWarmup": { "Territories": [ "US", "US-CA" ], "YearsAhead": 2 }
  }
}
```

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

void Register(IServiceCollection services, IConfiguration configuration)
{
    services.AddNotableDateService(AmericasCalendarData.LoadResource("US"));
    services.AddSqliteNotableDateCache(configuration);                                    // Calendar:NotableDateCache:Sqlite
    services.AddCachedNotableDateService(configuration,                                    // Calendar:NotableDateCache
        cacheFactory: sp => sp.GetRequiredService<INotableDateCache>());
    services.AddNotableDateCacheWarmup(configuration);                                     // Calendar:NotableDateCacheWarmup
}
```

## Where to go next

- **[Caching notable dates](notable-date-caching.md)** — the concepts, quick-start, freshness, warm-up, and troubleshooting.
- **[Writing a cache backend](custom-backend.md)** — the `INotableDateCache` contract, the invariants, and a complete in-memory backend.
- **[Calendar dependency injection](../dependency-injection.md)** — registering the service the cache decorates.
- **[Bodu.Globalization.Calendar.Caching API reference](xref:Bodu.Globalization.Calendar.Caching)**
- **[Globalization & Calendars guides](../../topics/globalization-and-calendars.md)** — every guide in this topic: the runtime, companions, data packs, and the notable-date catalogue.
