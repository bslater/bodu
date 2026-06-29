---
title: Caching and aggregating exchange rates
---

# Caching and aggregating exchange rates

`Bodu.Financial.ExchangeRates.Caching` adds two pieces **in front of** the
exchange-rate providers. The concrete providers (Yahoo, OFX, RBA, ECB, BoE) stay pure
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
  and [`JsonFileExchangeRateCache`](xref:Bodu.Financial.ExchangeRates.Caching.JsonFileExchangeRateCache)
  (on disk, with a configurable [layout and date partitioning](#file-layouts-and-date-partitioning)),
  [`InMemoryExchangeRateCache`](xref:Bodu.Financial.ExchangeRates.Caching.InMemoryExchangeRateCache),
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

The decorator is `IDisposable`. By default it does **not** dispose the inner
provider — under dependency injection the container owns it, and a hand-composed
inner is owned by whoever created it. Pass `ownsInner: true` to make disposing the
decorator also dispose a disposable inner (for example a provider that built its
own `HttpClient`):

```csharp
using var cached = new CachingExchangeRateProvider(
    "RBA", new RbaExchangeRateProvider(rbaOptions), options, ownsInner: true);
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

`GetRatesAsync` returns every rate whose date falls in the inclusive window. Whether
the cache can serve a range is decided by **coverage** — the date ranges the source
was actually fetched for — not by the span of the stored rows. A range is served from
the cache only when the recorded coverage **contains** the whole requested window;
otherwise the range is refetched and the rows plus the covered window are written back
together (atomically) through `StoreFetchedRange`.

When the direct pair's coverage does not contain the window but the **inverse** pair's
does — and inversion is permitted by the provider's `DefaultLookupOptions` (the default) —
the range is served from the inverse pair by reciprocating each rate. So a `USD/AUD` range
already fetched also satisfies an `AUD/USD` range request without a refetch, mirroring the
single-date surface.

```csharp
IReadOnlyList<ExchangeRate> january =
    await cachedRba.GetRatesAsync("AUD", "USD", new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 31));
```

> [!NOTE]
> Coverage is recorded for the whole fetched window even on days that returned no
> observation (a weekend, a holiday, a true gap), so a later lookup of the same window
> is served from the cache rather than refetched. A sparse set of rows is therefore
> never mistaken for proof that every interior day was fetched — the distinction a
> [`DateRangeCoverage`](xref:Bodu.Financial.DateRangeCoverage) makes explicit.

## The cache cascade

The cache is deliberately layered so you can plug in at whichever level fits:

| Layer | Type | Responsibility |
|---|---|---|
| Contract | [`IExchangeRateCache`](xref:Bodu.Financial.ExchangeRates.Caching.IExchangeRateCache) | Single-provider store: a bound `Provider`; rate rows via `GetRates`/`Store`, fetch coverage via `GetCoverage`/`RecordCoverage`, and the atomic `StoreFetchedRange` that writes both together. |
| Core | [`ExchangeRateCacheBase<TOptions>`](xref:Bodu.Financial.ExchangeRates.Caching.ExchangeRateCacheBase`1) | Per-pair locking + row/coverage freshness filtering, merge, and prune. **No physical layout.** |
| File seam | [`IFileExchangeRateCache`](xref:Bodu.Financial.ExchangeRates.Caching.IFileExchangeRateCache) / [`FileExchangeRateCacheBase<TOptions>`](xref:Bodu.Financial.ExchangeRates.Caching.FileExchangeRateCacheBase`1) | Layout-driven directory + file-name resolution, date partitioning, best-effort IO. |
| Leaf | [`TomlFileExchangeRateCache`](xref:Bodu.Financial.ExchangeRates.Caching.TomlFileExchangeRateCache) / [`JsonFileExchangeRateCache`](xref:Bodu.Financial.ExchangeRates.Caching.JsonFileExchangeRateCache) | The TOML or JSON serialization format only. |

### The on-disk format

A cache bound to provider `RBA` stores `AUD/USD` as `<directory>/RBA/AUDUSD.toml`
— a per-provider subdirectory with one file per pair (this default layout, and how
to change it, is covered under [File layouts and date partitioning](#file-layouts-and-date-partitioning)).
Each file opens with a **self-describing header** — the bound `Provider` and the
pair's `From`/`To` currency codes — so a file carries its own identity rather than
relying on its name and folder. Each dated rate is then a TOML table; the `decimal`
rate is written as a **quoted string** so its full precision and scale round-trip
exactly, and the dates use TOML's native RFC 3339 forms:

```toml
Provider = "RBA"
From = "AUD"
To = "USD"

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
`TomlDecimalHandling.String`. A file written before the header existed simply has
no `Provider`/`From`/`To` keys and still reads. The file is **best-effort**: any
I/O or TOML error on read yields an empty result, and a failed write is swallowed,
so a cache problem never breaks rate retrieval. You can use a cache directly — note
there is no provider argument; the cache is bound to its provider at construction:

<!-- compile -->
```csharp
var cache = new TomlFileExchangeRateCache(
    new FileExchangeRateCacheOptions { Provider = "RBA", CacheDirectory = "/var/cache/fx" });

var now = DateTimeOffset.UtcNow;
cache.Store(new ExchangeRatePair(CurrencyCode.AUD, CurrencyCode.USD),
    new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now) },
    TimeSpan.FromHours(24), now);

IReadOnlyList<CachedExchangeRate> fresh =
    cache.GetRates(new ExchangeRatePair(CurrencyCode.AUD, CurrencyCode.USD), TimeSpan.FromHours(24), now);
```

### The JSON format

[`JsonFileExchangeRateCache`](xref:Bodu.Financial.ExchangeRates.Caching.JsonFileExchangeRateCache)
is the same cache with a JSON body instead of TOML — same `.json` files, same
self-describing header, same layouts and partitioning, same best-effort IO. It is
a drop-in swap when you want a format other tools read natively; decimals are
written as JSON numbers, which `System.Text.Json` round-trips losslessly to
`decimal`:

```json
{
  "Provider": "RBA",
  "From": "AUD",
  "To": "USD",
  "Entries": [
    { "Date": "2023-01-03", "Rate": 0.5000, "CachedAtUtc": "2023-01-04T09:15:00+00:00" }
  ],
  "Coverage": []
}
```

<!-- compile -->
```csharp
var cache = new JsonFileExchangeRateCache(
    new FileExchangeRateCacheOptions { Provider = "RBA", CacheDirectory = "/var/cache/fx" });
```

### File layouts and date partitioning

The [`Layout`](xref:Bodu.Financial.ExchangeRates.Caching.FileExchangeRateCacheOptions.Layout)
option decides **where** a pair's rows are stored: the folder hierarchy, the file
name, and whether the rows are **split across files by date**. It defaults to
[`ExchangeRateCacheFileLayout.SingleFile`](xref:Bodu.Financial.ExchangeRates.Caching.ExchangeRateCacheFileLayout)
— the `<directory>/<provider>/<from><to>.toml` layout shown above. The built-in
partitioned layouts isolate each pair in its own folder and write one file per
calendar period, keyed by the period:

| Layout | Files for `AUD/USD` under provider `RBA` |
|---|---|
| `SingleFile` (default) | `RBA/AUDUSD.toml` |
| `Yearly` | `RBA/AUDUSD/2023.toml`, `RBA/AUDUSD/2024.toml`, … |
| `Monthly` | `RBA/AUDUSD/2023-01.toml`, `RBA/AUDUSD/2023-02.toml`, … |
| `Daily` | `RBA/AUDUSD/2023-01-03.toml`, … |

Each rate is routed to the file for its observation date, and a recorded coverage
window that crosses a period boundary is split at the boundary so each file carries
only its own period; a read concatenates every file in the pair's folder and the
shared cache rules re-merge the halves, so the split is lossless.

<!-- compile -->
```csharp
// One file per month for each pair.
var monthly = new TomlFileExchangeRateCache(new FileExchangeRateCacheOptions
{
    Provider = "RBA",
    CacheDirectory = "/var/cache/fx",
    Layout = ExchangeRateCacheFileLayout.Monthly,
});
```

For a layout the built-ins do not cover, build one with
[`ExchangeRateCacheFileLayout.Create`](xref:Bodu.Financial.ExchangeRates.Caching.ExchangeRateCacheFileLayout.Create):
supply a partition strategy (one of `Single`/`Yearly`/`Monthly`/`Daily`, or
[`ExchangeRateCachePartitionStrategy.Custom`](xref:Bodu.Financial.ExchangeRates.Caching.ExchangeRateCachePartitionStrategy.Custom)
for an arbitrary period such as fiscal quarters) and optional delegates that decide
the directory and the file name:

<!-- compile -->
```csharp
var custom = new TomlFileExchangeRateCache(new FileExchangeRateCacheOptions
{
    Provider = "RBA",
    CacheDirectory = "/var/cache/fx",
    Layout = ExchangeRateCacheFileLayout.Create(
        ExchangeRateCachePartitionStrategy.Yearly,
        directory: ctx => System.IO.Path.Combine(ctx.Root, "fx", ctx.Provider, $"{ctx.Pair.From}{ctx.Pair.To}"),
        fileName: ctx => $"{ctx.PartitionKey}{ctx.FileExtension}"),
});
```

A partitioned layout has no single backing file, so `ResolveFilePath` throws for it;
use `ResolveDirectory(pair)` for the pair's folder or `ResolvePartitionPath(pair, date)`
for the file a given date lands in. The convenience
`CachingExchangeRateProvider("RBA", inner, options)` constructor uses the default
single-file TOML layout; to use a custom layout or the JSON format, construct the
file cache yourself and pass it to the
`CachingExchangeRateProvider(inner, cache, options)` constructor.

### Custom cache stores

A cache backend is any [`IExchangeRateCache`](xref:Bodu.Financial.ExchangeRates.Caching.IExchangeRateCache)
implementation. To back the cache with a store of your own, implement that interface
directly — the shipped
[`SqliteExchangeRateCache`](xref:Bodu.Financial.ExchangeRates.Caching.SqliteExchangeRateCache)
and [`DistributedExchangeRateCache`](xref:Bodu.Financial.ExchangeRates.Caching.DistributedExchangeRateCache)
are exactly that and serve as worked references. Delegate the freshness, validity,
merge, and coverage rules to the shared, public
[`ExchangeRateCacheRules`](xref:Bodu.Financial.ExchangeRates.Caching.ExchangeRateCacheRules)
so your backend stays behaviourally identical to the in-box caches (the same
`ExchangeRateCacheContractTests` apply), and make `StoreFetchedRange` write the merged
rows and the covered window as one atomic unit so a reader never observes coverage
without its rows.

The in-box [`ExchangeRateCacheBase<TOptions>`](xref:Bodu.Financial.ExchangeRates.Caching.ExchangeRateCacheBase`1)
and [`FileExchangeRateCacheBase<TOptions>`](xref:Bodu.Financial.ExchangeRates.Caching.FileExchangeRateCacheBase`1)
are internal scaffolding for the in-memory, TOML, and JSON caches — they own the per-pair
locking and the read-modify-write sequencing over a `CachePairState` — and their
storage seam is not a public subclassing point. Implement `IExchangeRateCache`
directly, as the SQLite and distributed backends do.

### Persistent and shared backends

Two further `IExchangeRateCache` backends ship as separate packages and drop in the
same way — construct one and hand it to a `CachingExchangeRateProvider`, or register
it through the DI extension method that ships inside the backend's own package (in the
`Bodu.Financial.ExchangeRates` namespace):

- [`SqliteExchangeRateCache`](xref:Bodu.Financial.ExchangeRates.Caching.SqliteExchangeRateCache)
  (`Bodu.Financial.ExchangeRates.Caching.Sqlite`) persists rates and coverage in a
  SQLite database — durable across restarts, with per-pair transactional writes.
  Register it with `AddSqliteRateCache("RBA", …)`.
- [`DistributedExchangeRateCache`](xref:Bodu.Financial.ExchangeRates.Caching.DistributedExchangeRateCache)
  (`Bodu.Financial.ExchangeRates.Caching.Distributed`) stores each pair as a JSON blob
  in any `IDistributedCache` (Redis, SQL Server, in-memory), so several processes share
  one warm cache. Register it with `AddDistributedRateCache("RBA")` or
  `AddRedisRateCache(redis => …, "RBA")` — the Redis configurator is the first
  argument, the provider name the second.

Every backend shares the same freshness, merge, and coverage semantics — the same
`ExchangeRateCacheContractTests`.

> [!IMPORTANT]
> The distributed cache is a **best-effort shared performance hint, not an
> authoritative multi-writer store.** `IDistributedCache` offers no atomic
> read-modify-write, so same-process races are guarded by a per-pair in-process lock
> while **cross-process** writes to one pair are last-write-wins (a `StoreFetchedRange`
> blob is still all-or-nothing per write, so a reader never sees coverage without its
> rows). When correctness under concurrent writers matters, prefer the SQLite backend
> (one transaction per write) or a real database.

The choice is one of reach and durability:

| Backend | Best for | Not for | Correctness note |
|---|---|---|---|
| [`NullExchangeRateCache`](xref:Bodu.Financial.ExchangeRates.Caching.NullExchangeRateCache) | tests / disabling the cache | any reuse | stores nothing; every lookup is a miss |
| [`InMemoryExchangeRateCache`](xref:Bodu.Financial.ExchangeRates.Caching.InMemoryExchangeRateCache) | a single, long-lived process | restarts; multiple processes | process-local; lost on restart |
| [`TomlFileExchangeRateCache`](xref:Bodu.Financial.ExchangeRates.Caching.TomlFileExchangeRateCache) / [`JsonFileExchangeRateCache`](xref:Bodu.Financial.ExchangeRates.Caching.JsonFileExchangeRateCache) | simple durable local cache; inspectable files | high multi-process write concurrency | atomic temp-and-move per file; best-effort |
| [`SqliteExchangeRateCache`](xref:Bodu.Financial.ExchangeRates.Caching.SqliteExchangeRateCache) | durable single-host cache | a cache shared across hosts | strongest shipped local option; one transaction per write |
| [`DistributedExchangeRateCache`](xref:Bodu.Financial.ExchangeRates.Caching.DistributedExchangeRateCache) | a warm cache shared across processes/hosts | an authoritative multi-writer store | last-write-wins per pair across processes |

## Cache backends in depth

The earlier table picks a backend by reach and durability; this section goes
one level down into the *semantics* each one commits to, so a choice survives
the move from a single process to a fleet. Every backend implements the same
[`IExchangeRateCache`](xref:Bodu.Financial.ExchangeRates.Caching.IExchangeRateCache)
contract and delegates its freshness, validity, merge, and coverage rules to
the shared
[`ExchangeRateCacheRules`](xref:Bodu.Financial.ExchangeRates.Caching.ExchangeRateCacheRules),
so they differ only in *where the bytes live* and *how a concurrent write is
ordered* — never in what counts as a hit.

| | `Null` | `InMemory` | `TomlFile` / `JsonFile` | `Sqlite` | `Distributed` |
|---|---|---|---|---|---|
| Package | core | core | core | `.Caching.Sqlite` | `.Caching.Distributed` |
| Scope | none | one process | one host | one host | many hosts |
| Survives restart | n/a | no | yes | yes | yes (in the backing store) |
| Shared across processes | n/a | no | through the file system | through the file | yes |
| `StoreFetchedRange` atomicity | n/a | per-pair lock | temp-and-move per file | one transaction | one blob write |
| Cross-process write race | n/a | n/a | last-write-wins per file | serialized rows, OS-level | last-write-wins per pair |
| Construct | `Create(provider)` | `new(provider)` | `new(options)` | `AddSqliteRateCache` | `AddDistributedRateCache` / `AddRedisRateCache` |

**Expiry, invalidation, and refresh are uniform.** None of the backends has a
private eviction clock. A row is fresh while `asOf - CachedAtUtc < duration`
(the resolved per-provider expiry), and freshness is evaluated on every read;
stale rows are pruned on the next write. There is no explicit invalidate call —
a value "refreshes" by being re-fetched on a miss and merged in, latest
`CachedAtUtc` winning per date. This is why the same rules object is shared: an
expired row in SQLite and an expired row in Redis disappear at the same instant
relative to their own `CachedAtUtc`, with no backend-specific TTL drift. A
distributed backend may *additionally* set an absolute expiration on its blob as
a storage hint, but correctness never depends on it — the freshness filter still
runs on read.

**Atomicity is the axis that actually matters across processes.**
`StoreFetchedRange` must persist the merged rows and the covered window together
or neither, so a reader never sees coverage without its rows and reports a false
range hit. Each backend honours that differently:

- [`InMemoryExchangeRateCache`](xref:Bodu.Financial.ExchangeRates.Caching.InMemoryExchangeRateCache)
  and the file caches ([`TomlFileExchangeRateCache`](xref:Bodu.Financial.ExchangeRates.Caching.TomlFileExchangeRateCache)
  and [`JsonFileExchangeRateCache`](xref:Bodu.Financial.ExchangeRates.Caching.JsonFileExchangeRateCache))
  serialize per-pair writes under an in-process lock; the file caches additionally
  write each file through a temp-and-move so a half-written file is never observed.
  Two *processes* writing the same file (for example `AUDUSD.toml`) are
  last-write-wins; under a partitioned layout each per-period file is written that
  same atomic way.
- [`SqliteExchangeRateCache`](xref:Bodu.Financial.ExchangeRates.Caching.SqliteExchangeRateCache)
  wraps each `StoreFetchedRange` in a single transaction over its `rates` and
  `coverage` tables, so the all-or-nothing guarantee holds even when several
  processes on the host share the database file.
- [`DistributedExchangeRateCache`](xref:Bodu.Financial.ExchangeRates.Caching.DistributedExchangeRateCache)
  stores the whole per-pair state as one JSON blob, so a `StoreFetchedRange` is
  all-or-nothing *across processes* — but because `IDistributedCache` has no
  atomic read-modify-write, the read-merge-write cycle of `Store` /
  `RecordCoverage` is only same-process safe; cross-process writes to a pair are
  last-write-wins.

**When to use which.** Reach for `NullExchangeRateCache` to disable caching in a
test without changing the composition. Use `InMemoryExchangeRateCache` for a
single long-lived service that can afford a cold start after a restart. Pick
`TomlFileExchangeRateCache` for a durable, inspectable local cache where write
concurrency is low — the per-pair files are human-readable. Prefer
`SqliteExchangeRateCache` when several processes on one host must share a warm,
correct-under-concurrency cache. Choose `DistributedExchangeRateCache` only when
the cache must span hosts and you accept its best-effort, last-write-wins
nature as a performance hint rather than an authoritative store; when correctness
under concurrent writers matters across a fleet, front a real database with your
own [`IExchangeRateCache`](xref:Bodu.Financial.ExchangeRates.Caching.IExchangeRateCache).

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
  synthetic provider label (`Average` by default). The mean is an analytical,
  composite value — it can equal a rate no source actually published — so it suits
  smoothing or cross-source comparison rather than an authoritative observation; for
  tax, accounting, or audit use prefer a single source (priority or per-pair routing).
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
aggregation.Routes[new ExchangeRatePair(CurrencyCode.AUD, CurrencyCode.USD)] = new ExchangeRatePairRoute(new[] { "RBA", "ECB" });
aggregation.Routes[new ExchangeRatePair(CurrencyCode.USD, CurrencyCode.GBP)] = new ExchangeRatePairRoute(new[] { "ECB", "RBA" });
aggregation.Routes[new ExchangeRatePair(CurrencyCode.EUR, CurrencyCode.USD)] = new ExchangeRatePairRoute(new[] { "ECB", "RBA" }, new AverageStrategy());

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

The `Bodu.Financial.ExchangeRates.Caching` package ships its own DI registration
(there is no separate `*.DependencyInjection` package); its extension methods register
either shape on the `IFinancialServiceBuilder` and live in the
`Bodu.Financial.ExchangeRates` namespace, so one `using` brings them into
scope. Both resolve as the dated **and** timeless surfaces.

A single cached provider:

```csharp
using Bodu.Financial;
using Bodu.Financial.ExchangeRates;
using Microsoft.Extensions.DependencyInjection;

services.AddFinancialService()
        .AddRbaHistoricalRates()
        .AddCachedExchangeRateProvider<RbaExchangeRateProvider>("RBA",
            configure: o => o.DefaultExpiry = TimeSpan.FromHours(12));
```

A group of cached providers with per-pair routing. Each child is **also**
registered as a keyed `IDatedExchangeRateProvider`, so a specific source is
resolvable by name:

```csharp
services.AddFinancialService()
        .AddRbaHistoricalRates()
        .AddEcbReferenceRates()
        .AddAggregatedExchangeRateProvider(agg => agg
            .AddCachedChild<RbaExchangeRateProvider>("RBA")
            .AddCachedChild<EcbExchangeRateProvider>("ECB")
            .MapPair(new ExchangeRatePair(CurrencyCode.AUD, CurrencyCode.USD), "RBA", "ECB")
            .MapPair(new ExchangeRatePair(CurrencyCode.USD, CurrencyCode.GBP), "ECB", "RBA"));

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
- Range serving is decided by recorded **coverage**, not by the rows: a range is
  served only when the still-fresh coverage windows contain the whole request, and a
  range fetch writes its rows and covered window together through `StoreFetchedRange`.
  Coverage windows expire on the same `duration` and are pruned on write.

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
- [`JsonFileExchangeRateCache` API reference](xref:Bodu.Financial.ExchangeRates.Caching.JsonFileExchangeRateCache)
- [`ExchangeRateCacheFileLayout` API reference](xref:Bodu.Financial.ExchangeRates.Caching.ExchangeRateCacheFileLayout)
