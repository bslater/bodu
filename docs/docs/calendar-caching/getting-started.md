---
title: Bodu.Globalization.Calendar.Caching — Getting started
---

# Bodu.Globalization.Calendar.Caching — Getting started

Unfamiliar with terms like *decorator*, *cache unit*, *resource version*, *refresh-ahead*, *warm-up*, or *storage
failure policy*? Read [Core concepts](concepts.md) first.

## Install

```bash
dotnet add package Bodu.Globalization.Calendar.Caching

# Optional durable backends:
dotnet add package Bodu.Globalization.Calendar.Caching.Sqlite
dotnet add package Bodu.Globalization.Calendar.Caching.Distributed
```

Targets `net8.0`. Depends on:

- **`Bodu.Globalization.Calendar.Caching`** — `Bodu.Core`, `Bodu.Globalization.Calendar` (the service contract it
  decorates), `Bodu.Text.Toml` (the TOML file cache), and `Microsoft.Extensions.Configuration.Abstractions` /
  `.Configuration.Binder` / `.DependencyInjection.Abstractions` / `.Hosting.Abstractions` /
  `.Logging.Abstractions` / `.Options` / `.Options.ConfigurationExtensions`, all at the .NET 8.0 LTS line.
- **`Bodu.Globalization.Calendar.Caching.Sqlite`** — the core caching package plus `Microsoft.Data.Sqlite`.
- **`Bodu.Globalization.Calendar.Caching.Distributed`** — the core caching package plus
  `Microsoft.Extensions.Caching.Abstractions` and `Microsoft.Extensions.Caching.StackExchangeRedis`.

The samples below resolve the wrapped service from a regional data pack (`Bodu.Globalization.Calendar.Americas` /
`.AsiaPacific`); any <xref:Bodu.Globalization.Calendar.INotableDateService> works in its place.

## Minimal samples

### Decorate a service with the in-memory cache

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Caching;

INotableDateService engine = AmericasCalendarData.CreateService("US");

using var calendar = new CachingNotableDateService(
    engine,
    new InMemoryNotableDateCache(),
    new NotableDateCachingOptions { Ttl = TimeSpan.FromDays(7) });

// First call computes the whole of 2026 through the engine and caches it.
IReadOnlyList<NotableDate> year = calendar.Resolve(2026, "US");

// Same civil year: served from the cache, clipped to the day.
IReadOnlyList<NotableDate> independenceDay = calendar.Resolve(new DateOnly(2026, 7, 4), "US");

// A filter is applied after assembly, so it shares the cached year.
IReadOnlyList<NotableDate> publicHolidays = calendar.Resolve(
    2026, "US", NotableDateFilter.ForCategory(NotableDateCategory.PublicHoliday));
```

`CachingNotableDateService` implements `INotableDateService`, so the by-year `Resolve(year, territory)` extension
and the single-day, range, and filtered overloads all work unchanged. The in-memory cache starts empty on every
process start; the entries expire by the options' time-to-live and by resource version like every other backend.

### Persist to TOML files

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Caching;

var cache = new TomlNotableDateCache(new FileNotableDateCacheOptions
{
    CacheDirectory = "/var/cache/notable-dates",    // one <TERRITORY>.toml file per territory
    ValidateStorageOnStart = true,                  // create the directory now; throw if that fails
});

using var calendar = new CachingNotableDateService(
    AmericasCalendarData.CreateService("US"),
    cache,
    new NotableDateCachingOptions { ResourceVersion = "us-holidays-2026.1" },
    ownsCache: true);                               // dispose the cache with the decorator

IReadOnlyList<NotableDate> year = calendar.Resolve(2026, "US");   // written to /var/cache/notable-dates/US.toml
```

Leave `CacheDirectory` as `null` to use a `bodu-notable-dates` folder under the system temporary path. Swap
`TomlNotableDateCache` for `JsonNotableDateCache` to write JSON with the same options and layout. Without a resource
provider the fixed `ResourceVersion` keys every entry — bump it after a data update so stale years recompute.

### Invalidate on reload with a resource provider

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Caching;

var resourceProvider = new MutableNotableDateResourceProvider(AsiaPacificCalendarData.LoadResource("AU"));

using var calendar = new CachingNotableDateService(
    new ReloadableNotableDateService(resourceProvider),
    new InMemoryNotableDateCache(),
    new NotableDateCachingOptions(),
    versionSource: resourceProvider);

_ = calendar.Resolve(2026, "AU-NSW");                              // computes and caches

resourceProvider.Reload(AsiaPacificCalendarData.LoadResource("AU"));

_ = calendar.Resolve(2026, "AU-NSW");                              // version changed: recomputes
```

The decorator derives the version token from the provider's current resource and a reload generation, so a reload
invalidates every cached year on the next query — even when the reloaded resource carries the same identifier.

### Register under dependency injection

`AddCachedNotableDateService` decorates the `INotableDateService` already in the container, so it must come *after*
the service registration (it throws `InvalidOperationException` otherwise). Consumers keep injecting
`INotableDateService`. Passing an `IConfiguration` binds
<xref:Bodu.Globalization.Calendar.Caching.NotableDateCachingOptions> from the `Calendar:NotableDateCache` section:

```csharp
using Bodu.Globalization.Calendar;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNotableDateService(AmericasCalendarData.LoadResource("US"));
builder.Services.AddCachedNotableDateService(builder.Configuration);
```

Every bindable key of the section, with its default:

```json
{
  "Calendar": {
    "NotableDateCache": {
      "Ttl": "30.00:00:00",
      "TtlJitter": 0.0,
      "RefreshAheadFraction": 0.0,
      "ResourceVersion": null,
      "CacheDirectory": null,
      "CacheHitLogLevel": "Information",
      "CacheMissLogLevel": "Information"
    }
  }
}
```

`Ttl` is a `TimeSpan` in the standard `d.hh:mm:ss` form; `TtlJitter` and `RefreshAheadFraction` are fractions in
`[0, 1)`; the two log levels are `Microsoft.Extensions.Logging.LogLevel` names. The options are validated at host
start (`ValidateOnStart`), so a non-positive `Ttl` or an out-of-range fraction fails startup rather than the first
request. Without a `cacheFactory` the registration builds a `TomlNotableDateCache` under `CacheDirectory` and owns
it; the code-only overload takes the same `configure` and `cacheFactory` arguments without configuration binding:

```csharp
builder.Services.AddCachedNotableDateService(
    configure: o =>
    {
        o.Ttl = TimeSpan.FromDays(7);
        o.RefreshAheadFraction = 0.75;
    },
    cacheFactory: _ => new InMemoryNotableDateCache());
```

When the wrapped service was registered with `AddReloadableNotableDateService`, the decorator picks up the
container's <xref:Bodu.Globalization.Calendar.INotableDateResourceProvider> automatically and a `Reload(...)`
invalidates the cache. A registered `TimeProvider` and `ILoggerFactory` are used when present.

### Use the SQLite backend

`AddSqliteNotableDateCache` registers a singleton <xref:Bodu.Globalization.Calendar.Caching.SqliteNotableDateCache>
as `INotableDateCache` and binds <xref:Bodu.Globalization.Calendar.Caching.SqliteNotableDateCacheOptions> from
`Calendar:NotableDateCache:Sqlite`. It does not change what `AddCachedNotableDateService` builds by default, so hand
the registered cache to the decorator through `cacheFactory`:

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Caching;
using Microsoft.Extensions.DependencyInjection;

builder.Services.AddNotableDateService(AmericasCalendarData.LoadResource("US"));
builder.Services.AddSqliteNotableDateCache(builder.Configuration);
builder.Services.AddCachedNotableDateService(
    builder.Configuration,
    cacheFactory: sp => sp.GetRequiredService<INotableDateCache>());
```

```json
{
  "Calendar": {
    "NotableDateCache": {
      "Ttl": "30.00:00:00",
      "Sqlite": {
        "DatabaseFilePath": "/var/cache/notable-dates.db",
        "ConnectionString": null,
        "UseWriteAheadLogging": true,
        "BusyTimeout": "00:00:05",
        "ThrowOnStorageFailure": false,
        "ValidateStorageOnStart": true
      }
    }
  }
}
```

Either `DatabaseFilePath` or a full `ConnectionString` is required (the connection string wins when both are set —
use it for a shared in-memory database such as `Data Source=holidays;Mode=Memory;Cache=Shared`). With
`ValidateStorageOnStart` the registration opens and initializes the database during `ValidateOnStart`, so an
unwritable path fails the host start. The container disposes the cache — and its keep-alive connection — on
shutdown.

### Use Redis (or any `IDistributedCache`)

`AddRedisNotableDateCache` registers the Redis `IDistributedCache` and a
<xref:Bodu.Globalization.Calendar.Caching.DistributedNotableDateCache> over it in one call;
`AddDistributedNotableDateCache` registers only the notable-date cache over whatever `IDistributedCache` the
container already has. Both bind <xref:Bodu.Globalization.Calendar.Caching.DistributedNotableDateCacheOptions> from
`Calendar:NotableDateCache:Distributed`:

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Caching;
using Microsoft.Extensions.DependencyInjection;

builder.Services.AddNotableDateService(AmericasCalendarData.LoadResource("US"));
builder.Services.AddRedisNotableDateCache(
    redis => redis.Configuration = "localhost:6379",
    builder.Configuration);
builder.Services.AddCachedNotableDateService(
    builder.Configuration,
    cacheFactory: sp => sp.GetRequiredService<INotableDateCache>());
```

```json
{
  "Calendar": {
    "NotableDateCache": {
      "Distributed": {
        "KeyPrefix": "payroll:",
        "EntryExpirationMargin": "01:00:00",
        "ThrowOnStorageFailure": false,
        "ValidateStorageOnStart": true
      }
    }
  }
}
```

Keys take the form `<KeyPrefix>notable-dates:<TERRITORY>`, so several logical caches can share one Redis instance.
Each written territory blob is stamped with a server-side lifetime of the time-to-live plus `EntryExpirationMargin`
(default one hour), so a territory that stops being queried self-evicts; set the margin to `null` to write without
server-side expiry. For a non-Redis store, register it yourself and call `AddDistributedNotableDateCache()`:

```csharp
builder.Services.AddDistributedMemoryCache();          // or AddDistributedSqlServerCache, …
builder.Services.AddDistributedNotableDateCache(configure: o => o.KeyPrefix = "payroll:");
```

### Warm the cache at startup

```csharp
using Bodu.Globalization.Calendar;

builder.Services.AddNotableDateService(AmericasCalendarData.LoadResource("US"));
builder.Services.AddCachedNotableDateService(builder.Configuration);
builder.Services.AddNotableDateCacheWarmup(builder.Configuration);
```

```json
{
  "Calendar": {
    "NotableDateCacheWarmup": {
      "Territories": [ "US", "US-CA", "US-NY" ],
      "YearsBehind": 0,
      "YearsAhead": 1,
      "FirstYear": null,
      "LastYear": null
    }
  }
}
```

The hosted service runs after the host has started and never blocks it: each territory is warmed over the span
(current UTC year − `YearsBehind` … current year + `YearsAhead`, or the pinned `FirstYear` / `LastYear`) through the
normal read-through path, a failing territory is logged and skipped, and shutdown cancels the run between
territories. `Territories` must be non-empty or startup validation fails. Register it *after*
`AddCachedNotableDateService`; otherwise the run finds a non-caching service, logs a warning, and no-ops. The same
work is available in code as `calendar.Warm(new[] { "US", "US-CA" }, 2026, 2027)`, which returns the number of
territories warmed.

### Implement a custom cache

Any store can back the decorator by implementing <xref:Bodu.Globalization.Calendar.Caching.INotableDateCache>. The
contract has three obligations beyond the signatures: serve an entry only while it is fresh under the supplied
`ttl` / `asOf` and matches the requested version, preserve the order of an entry's occurrences, and degrade to an
empty read or a `Failed` write rather than throwing on a storage fault. This sketch keeps whole entries in a
dictionary and prunes a territory's superseded versions on write:

```csharp
using System.Collections.Concurrent;
using Bodu.Globalization.Calendar.Caching;

public sealed class DictionaryNotableDateCache : INotableDateCache
{
    private readonly ConcurrentDictionary<(string Territory, int Year), NotableDateCacheEntry> _entries = new();

    public NotableDateCacheEntry? GetYear(string territory, int year, string resourceVersion, TimeSpan ttl, DateTimeOffset asOf)
    {
        ArgumentNullException.ThrowIfNull(territory);
        ArgumentNullException.ThrowIfNull(resourceVersion);

        return _entries.TryGetValue((Normalize(territory), year), out NotableDateCacheEntry? entry)
            && string.Equals(entry.ResourceVersion, resourceVersion, StringComparison.Ordinal)
            && entry.IsFresh(asOf, ttl)
            ? entry
            : null;
    }

    public NotableDateCacheWriteStatus StoreYear(NotableDateCacheEntry entry, TimeSpan ttl, DateTimeOffset asOf)
    {
        ArgumentNullException.ThrowIfNull(entry);

        string territory = Normalize(entry.Territory);

        // A new resource version supersedes every year cached for the territory under the old one.
        foreach (KeyValuePair<(string Territory, int Year), NotableDateCacheEntry> existing in _entries)
        {
            if (existing.Key.Territory == territory
                && !string.Equals(existing.Value.ResourceVersion, entry.ResourceVersion, StringComparison.Ordinal))
            {
                _entries.TryRemove(existing.Key, out _);
            }
        }

        _entries[(territory, entry.Year)] = entry;
        return NotableDateCacheWriteStatus.Stored;
    }

    public void Clear() =>
        _entries.Clear();

    private static string Normalize(string territory) =>
        territory.Trim().ToUpperInvariant();
}
```

Plug it in exactly like a shipped backend — `new CachingNotableDateService(engine, new DictionaryNotableDateCache(),
options)` or `AddCachedNotableDateService(cacheFactory: _ => new DictionaryNotableDateCache())`. The shipped
backends additionally derive from <xref:Bodu.Globalization.Calendar.Caching.NotableDateCacheBase`1>, which supplies
the merge-and-prune, validity, and per-territory locking; that storage seam is opened to the companion SQLite and
distributed packages, so a third-party backend implements the interface directly as above.

## Where to go next

- **[Core concepts](concepts.md)** — vocabulary refresher.
- **[Introduction](index.md)** — package family, type map, and scenario index.
- **[Caching notable dates guide](../../guides/calendar/caching/notable-date-caching.md)** — freshness tuning, observability, troubleshooting.
- **[Calendar dependency injection](../../guides/calendar/dependency-injection.md)** — the registrations the decorator wraps, including the reloadable and keyed forms.
- **[Bodu.Globalization.Calendar.Caching API reference](xref:Bodu.Globalization.Calendar.Caching)** — full type-by-type docs.
- **[Runnable samples](../../samples/calendar.md)** — offline calendar sample projects you can `dotnet run`.
