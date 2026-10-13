# Bodu.Globalization.Calendar.Samples.Caching

Demonstrates the calendar caching layer: the read-through `CachingNotableDateService` decorator
over the in-memory, file, SQLite and `IDistributedCache` backends, explicit cache warm-up of a
serving window, and the dependency-injection registration that decorates an already-registered
`INotableDateService`.

A `CountingNotableDateService` wrapper counts the resolutions that reach the real engine, so every
cache hit below is proved deterministically by call count rather than by timing.

```bash
dotnet run --project samples/Globalization.Calendar/Bodu.Globalization.Calendar.Samples.Caching
```

## Scenario 1 - ReadThroughCaching

**Intent.** Show what the cache stores and why a whole civil year is the right unit - and that
filters deliberately stay out of the cache key.

**What it does.** Resolves a whole year, repeats the query, asks for a sub-range inside that year,
and runs a filtered query over it, reporting the engine counter each time.

**What to expect.**

```text
  Cold whole-year query: 22 occurrences, engine resolutions = 1  (the cold path: one engine resolution, stored as a single whole-year entry)
  Warm whole-year query: engine resolutions = 1  (still 1 - the count not moving is the assertion, proved by call count rather than by timing)
  April sub-range from cache: 6 occurrences, engine resolutions = 1  (a clip of the cached year, not a miss - which is why a whole year is the right unit to store)
  Filtered query from cache: 8 non-working dates, engine resolutions = 1  (filters are applied after the cache, never keyed into it - otherwise two callers with different filters would each pay full price)
```

A year is the unit rules are evaluated in, so it is the smallest thing worth storing - which is
what makes a sub-range query a clip of something already computed rather than a miss. Keying on
the filter would multiply entries for what is one result list filtered differently, so two
callers with different filters would each pay full price.

**APIs demonstrated.** `CachingNotableDateService`, `InMemoryNotableDateCache`,
`NotableDateCachingOptions`, the filtered `Resolve` overload over a cached year.

## Scenario 2 - FileBackedCaches

**Intent.** An in-memory cache is empty at every process start - exactly when a service is least
able to absorb the work. Show the file backends surviving the restart.

**What it does.** Resolves a year through the JSON cache and lists the files written, then builds a
completely fresh service (with its own counter) over the same directory and resolves the same
range, then writes the same data through the TOML backend.

**What to expect.**

```text
  First instance resolved from the engine: 1 resolution(s)  (the cold path, and the only time the rules run)
    cache file: NZ.json
  (one file per territory, holding its cached years - this is what survives the process exit)
  Second instance served 19 occurrences with 0 engine resolution(s)  (expected 0 - a brand-new service with its own counter, standing in for a new process, starts warm)
    toml cache file: NZ.toml
  (a constructor swap and nothing else - worth it when the cache is committed or inspected, since TOML diffs legibly)
```

The second service instance stands in for a new process, which is what makes the claim testable
offline. Two formats ship because the choice is about who reads the file: TOML, which
`AddCachedNotableDateService` writes by default, is worth having when the cache is committed or
inspected, since it diffs legibly.

**APIs demonstrated.** `JsonNotableDateCache`, `TomlNotableDateCache`,
`FileNotableDateCacheOptions.CacheDirectory`.

## Scenario 3 - DurableBackends

**Intent.** The in-memory cache dies with the process. Show the two add-on backends: a SQLite file
a cold-starting process carries between runs, and an `IDistributedCache` that several processes
share.

**What it does.** Resolves a year through a `SqliteNotableDateCache`, disposes the service, builds a
second one over the same database file and repeats the query; then does the same with two services
over one `DistributedNotableDateCache`, counting engine resolutions throughout. The distributed half
uses `MemoryDistributedCache`, so the sample stays offline.

**What to expect.**

```text
  SQLite  cold  :  22 occurrences, engine resolutions = 1   (computed once and written to the database file)
  SQLite  warm  :  22 occurrences, engine resolutions = 1   (a NEW service over the same file - the count did not move, so this came off disk)
  database file : 4096 bytes on disk   (what a cold-start CLI or function carries between runs)

  shared  node A:  22 occurrences, engine resolutions = 1   (first node through: computed and published to the shared store)
  shared  node B:  22 occurrences, engine resolutions = 1   (a second node - it read what node A published rather than recomputing)
  (swap MemoryDistributedCache for a Redis-backed IDistributedCache and nothing above changes)
```

Both backends sit behind the same `INotableDateCache` contract, so choosing between them is a
composition decision at startup rather than a change to calling code.

**APIs demonstrated.** `SqliteNotableDateCache`, `DistributedNotableDateCache`,
`DistributedNotableDateCacheOptions.KeyPrefix`.

## Scenario 4 - WarmUp

**Intent.** A read-through cache moves the cost rather than removing it, and the caller who pays
is whoever asks first. Show warming paying it deliberately instead.

**What it does.** Pre-resolves two territories across two civil years, then runs two queries inside
that window, the first straddling a year boundary, checking the engine counter before and after.

**What to expect.**

```text
  Warmed 2 territories; engine resolutions during warm-up = 4  (all the engine work, paid deliberately at start-up rather than by whoever asks first)
  Two cross-window queries served; engine resolutions still = 4  (unchanged - the first query straddled a year boundary and found both cached years, the second found its one)
```

The window is (territory, year) pairs because that is the cache's unit, so warming is exactly as
granular as the cache is - nothing wasted and no gaps. In a hosted application the same thing
runs as a background service with a rolling window; `AddNotableDateCacheWarmup` is sketched in the
scenario's closing comment, and no-ops with a log message when the registered service is not the
caching decorator.

**APIs demonstrated.** `CachingNotableDateService.Warm(territories, firstYear, lastYear)`, and the
commented `AddNotableDateCacheWarmup` hosted-service form.

## Scenario 5 - DiRegistration

**Intent.** Caching is a deployment decision, not an application one. Show it registered as a
decorator so consumers never learn whether a cache exists.

**What it does.** Registers a data-pack service, adds the caching decorator with a seven-day TTL,
resolves `INotableDateService` from the container, and reports the concrete type that came back.

**What to expect.**

```text
  Resolved service type: CachingNotableDateService  (the decorator, not the data-pack service - the registration wrapped what was already there)
  AU 2026-04-25: Anzac Day  (identical to the uncached result - consumers inject the interface and never learn a cache exists)
```

The TTL matters because the cache holds computed rule output and rule data can be republished - so
the entry has to expire even though the calculation itself is deterministic. Without a
`cacheFactory`, the registration writes a TOML file cache under the temp folder's
`bodu-notable-dates` directory. The durable backends slot into the same call through
`cacheFactory`, which is why moving to SQLite or Redis is a registration line rather than a
refactor; the scenario's closing comment shows those registrations, and the scenario keeps the
default backend.

**APIs demonstrated.** `AddCachedNotableDateService` (with `configure`),
`NotableDateCachingOptions.Ttl`, and the commented `AddSqliteNotableDateCache` /
`AddDistributedNotableDateCache` / `AddRedisNotableDateCache` registrations handed to
`cacheFactory`.

## Layout

```text
Bodu.Globalization.Calendar.Samples.Caching/
  Program.cs                          # runs the scenarios in order
  SampleConsole.cs                    # the What / Why / Expect scenario banner
  CountingNotableDateService.cs       # counts resolutions reaching the real engine
  Scenarios/ReadThroughCaching.cs
  Scenarios/FileBackedCaches.cs
  Scenarios/DurableBackends.cs
  Scenarios/WarmUp.cs
  Scenarios/DiRegistration.cs
```

## NuGet equivalents

```bash
dotnet add package Bodu.Globalization.Calendar
dotnet add package Bodu.Globalization.Calendar.Caching
dotnet add package Bodu.Globalization.Calendar.DependencyInjection
dotnet add package Bodu.Globalization.Calendar.AsiaPacific
# the durable backends (the DurableBackends scenario):
dotnet add package Bodu.Globalization.Calendar.Caching.Sqlite
dotnet add package Bodu.Globalization.Calendar.Caching.Distributed
dotnet add package Microsoft.Extensions.Caching.Memory   # MemoryDistributedCache, the offline IDistributedCache
```
