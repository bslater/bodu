# Bodu.Globalization.Calendar.Samples.Caching

Demonstrates the calendar caching layer: the read-through `CachingNotableDateService` decorator
over the in-memory and durable file backends, explicit cache warm-up of a serving window, and the
dependency-injection registration that decorates an already-registered `INotableDateService`.

```bash
dotnet run --project samples/Globalization.Calendar/Bodu.Globalization.Calendar.Samples.Caching
```

## Scenarios

| Scenario | Shows |
|---|---|
| `ReadThroughCaching` | Whole-(territory, civil-year) cache entries; warm queries, sub-range clipping, and filtered overloads all served without re-resolving |
| `FileBackedCaches` | `JsonNotableDateCache` / `TomlNotableDateCache` persisting years to disk so a fresh service instance starts warm |
| `WarmUp` | `CachingNotableDateService.Warm` pre-resolving territories × years, plus the commented `AddNotableDateCacheWarmup` hosted-service form |
| `DiRegistration` | `AddCachedNotableDateService` decorating the registered service; commented Sqlite / distributed backend registrations |

A `CountingNotableDateService` wrapper counts the resolutions that reach the real engine, so every
cache hit is proved deterministically by call count rather than by timing.

## NuGet equivalents

```bash
dotnet add package Bodu.Globalization.Calendar
dotnet add package Bodu.Globalization.Calendar.Caching
dotnet add package Bodu.Globalization.Calendar.DependencyInjection
dotnet add package Bodu.Globalization.Calendar.AsiaPacific
# optional durable backends:
dotnet add package Bodu.Globalization.Calendar.Caching.Sqlite
dotnet add package Bodu.Globalization.Calendar.Caching.Distributed
```
