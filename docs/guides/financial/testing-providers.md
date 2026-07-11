---
title: Testing your own provider
---

# Testing your own provider

`Bodu.Financial.ExchangeRates.Testing` ships the MSTest contract-test bases the built-in
providers themselves pass. When you write a custom rate source — or need a deterministic rate
provider inside your own test suite — this package and one fixed-provider type cover both sides
of the problem.

## FixedDatedRateProvider is the recommended test double

Code that consumes <xref:Bodu.Financial.ExchangeRates.IDatedRateProvider> should be tested
against <xref:Bodu.Financial.ExchangeRates.FixedDatedRateProvider>, not a mock: it is the real
lookup engine over an in-memory <xref:Bodu.Financial.ExchangeRates.RateBook>, so date
resolution, inverse fallback, identity rates, and provenance behave exactly as they will in
production.

```csharp
using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates;

var rates = new FixedDatedRateProvider(new[]
{
    new ExchangeRate(CurrencyCode.AUD, CurrencyCode.USD, new DateOnly(2024, 3, 15), 0.6580m, "Test"),
});

// Inject `rates` wherever the system under test wants an IDatedRateProvider.
```

For larger fixtures, build the book with
<xref:Bodu.Financial.ExchangeRates.RateTableBuilder> /
<xref:Bodu.Financial.ExchangeRates.RateSeriesBuilder> — see the
[dependency-injection guide](dependency-injection.md) for wiring test doubles through
`AddDatedExchangeRateProvider`.

## Deriving the dated-provider contract

Any custom `IDatedRateProvider` should pass the shared contract. Derive
`DatedRateProviderContractTests<TProvider>`, supply a seeded provider and the dates that
characterise it, and the base exercises the whole surface: sync/async equivalence for single
dates and ranges, misses (`TryGetRate` false, `GetRate` throwing `KeyNotFoundException`),
provenance consistency, null-options defaulting, inverse reciprocal lookups, same-currency
identity rates, and (opt-in) disposal guards.

```csharp
using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates;
using Bodu.Financial.ExchangeRates.Testing;

[TestClass]
public sealed class CsvFileRateProviderTests
    : DatedRateProviderContractTests<CsvFileRateProvider>
{
    protected override CurrencyPair CanonicalPair => new(CurrencyCode.AUD, CurrencyCode.USD);

    protected override DateOnly KnownDate => new(2024, 1, 15);

    protected override DateOnly UnknownDate => new(2024, 6, 17);

    protected override CsvFileRateProvider CreateProvider() =>
        new("Data/custom-feed.csv", "CustomFeed");
}
```

Optional seams tune the contract to the provider's semantics: `RangeStart` / `RangeEnd` widen
the range assertions beyond `KnownDate`, `SupportsInverseLookup` and `SupportsIdentityRate`
switch those fallbacks off for providers that reject them, and `SupportsDisposalGuard` opts in
the post-dispose `ObjectDisposedException` sweep.

This exact pattern runs in the repository: the
[CustomProvider sample](../../samples/financial.md#bodufinancialsamplescustomprovider--test) is a consumer-shaped
`CsvFileRateProvider` whose companion test project derives the base and passes it in CI.

## Deriving the pair-web-provider contract

Providers built on `PairWebRateProvider<TSeries>` (per-pair sources such as Yahoo, OFX, XE, and
OANDA) additionally derive `PairWebRateProviderContractTests<TProvider, TSeries>`, which layers
the pair-warm-up lifecycle on top: `LoadPairAsync` resolving a known date, idempotent re-warm of
an already-covered window, `GetAvailablePairs` reporting loaded series, and the provider's
declared <xref:Bodu.Financial.ExchangeRates.RateHistoryAvailability> matching the
`ExpectedHistoryAvailability` the test states.

Point the provider's options at a local stub (a canned HTTP handler or file-backed
`IPairRateSource<TSeries>`) so the contract runs hermetically — the shipped provider test
projects follow this shape.
