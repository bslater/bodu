# Bodu.Financial.Samples.CachedRates

The provider-agnostic caching layer, demonstrated offline: wrap any `IDatedRateProvider` in a
read-through cache and stack the tiers you need. A small `CountingRateProvider` decorator
records every call that reaches the "source", so the console output *proves* which lookups were
served from cache — with a live web provider, each recorded call would be an HTTP fetch.

```bash
dotnet run --project samples/Financial/Bodu.Financial.Samples.CachedRates
```

Each run starts from an empty cache directory (`bin/.../rate-cache`), so the hit/miss story is
reproducible. `Program.cs` carries the commented switch to caching a live `RbaRateProvider`.

## Scenarios

### ReadThroughCache (`Scenarios/ReadThroughCache.cs`)

**Intent.** The core decorator contract: `CachingRateProvider` serves lookups from an
`IRateCache` and consults the inner source only on a miss — and every result *says* which side
served it, so caching never silently blurs provenance.

**What it does.** Wraps the counting source in a `CachingRateProvider` over a `TomlFileRateCache`
bound to the provider name, then performs the same AUD/USD lookup twice and prints each result's
provenance alongside the cumulative source-call count.

**What to expect.**

```
1st lookup: 0.6568 served from source (live); source calls so far: 1
2nd lookup: 0.6568 served from cache (TomlFileRateCache, age 0s); source calls so far: 1
```

The count staying at 1 is the proof: the second lookup never touched the source. The provenance
flips from live to cache and names the serving backend and the cached data's age — the audit
trail survives the decorator.

**APIs demonstrated.** `CachingRateProvider(inner, IRateCache, CachingRateOptions)`,
`TomlFileRateCache` / `FileRateCacheOptions`, `RateLookupResult.Provenance` (`Origin`,
`Backend`, `Age`), `RateOrigin.Live` / `Cache`.

### CoverageRanges (`Scenarios/CoverageRanges.cs`)

**Intent.** Range caching is subtler than row caching: the cache records which *date ranges* it
has fetched, not just which rows it holds. A range is served from cache only when coverage
contains the whole window; "there was nothing on these days" (weekends, holidays) is itself
cached knowledge. This scenario makes those semantics visible.

**What it does.** Requests AUD/EUR for March (cold), March again (covered), a wider March–April
window (partially covered), a weekend *inside* the covered window, and an uncovered June weekend
twice — printing observation counts, the source-call counter, and finally the exact calls that
reached the source.

**What to expect.**

```
March          : 21 observations; source calls: 1
March again    : 21 observations; source calls: 1
March..April   : 43 observations; source calls: 2
Weekend (cov.) : 0 observations; source calls: 2
June wknd x2   : 0 observations; source calls: 3
Calls that reached the source:
  GetRates AUD/EUR 2024-03-01..2024-03-31
  GetRates AUD/EUR 2024-03-01..2024-04-30
  GetRates AUD/EUR 2024-06-08..2024-06-09
```

Line 2: full coverage → served from cache. Line 3: partial coverage → the *full* window is
refetched (deliberately — a sparse row set cannot be told apart from never-fetched days, so
coverage is all-or-nothing per request window). Line 4: a weekend inside covered range answers
"0 observations" without touching the source. Line 5: the uncovered June weekend fetches once
(call 3), and the repeat is served from the recorded empty coverage — negative caching.

**APIs demonstrated.** `CachingRateProvider.GetRates`, coverage-based range semantics of
`IRateCache` (`StoreFetchedRange` / `DateRangeCoverage` behaviour observed from outside),
`RateRangeResult.Count`.

### TieredStacking (`Scenarios/TieredStacking.cs`)

**Intent.** A `CachingRateProvider` is itself an `IDatedRateProvider`, so caching layers compose
like any decorator: a fast in-memory L1 over a durable file L2 over the source. The payoff shows
up on restart — the durable tier keeps shielding the source when process memory is gone.

**What it does.** Builds L2 (`TomlFileRateCache`) over the counting source, stacks L1
(`InMemoryRateCache`) on top, performs a cold then warm lookup, then *disposes L1 and builds a
fresh one* (simulating a process restart) and looks up again.

**What to expect.**

```
Cold lookup     : source calls 1 (filled L1 and L2)
Warm lookup     : source calls 1 (served by L1)
After 'restart' : source calls 1 (served by L2, origin Cache)
```

The source-call counter never passes 1: the cold call fell through L1 → L2 → source and filled
both tiers on the way back; the warm call stopped at L1; and after the simulated restart the
fresh L1 missed but L2's file served the rate. Swap L2 for the SQLite backend
(`Bodu.Financial.ExchangeRates.Caching.Sqlite`) or a distributed cache
(`...Caching.Distributed`) for the production shapes — the seam is the same `IRateCache`.

**APIs demonstrated.** Stacking `CachingRateProvider` instances, `InMemoryRateCache`,
`TomlFileRateCache` durability, `ownsInner` semantics via `using` scopes.

### HistoryClamping (`Scenarios/HistoryClamping.cs`)

**Intent.** Providers differ in how far back their history reaches — archives are unbounded,
central banks publish since an inception date, some APIs keep only a rolling window.
`RateHistoryAvailability` is the provider's declaration of that reach, and the caching and
aggregation layers consume it (their `RespectHistoryAvailability` options default to true) to
clamp or skip fetches that cannot possibly succeed.

**What it does.** Evaluates the three declaration kinds — `Unbounded`,
`Since(1999-01-04)` (the ECB shape), `RollingDays(180)` (the OANDA shape) — against a fixed
"as of" date and two probe dates, then prints what the offline sample source itself declares
(derived automatically from its loaded observations).

**What to expect.**

```
kind                   earliest as of 2024-07-01  2024-06-03  1990-01-15
Unbounded              (no limit)                 True       True
Since(1999-01-04)      1999-01-04                 True       False
RollingDays(180)       2024-01-03                 True       False
Offline source declares: Since, earliest 2024-01-02
```

`RollingDays(180)`'s earliest date is computed from the as-of instant — the window slides. Both
bounded kinds reject the 1990 probe; a caching layer wrapping such a provider would skip that
fetch instead of issuing a doomed request. The last line shows a `FixedDatedRateProvider`
deriving its own `Since(first observation)` declaration.

**APIs demonstrated.** `RateHistoryAvailability.Unbounded` / `Since` / `RollingDays`,
`GetEarliestAvailable(asOf)`, `IsAvailable(date, asOf)`,
`FixedDatedRateProvider.HistoryAvailability`.

## Data

`Data/aud-daily-2024H1.csv` — the same illustrative AUD business-day rates as the OfflineRates
sample (synthetic values; see the file header). The cache files are derived at runtime and never
committed.

## NuGet equivalent

```bash
dotnet add package Bodu.Financial.ExchangeRates.Caching
# optional add-ons:
dotnet add package Bodu.Financial.ExchangeRates.Caching.Sqlite
dotnet add package Bodu.Financial.ExchangeRates.Caching.Distributed
```
