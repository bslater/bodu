# Bodu.Financial.ExchangeRates.Caching

A caching layer for `Bodu.Financial` exchange-rate providers.

The provider classes (Yahoo, RBA, ECB, BoE) are pure fetchers — they know nothing
of caching. This package adds caching as a **decorator** that implements the same
`IDatedExchangeRateProvider` contract the consumer already resolves:

```
Caller
  │  IDatedExchangeRateProvider
  ▼
CachingDatedExchangeRateProvider   ── returns the rate when a fresh one is cached;
  │                                    otherwise delegates the request downstream
  ▼
concrete provider (Yahoo / RBA / ECB / BoE)
```

## What it does

- `CachingDatedExchangeRateProvider` wraps **one or more named** `IDatedExchangeRateProvider`
  sources supplied at construction, plus an `IExchangeRateCache`. On a lookup it consults the
  sources in order; for each it first tries to satisfy the request from that source's fresh
  cached rows (reusing `FixedDatedExchangeRateProvider` for date-resolution, inverse, and
  identity handling), and on a miss delegates to the source and stores the resolved observation.
  The first source to satisfy the lookup wins.
- `CachingExchangeRateOptions` carries the cache **location**, the **default expiry**, and a
  **per-provider expiry** map (`ProviderExpiry`) that overrides the default for named sources.
- `IExchangeRateCache` is the cache contract. The cache owns expiry: callers pass a
  caching duration, and the cache returns only fresh rows and prunes stale ones on write.
- `TomlFileSystemExchangeRateCache` persists rates as TOML, one file per
  `(provider, currency pair)` — for example `Yahoo_AUDUSD.toml`. Decimal rates are
  written as quoted strings for lossless round-trips.
- `NullExchangeRateCache` is a no-op cache for when on-disk caching is disabled.

## Key types

| Type | Role |
|---|---|
| `CachingDatedExchangeRateProvider` | The caching provider over one or more named `IDatedExchangeRateProvider` sources. |
| `CachingExchangeRateOptions` | Cache location, default expiry, and per-provider expiry overrides. |
| `IExchangeRateCache` | Cache contract (`GetRates` returns fresh rows; `Store` merges and prunes). |
| `ExchangeRateCacheBase` | Shared merge / prune / freshness mechanism; derived types persist entries. |
| `TomlFileSystemExchangeRateCache` | TOML-on-disk cache (`FileSystemExchangeRateCacheOptions`). |
| `NullExchangeRateCache` | No-op cache. |
| `CachedExchangeRate` | One cached row: observation date, rate, and the UTC instant it was cached. |

For dependency-injection wiring, see
`Bodu.Financial.ExchangeRates.Caching.DependencyInjection`.
