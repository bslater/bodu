# Bodu.Financial.Samples.CachedRates

The provider-agnostic caching layer, demonstrated offline: wrap any `IDatedRateProvider` in a
read-through cache and stack the tiers you need.

```bash
dotnet run --project samples/Financial/Bodu.Financial.Samples.CachedRates
```

## What it demonstrates

- `Scenarios/ReadThroughCache.cs` — `CachingRateProvider` over a `TomlFileRateCache`: first
  lookup hits the source, the second is served from the cache; provenance says which.
- `Scenarios/CoverageRanges.cs` — coverage-based range serving: the cache records fetched *date
  ranges*, serves fully covered windows without touching the source, and caches
  empty-but-fetched windows (weekends) so "no observation" is not refetched.
- `Scenarios/TieredStacking.cs` — a caching provider is itself an `IDatedRateProvider`, so tiers
  compose: in-memory L1 over durable file L2 over the source, surviving a simulated restart.
- `Scenarios/HistoryClamping.cs` — `RateHistoryAvailability` (`Unbounded` / `Since` /
  `RollingDays`) and how the cache/aggregator use it to clamp doomed requests.
- `CountingRateProvider.cs` — a small delegating decorator that records every call reaching the
  source, making the hit/miss behaviour visible in the console output.

The commented block in `Program.cs` shows the switch to a live `RbaRateProvider`; the SQLite and
distributed backends (`AddSqliteRateCache`, `AddDistributedRateCache`/`AddRedisRateCache`) slot
into the same `IRateCache` seam.

## NuGet equivalent

```bash
dotnet add package Bodu.Financial.ExchangeRates.Caching
# optional add-ons:
dotnet add package Bodu.Financial.ExchangeRates.Caching.Sqlite
dotnet add package Bodu.Financial.ExchangeRates.Caching.Distributed
```
