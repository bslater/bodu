# Bodu.Financial.Samples.CustomProvider

Consumer extensibility: write your own `IDatedRateProvider` and prove it with the shipped
contract-test base. `CsvFileRateProvider.cs` is the recommended shape for bringing any custom
rate source into the stack — parse your data into a `RateTableBuilder`, freeze it into a
`RateBook`, and delegate the lookup surface to a `FixedDatedRateProvider`, so date resolution,
inverse fallback, identity rates, and provenance behave exactly like every shipped provider
without writing any lookup logic.

```bash
dotnet run --project samples/Financial/Bodu.Financial.Samples.CustomProvider
dotnet test samples/Financial/Bodu.Financial.Samples.CustomProvider.Test
```

## Scenarios

`Program.cs` exercises the custom provider end to end in four steps (one file — the provider
itself is the point of this sample):

### Direct lookups

**Intent.** Prove the delegated surface gives a hand-rolled provider the full lookup semantics
for free.

**What it does.** Resolves AUD/USD on an exact business day, USD/AUD (a direction the file never
quotes — served by inverse fallback), and a Saturday under `PreviousWithin(3)`; prints the
provider's self-declared history availability.

**What to expect.**

```
AUD/USD exact    : 0.6660 [CustomFeed]
USD/AUD inverse  : 1.5015015015015015015015015015 (derived from the AUD/USD observation)
Saturday lookup  : 0.6663 resolved to 2024-01-12
Declared history : Since, earliest 2024-01-02
```

The inverse line is the reciprocal of the stored AUD/USD observation, unrounded — rounding
belongs to the money boundary, not the rate. The provider label `[CustomFeed]` flows through
provenance from the constructor argument. History availability was derived automatically from
the loaded observations.

**APIs demonstrated.** `RateTableBuilder` → `RateBook` → `FixedDatedRateProvider` delegation,
`RateLookupOptions.PreviousWithin`, inverse fallback, `RateHistoryAvailability`.

### Money conversion through the custom provider

**Intent.** The conversion extensions accept *any* `IDatedRateProvider` — including yours.

**What it does / what to expect.** Converts a 2,499.95 AUD invoice through the custom provider:

```
Convert          : AUD 2,499.95 -> USD 1,664.97
```

**APIs demonstrated.** `MoneyOfTCurrencyExchangeRateExtensions.ConvertTo<AUD, USD>(provider,
date)`.

### Composing under the shipped decorators

**Intent.** A custom source is a first-class citizen: it wraps in `CachingRateProvider` exactly
like a live web provider.

**What it does / what to expect.** Wraps the provider in an in-memory read-through cache and
looks up twice:

```
Cached wrapper   : second lookup served from Cache (InMemoryRateCache)
```

**APIs demonstrated.** `CachingRateProvider` over a consumer-written inner provider,
`InMemoryRateCache`, provenance `Origin`/`Backend`.

## The contract-test project

`../Bodu.Financial.Samples.CustomProvider.Test/CsvFileRateProviderTests.cs` derives
`DatedRateProviderContractTests<CsvFileRateProvider>` from
`Bodu.Financial.ExchangeRates.Testing` — the same base every built-in provider passes. The
subclass supplies only the seed knowledge (`CanonicalPair` AUD/USD, `KnownDate` 2024-01-15,
`UnknownDate` outside the file, the range bounds, and `CreateProvider()`); the base then
exercises the whole `IDatedRateProvider` contract: sync/async equivalence for single dates and
ranges, misses (`TryGetRate` false where `GetRate` throws `KeyNotFoundException`), provenance
consistency, null-options defaulting, inverse reciprocal lookups, and same-currency identity
rates. Expect **8 passing, 1 skipped** (the disposal-guard test — this provider is not
`IDisposable`, so `SupportsDisposalGuard` stays false).

The test project runs in CI automatically alongside the library suites (test discovery is
driven by `bodu.slnx` membership).

## Data

`Data/custom-feed.csv` — 22 business days of illustrative AUD/USD rates for January 2024
(synthetic; see the file header). The test project links the same file, so the provider and its
contract tests share one fixture.

## NuGet equivalent

```bash
dotnet add package Bodu.Financial
dotnet add package Bodu.Financial.ExchangeRates.Caching
# test project only:
dotnet add package Bodu.Financial.ExchangeRates.Testing
```
