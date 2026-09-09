---
uid: Bodu.Financial.ExchangeRates.Testing
---

# Bodu.Financial.ExchangeRates.Testing

## Purpose

**Bodu.Financial.ExchangeRates.Testing** holds the MSTest contract-test bases that every shipped exchange-rate provider and cache backend passes, plus a recording HTTP stub for driving a provider offline. It is an **in-repository test-infrastructure project, not a NuGet package**: its project file sets `IsPackable=false` and `IsTestProject=true`, it pre-imports `Microsoft.VisualStudio.TestTools.UnitTesting`, and the provider, cache, and sample test projects consume it by `ProjectReference` (`..\..\Bodu.Financial.ExchangeRates.Testing\src\Bodu.Financial.ExchangeRates.Testing.csproj`). It references `Bodu.Financial` and `Bodu.Financial.ExchangeRates` and nothing else.

A consumer writing a custom <xref:Bodu.Financial.ExchangeRates.IDatedRateProvider> outside the repository does not get these bases from NuGet; the [testing guide](~/guides/financial/testing-providers.md) explains the recommended alternative — testing consumers against <xref:Bodu.Financial.ExchangeRates.FixedDatedRateProvider> — and the shape the bases assert, which is easy to mirror in a local test project.

## Static documentation

- **[Testing your own provider](~/guides/financial/testing-providers.md)** — `FixedDatedRateProvider` as the recommended test double, deriving the dated-provider and pair-web-provider contracts, and running a provider hermetically over a stub.
- **[Bodu.Financial.ExchangeRates introduction](~/docs/exchange-rates/index.md)** — the provider bases these contracts exercise.
- **[Runnable samples](~/samples/financial.md#bodufinancialsamplescustomprovider--test)** — the `CustomProvider` sample and its companion test project, which derives the dated-provider contract in CI.

## Key types

**Contract-test bases**

- <xref:Bodu.Financial.ExchangeRates.Testing.DatedRateProviderContractTests`1> — the contract every `IDatedRateProvider` must satisfy, asserted against whatever data the subclass seeds. A `sealed` `[TestClass]` derives it and supplies:
  - `CanonicalPair` (abstract <xref:Bodu.Financial.ExchangeRates.CurrencyPair>), `KnownDate` (resolves under `RateLookupOptions.Exact`), `UnknownDate` (resolves in neither direction), and `CreateProvider()` (a cold, independent instance per call).
  - Optional seams: `RangeStart` / `RangeEnd` (default `KnownDate`) widen the range assertions; `SupportsInverseLookup` and `SupportsIdentityRate` (default `true`) switch off the reciprocal and same-currency assertions; `SupportsDisposalGuard` (default `false`) opts in the post-dispose `ObjectDisposedException` sweep.
  - The inherited `[TestMethod]`s: sync/async equivalence for a single date and for a range, `TryGetRate` returning `false` and `GetRate` throwing `KeyNotFoundException` on `UnknownDate`, self-consistent <xref:Bodu.Financial.ExchangeRates.RateProvenance>, null-options defaulting, the inverse reciprocal, the same-currency identity rate, and (opt-in) every member throwing after disposal.
- <xref:Bodu.Financial.ExchangeRates.Testing.PairWebRateProviderContractTests`2> — extends the dated contract for providers built on <xref:Bodu.Financial.ExchangeRates.PairWebRateProvider`1>. Adds one abstract member, `ExpectedHistoryAvailability` (a <xref:Bodu.Financial.ExchangeRates.RateHistoryAvailability>), and four tests: the provider's declared `HistoryAvailability` matches it, `LoadPairAsync` over `RangeStart`–`RangeEnd` makes `KnownDate` resolve exactly, warming an already-covered window is idempotent, and `GetAvailablePairs()` is empty cold and reports the pair after the warm-up.

**HTTP stub**

- <xref:Bodu.Financial.ExchangeRates.Testing.StubHttpMessageHandler> — an `HttpMessageHandler` that returns fixed content for every request and records what it received. Constructed with `StubHttpMessageHandler(byte[] content, HttpStatusCode statusCode = HttpStatusCode.OK)` (throws `ArgumentNullException` for null content); exposes `RequestCount`, `LastRequestUri`, and `LastAuthorization` (the last request's `Authorization` header, or `null`). Wrap it in an `HttpClient` and hand that client to a provider's `HttpClient`-accepting constructor so the real <xref:Bodu.Financial.ExchangeRates.IPairRateSource`1> builds the request and parses the fixture — closing the gap a fixture-backed source alone leaves, namely the URL the provider actually constructs.

## Example

A derived contract test for a consumer-written provider, and a pair-web-provider contract driven hermetically over the stub handler:

```csharp
using System.Net.Http;
using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates;
using Bodu.Financial.ExchangeRates.Testing;

// A custom IDatedRateProvider: supply the seeded pair and the dates that characterise it;
// the base exercises the whole lookup surface.
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

// A pair-web provider over a canned response: the real source builds the request and parses
// the fixture; the handler records the URL so the query string can be asserted too.
[TestClass]
public sealed class FixerRateProviderContractTests
    : PairWebRateProviderContractTests<FixerRateProvider, FixerSeriesInfo>
{
    private static readonly byte[] s_fixture = File.ReadAllBytes("Fixtures/fixer-eurusd-2024-01.json");

    protected override CurrencyPair CanonicalPair => new(CurrencyCode.EUR, CurrencyCode.USD);

    protected override DateOnly KnownDate => new(2024, 1, 3);

    protected override DateOnly UnknownDate => new(2023, 12, 25);

    protected override DateOnly RangeStart => new(2024, 1, 1);

    protected override DateOnly RangeEnd => new(2024, 1, 31);

    protected override RateHistoryAvailability ExpectedHistoryAvailability =>
        RateHistoryAvailability.Since(new DateOnly(1999, 1, 1));

    protected override FixerRateProvider CreateProvider()
    {
        var handler = new StubHttpMessageHandler(s_fixture);
        var client = new HttpClient(handler);
        var options = new FixerRateProviderOptions { ApiKey = "test-key" };

        return new FixerRateProvider(client, options);
    }

    [TestMethod]
    public async Task LoadPairAsync_WhenCalled_ShouldSendAccessKey()
    {
        var handler = new StubHttpMessageHandler(s_fixture);
        using var provider = new FixerRateProvider(
            new HttpClient(handler),
            new FixerRateProviderOptions { ApiKey = "test-key" });

        await provider.LoadPairAsync("EUR", "USD", RangeStart, RangeEnd);

        Assert.AreEqual(1, handler.RequestCount);
        StringAssert.Contains(handler.LastRequestUri!.Query, "access_key=test-key");
    }
}
```

## Notes

- **Not published.** Because the project is `IsPackable=false`, the types on this page are reachable only by project reference inside the repository. The [testing guide](~/guides/financial/testing-providers.md) covers what a consumer should do instead.
- **Seeded, not imposed.** The dated contract asserts invariants — sync equals async, `GetRate` throws exactly where `TryGetRate` returns `false`, provenance is self-consistent — against the subclass's own data, so it applies equally to a fixed-base feed and to an arbitrary-pair feed, and to a cache-fronted provider (which reports `RateOrigin.Cache` where a direct one reports `RateOrigin.Live`).
- **Independent instances.** `CreateProvider()` must return a fresh instance per call; the base never shares state between tests, and drives a cache-backed provider into a steady served-from-cache state itself before asserting.
- **Stub content is fixed per handler.** The handler returns the same bytes and status code for every request; construct one per scenario when a test needs different responses.
- **See also:** the [`Bodu.Financial.ExchangeRates` reference](xref:Bodu.Financial.ExchangeRates) and the shipped provider test projects, which follow exactly this shape.
