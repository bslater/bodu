# Bodu.Globalization.Calendar.Caching.Distributed

> **API stability — Stable.** The public API surface is committed; breaking changes are reserved for a major-version bump per [SemVer](https://semver.org).

A **shared** storage backend for the
[`Bodu.Globalization.Calendar.Caching`](https://www.nuget.org/packages/Bodu.Globalization.Calendar.Caching)
notable-date cache, over any `Microsoft.Extensions.Caching.Distributed.IDistributedCache` — Redis,
SQL Server, or any other implementation. One instance computes a civil year; every instance reads it.

> Backend selection and the full options surface are covered in
> [Backends and options](../docs/guides/calendar/caching/backends-and-options.md); the caching model
> itself is in [Caching notable dates](../docs/guides/calendar/caching/notable-date-caching.md).

## Installation

```shell
dotnet add package Bodu.Globalization.Calendar.Caching.Distributed
```

Targets `net8.0`. All types live in the `Bodu.Globalization.Calendar.Caching` namespace, alongside
the other backends.

```csharp
services.AddDistributedNotableDateCache(/* … */);   // any IDistributedCache
services.AddRedisNotableDateCache(/* … */);         // Redis convenience overload
```

## What it adds

| Type | Role |
|---|---|
| `DistributedNotableDateCache` | `INotableDateCache` over any `IDistributedCache` |
| `DistributedNotableDateCacheOptions` | Key prefix and entry configuration |
| `DistributedNotableDateCacheExtensions` | The `AddDistributedNotableDateCache` / `AddRedisNotableDateCache` registrations |

It is a storage backend only — the caching *policy* (per-year units, time-to-live plus
resource-version invalidation, single-flight cold misses, best-effort storage) lives in
`CachingNotableDateService` and is identical across every backend.

Two consequences worth knowing for a shared cache. Single-flight coalescing is **per process**, so
each instance may compute a cold year once; the resource-version token keeps that correct, since an
entry written under one resource version is never served to a process on another. And the
best-effort contract matters more here than on local storage: a failed read is a miss and a failed
write is skipped, so an unreachable Redis degrades to recomputation rather than an outage.

## Choosing a backend

Use this package when instances should **share** computed years. For durability on a single machine
use [`…Caching.Sqlite`](https://www.nuget.org/packages/Bodu.Globalization.Calendar.Caching.Sqlite);
for a single short-lived process the in-memory backend in the core caching package is enough.

Part of the [Bodu](https://github.com/bslater/bodu) utility library.
