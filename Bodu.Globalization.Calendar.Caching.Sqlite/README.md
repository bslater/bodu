# Bodu.Globalization.Calendar.Caching.Sqlite

> **API stability - Stable.** The public API surface is committed: breaking changes are reserved for a major-version bump per [SemVer](https://semver.org), except a deliberate break that is recorded in the release notes and in the package's `CompatibilitySuppressions.xml`. See [versioning and support](https://bslater.github.io/bodu/docs/versioning-and-support.html).

A durable **SQLite** storage backend for the
[`Bodu.Globalization.Calendar.Caching`](https://www.nuget.org/packages/Bodu.Globalization.Calendar.Caching)
notable-date cache. `SqliteNotableDateCache` persists computed civil years in a SQLite database, so
a restarted process starts warm instead of recomputing every territory from scratch.

> Backend selection and the full options surface are covered in
> [Backends and options](../docs/guides/calendar/caching/backends-and-options.md); the caching model
> itself is in [Caching notable dates](../docs/guides/calendar/caching/notable-date-caching.md).

## Installation

```shell
dotnet add package Bodu.Globalization.Calendar.Caching.Sqlite
```

Targets `net8.0` and `net10.0`. The cache types live in the `Bodu.Globalization.Calendar.Caching` namespace,
alongside the other backends; the `AddSqliteNotableDateCache` registration lives in `Bodu.Globalization.Calendar`.

```csharp
services.AddSqliteNotableDateCache(/* … */);
```

## What it adds

| Type | Role |
|---|---|
| `SqliteNotableDateCache` | `INotableDateCache` over a SQLite database file |
| `SqliteNotableDateCacheOptions` | Connection and store configuration |
| `SqliteNotableDateCacheExtensions` | The `AddSqliteNotableDateCache` registration |

It is a storage backend only - the caching *policy* (per-year units, time-to-live plus
resource-version invalidation, single-flight cold misses, best-effort storage) lives in
`CachingNotableDateService` and is identical across every backend. Like the other shipped backends
it degrades gracefully: a failed read is treated as a miss and a failed write is skipped, so a
locked or unavailable database never breaks date resolution.

## Choosing a backend

Use this package when the cache must **survive process restarts on one machine**. For a single
short-lived process the in-memory backend is enough; for a cache **shared between instances**, use
[`…Caching.Distributed`](https://www.nuget.org/packages/Bodu.Globalization.Calendar.Caching.Distributed)
over any `IDistributedCache` (including Redis).

Part of the [Bodu](https://github.com/bslater/bodu) utility library.
