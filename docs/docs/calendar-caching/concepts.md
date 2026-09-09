---
title: Bodu.Globalization.Calendar.Caching — Core concepts
---

# Bodu.Globalization.Calendar.Caching — Core concepts

This page is the vocabulary the rest of the caching documentation assumes. Read it once before the
[getting-started samples](getting-started.md) or the [caching guide](../../guides/calendar/caching/notable-date-caching.md),
and refer back whenever a term feels imprecise.

Part of the **[Globalization & Calendars](../topics/globalization-and-calendars.md)** topic.

For the high-level shape of the library, start with the [introduction](index.md).

## Decorator vs service

The **service** is any <xref:Bodu.Globalization.Calendar.INotableDateService> — the engine's
<xref:Bodu.Globalization.Calendar.NotableDateService>, a data pack's `CreateService(...)` result, or the reloadable
<xref:Bodu.Globalization.Calendar.ReloadableNotableDateService>. It is a pure computer: given a resource, a territory,
and a year, it resolves the same occurrences every time and knows nothing about caching.

The **decorator** is <xref:Bodu.Globalization.Calendar.Caching.CachingNotableDateService>. It implements the same
interface, holds the wrapped service as its *inner* service, and answers each query from an
<xref:Bodu.Globalization.Calendar.Caching.INotableDateCache> — calling the inner service only on a miss. Because the
two share a contract, consumers never know which one they hold. Under dependency injection,
`AddCachedNotableDateService` swaps the decorator into the container's `INotableDateService` slot and makes the
previous registration the inner service.

The decorator is not a second engine. Every observable behavior of the wrapped service — inverted-range handling,
filter semantics, discovery methods — is delegated or reproduced exactly; caching only changes *when* a year is
computed.

## Cache key and cache unit

The **cache unit** is one territory's occurrences for one whole civil (Gregorian) year, represented by a
<xref:Bodu.Globalization.Calendar.Caching.NotableDateCacheEntry>. The **cache key** is the triple
`(territory, year, resourceVersion)`:

| Key component | Normalization |
|---|---|
| Territory | Trimmed and upper-cased with the invariant culture, so `us`, `US `, and `US` address one entry. `AU-NSW` and `AU` are distinct — a subdivision query inherits its parent's rules inside the engine, not inside the cache. |
| Year | The civil year, `1`–`9999`. |
| Resource version | An ordinal string token (see [below](#time-to-live-vs-resource-version)). |

A query is always decomposed into whole years. A single day, a sub-year range, and a multi-year range all resolve
through the same per-year entries; the assembled occurrences are then clipped to the requested window by their
emitted `Date`. The unit is deliberately coarse: resolving one year is a pure function of resource, territory, and
year, and a single-day query for a cached year costs a cache read rather than a recompute. A year that yields no
occurrences is still a valid, cacheable entry — it records that the year *was* computed.

A <xref:Bodu.Globalization.Calendar.NotableDateFilter> is never part of the key. The filtered `Resolve` overloads
apply the filter to the assembled unfiltered result, so every filter shares the same cached years.

## Hit, miss, coalesced flight, and refresh-ahead

| Event | What happens |
|---|---|
| **Hit** | `GetYear` returns a fresh, version-matching entry. The occurrences are served as-is (the cached list itself for a whole-year query), logged at `CacheHitLogLevel`, and counted on the `hits` meter. |
| **Miss** | `GetYear` returns `null`. The decorator resolves the *whole* civil year through the inner service, writes it back with `StoreYear`, logs at `CacheMissLogLevel`, and counts on the `misses` meter. |
| **Coalesced flight** | A second caller misses the same `(territory, year, version)` while the first computation is still running. It joins the in-flight computation (a `Lazy<T>` per key) instead of starting its own, so *N* concurrent cold callers cost one engine computation. A faulted computation is not memoized — the next caller retries fresh. |
| **Refresh-ahead** | With `RefreshAheadFraction` above `0`, a hit whose entry is older than that fraction of the effective time-to-live is still served immediately and additionally schedules one background recompute of the year. At most one recompute per key is pending at a time; a failing recompute is logged and swallowed, and the next aged hit retries. Because the fraction is below `1`, a continuously hot territory never surfaces a miss. |

Refresh-ahead is access-triggered — no timers run, and only a year that was actually served can schedule a
recompute. Disposing the decorator prevents new recomputes and abandons pending ones without draining them.

## Time-to-live vs resource version

Freshness has two independent triggers, and the **cache** — not the decorator — evaluates both on every call, using
the `ttl` and `asOf` arguments it is handed.

**Time-to-live** (`NotableDateCachingOptions.Ttl`, default 30 days) expires an entry a fixed duration after its
`ComputedAtUtc`. The comparison is strict: an entry exactly one time-to-live old is stale. Because notable-date
resolution is deterministic for a given resource, this trigger is a safety net against drift rather than the primary
invalidation mechanism; there is no upper bound, and an extreme duration effectively disables it. Two refinements
shape it:

- **Jitter** (`TtlJitter`, default `0`, range `[0, 1)`) deterministically shaves up to that fraction off a territory's
  effective time-to-live, derived from a stable hash of the normalized territory — never from randomness — so
  territories warmed together do not all expire at the same instant, and a given territory behaves identically
  across processes and under test. Jitter only ever shortens the duration.
- **Refresh-ahead** (`RefreshAheadFraction`, default `0`, range `[0, 1)`) is described above.

**Resource version** is the ordinal token every entry is keyed by. An entry is served only when its
`ResourceVersion` equals the requested one, and a `StoreYear` drops every existing entry whose version differs from
the one being written — so a version change invalidates a territory's cache wholesale, regardless of the
time-to-live. The token comes from one of two places:

| Decorator constructed with | Token |
|---|---|
| An <xref:Bodu.Globalization.Calendar.INotableDateResourceProvider> (`versionSource`) | `"<ResourceId>\|<SchemaVersion>\|<generation>"`, where the generation increments every time the provider's `Current` reference changes. A <xref:Bodu.Globalization.Calendar.MutableNotableDateResourceProvider>`.Reload(...)` therefore invalidates every cached year on the next query, even when the new resource carries the same identifier. Under DI this provider is picked up automatically when `AddReloadableNotableDateService` registered it. |
| No provider | `NotableDateCachingOptions.ResourceVersion`, or a built-in default token when that is `null`. Set it to the data version of the resource and bump it after a data update. |

Validity is a third, silent check: an entry with a null territory or version, a year outside `1`–`9999`, or a
`ComputedAtUtc` more than one minute ahead of `asOf` (clock skew tolerance) is treated as absent on read and dropped
on write, so a tampered or malformed store never surfaces a nonsensical result.

## Warm-up

A **warm-up** pre-pays the year computations a cold cache would otherwise charge to the first user requests. It goes
through the normal read-through path — cold years compute and store, already-cached years cost only a cache read — so
it is safe to repeat.

- `CachingNotableDateService.Warm(territories, firstYear, lastYear)` warms an inclusive span of civil years for each
  territory in turn, synchronously. A territory whose resolution fails is logged (`EventId 4607`, `Warning`) and
  skipped; the return value is the number of territories warmed.
- `AddNotableDateCacheWarmup(...)` registers a hosted service that calls `Warm` after the host has started, one
  territory at a time, without ever blocking or crashing the host. The span comes from
  <xref:Bodu.Globalization.Calendar.Caching.NotableDateCacheWarmupOptions>: a rolling window of `YearsBehind`
  (default `0`) and `YearsAhead` (default `1`) around the current UTC year — recomputed on every run, so a long-lived
  deployment stays current across restarts — with either bound pinned by `FirstYear` / `LastYear`. `Territories`
  must be non-empty, or startup validation fails. When the registered `INotableDateService` is not the caching
  decorator, the run logs `EventId 4625` and no-ops; register `AddCachedNotableDateService` first.

## Write status

<xref:Bodu.Globalization.Calendar.Caching.NotableDateCacheWriteStatus> is what `StoreYear` returns, so a caller can
tell whether the computed year actually landed:

| Status | Meaning |
|---|---|
| `Stored` | The entry was persisted and is durable for the backend's lifetime (an in-memory write counts, for the instance's lifetime). |
| `Skipped` | The cache intentionally stores nothing — <xref:Bodu.Globalization.Calendar.Caching.NullNotableDateCache> — so nothing was persisted and nothing was expected to be. |
| `Failed` | A storage error was swallowed under the best-effort policy; nothing was persisted, and the next lookup for that year recomputes rather than trusting a write that never happened. |

The decorator itself ignores the status — its result is already computed — but a custom backend or a diagnostic
wrapper can act on it.

## Storage failure policy

Every shipped backend is **best-effort** by default: a read that fails returns an empty result (a miss), a write that
fails returns `Failed`, and `Clear` swallows faults. The exceptions treated as storage failures are
`IOException` and `UnauthorizedAccessException` for the file caches, `SqliteException` and `IOException` for SQLite,
and any non-cancellation exception for the distributed cache. Row-level corruption — an unreadable occurrence blob, a
malformed TOML or JSON file — is data damage rather than a storage failure: the affected rows are skipped, the file is
reported (`EventId 4604`), and the next successful write repairs it.

Two switches on <xref:Bodu.Globalization.Calendar.Caching.NotableDateCacheOptions> tighten the policy:

| Option | Default | Effect |
|---|---|---|
| `ThrowOnStorageFailure` | `false` | Rethrow the underlying storage exception as the store produced it, instead of degrading. Which of `IOException` / `UnauthorizedAccessException` a fault yields varies by platform, so catch both. |
| `ValidateStorageOnStart` | `false` | Probe the store eagerly. A directly constructed file cache creates its directory; a SQLite cache opens and initializes the database; under DI the SQLite and distributed registrations run the probe through `ValidateOnStart`, so a misconfigured store fails the host start rather than the first lookup. Independent of `ThrowOnStorageFailure`. |

Argument validation always throws regardless of either switch.

## Backend classes

The shipped backends share two layers of mechanism so they differ only in how they read and write bytes:

| Class | Layer | Contributes |
|---|---|---|
| <xref:Bodu.Globalization.Calendar.Caching.NotableDateCacheBase`1> (`TOptions : NotableDateCacheOptions`) | Storage-agnostic | Territory normalization; the read-time freshness, validity, and version filter in `GetYear`; the read-merge-write sequence in `StoreYear` under a striped per-territory lock; the batch read the decorator uses for multi-year ranges. Derived types implement `ReadEntries(territory)`, `WriteEntries(territory, entries)`, and `Clear()`. `GetYear` is virtual so a keyed store can read one row instead of a whole territory. |
| <xref:Bodu.Globalization.Calendar.Caching.FileNotableDateCacheBase> (over <xref:Bodu.Globalization.Calendar.Caching.FileNotableDateCacheOptions>) | File mechanism | One file per territory named after the sanitized territory code under `CacheDirectory` (default: `bodu-notable-dates` under the system temp path); atomic temp-and-move writes; a bounded last-write-time parse memo so an unchanged file is not re-parsed; best-effort degradation. Derived types supply the file extension and the serialization. |
| <xref:Bodu.Globalization.Calendar.Caching.InMemoryNotableDateCache> | Backend | A concurrent dictionary of territory → entry list. |
| <xref:Bodu.Globalization.Calendar.Caching.TomlNotableDateCache>, <xref:Bodu.Globalization.Calendar.Caching.JsonNotableDateCache> | Backend | The TOML (`Bodu.Text.Toml`) and JSON (`System.Text.Json`) serializations of the shared <xref:Bodu.Globalization.Calendar.Caching.NotableDateCacheFile> document. |
| <xref:Bodu.Globalization.Calendar.Caching.SqliteNotableDateCache> | Backend (`…Caching.Sqlite`) | A `notable_dates` table with primary key `(territory, year, version)`, `computed_at` as invariant round-trip text, and `occurrences` as a JSON blob; WAL and `busy_timeout` pragmas per <xref:Bodu.Globalization.Calendar.Caching.SqliteNotableDateCacheOptions>. |
| <xref:Bodu.Globalization.Calendar.Caching.DistributedNotableDateCache> | Backend (`…Caching.Distributed`) | One JSON blob per territory under `<KeyPrefix>notable-dates:<TERRITORY>` in an `IDistributedCache`, stamped with a server-side absolute expiration of `ttl + EntryExpirationMargin`. |
| <xref:Bodu.Globalization.Calendar.Caching.NullNotableDateCache> | Backend | Implements the interface directly; stores nothing. |

The abstract storage seam (`ReadEntries` / `WriteEntries`) is `protected internal`, opened to the companion SQLite
and distributed packages. A backend outside the family implements the public
<xref:Bodu.Globalization.Calendar.Caching.INotableDateCache> contract directly, as
`NullNotableDateCache` does — see [Getting started](getting-started.md#implement-a-custom-cache).

## Observability

**Logs.** The decorator logs each hit and miss at the levels set on
<xref:Bodu.Globalization.Calendar.Caching.NotableDateCachingOptions> — `CacheHitLogLevel` and `CacheMissLogLevel`,
both defaulting to `Information` (set them to `Debug` in production once the cache is trusted). Every message
carries a stable event id:

| EventId | Level | Event |
|---|---|---|
| 4601 / 4602 | `CacheHitLogLevel` / `CacheMissLogLevel` | Year served from the cache / year recomputed on a miss and cached |
| 4603 | `Warning` | File-cache storage failure swallowed (rate-limited, with the count suppressed since the previous warning) |
| 4604 | `Warning` | Corrupt cache file treated as empty |
| 4605 / 4606 | `CacheMissLogLevel` / `Warning` | Refresh-ahead recomputed a year / refresh-ahead failed and was swallowed |
| 4607 | `Warning` | Warm-up of one territory failed and was skipped |
| 4611 | `Warning` | SQLite storage failure swallowed |
| 4621 | `Warning` | Distributed storage failure swallowed |
| 4622 / 4623 | `Information` | Startup warm-up started / completed |
| 4624 / 4625 | `Warning` | Startup warm-up failed and was abandoned / skipped because the registered service is not the caching decorator |

Storage-failure warnings are rate-limited to one per minute per cache instance; the swallowed-failure *counters* are
not, so sustained degradation stays measurable while its logging is throttled.

**Metrics.** Counters are published through `System.Diagnostics.Metrics`; with no listener attached an add is a no-op
branch. Tag values are normalized territory codes and fixed operation literals, so cardinality stays bounded.

| Meter | Instrument | Tags | Counts |
|---|---|---|---|
| `Bodu.Globalization.Calendar.Caching` | `bodu.calendar.notable_date_cache.hits` | `territory` | Years served from the cache |
| | `bodu.calendar.notable_date_cache.misses` | `territory` | Years recomputed on a miss |
| | `bodu.calendar.notable_date_cache.coalesced_flights` | `territory` | Callers that joined an in-flight computation (approximate under race) |
| | `bodu.calendar.notable_date_cache.refresh_ahead` | `territory`, `outcome` (`success` / `failed`) | Background refresh-ahead recomputes |
| | `bodu.calendar.notable_date_cache.storage_failures` | `operation` | Swallowed file-cache storage failures |
| `Bodu.Globalization.Calendar.Caching.Sqlite` | `bodu.calendar.notable_date_cache.sqlite.storage_failures` | `operation` | Swallowed SQLite storage failures |
| `Bodu.Globalization.Calendar.Caching.Distributed` | `bodu.calendar.notable_date_cache.distributed.storage_failures` | `operation` | Swallowed distributed storage failures |

## Thread safety and lifetime

**Decorator.** <xref:Bodu.Globalization.Calendar.Caching.CachingNotableDateService> is safe for concurrent use: the
single-flight map coalesces concurrent misses, the version token is a volatile read with a lock taken only when the
observed resource reference changes, and refresh-ahead registrations are published before their worker starts. It is
`IDisposable`; disposal stops new refresh-ahead work and disposes the cache only when constructed with
`ownsCache: true` (the DI registration passes `true` exactly when it created the default TOML cache itself).

**Backends.** Reads are lock-free; `StoreYear` runs its read-merge-write under a per-territory lock, so concurrent
writes to one territory in one process cannot interleave and lose a year. Across processes the guarantees are the
backend's: the file caches write atomically (a reader never sees a partial file) and re-parse a file another process
changed; SQLite serializes writers through its own locking with the configured `BusyTimeout`; the distributed cache
has no atomic read-modify-write, so cross-process writes to the same territory are last-write-wins — acceptable for a
best-effort cache whose entries are recomputable.

**Lifetime.** Every cache is designed to live as long as the service it backs — a singleton under DI. The SQLite cache
holds a keep-alive connection (so a shared in-memory database survives between operations) and must be disposed;
the container disposes it when it created it. The in-memory cache's contents die with the instance. Time is taken
from an injected `TimeProvider` (`TimeProvider.System` by default), so freshness is testable with a synthetic clock.

## Where to go next

- **[Getting started](getting-started.md)** — install and runnable minimal samples.
- **[Introduction](index.md)** — the package family, headline types, and scenario index.
- **[Caching notable dates guide](../../guides/calendar/caching/notable-date-caching.md)** — worked patterns and troubleshooting.
- **[Bodu.Globalization.Calendar.Caching API reference](xref:Bodu.Globalization.Calendar.Caching)** — full type-by-type docs.
- **[Globalization & Calendars topic](../topics/globalization-and-calendars.md)** — the runtime and its companions; the [topic concepts](../topics/globalization-and-calendars-concepts.md) page collects the shared vocabulary.
