// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PairWebExchangeRateProviderContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial;

namespace Bodu.Financial.ExchangeRates.Testing;

/// <summary>
/// Provides the shared contract every pair-based web provider built on <see cref="PairWebExchangeRateProvider{TSeries}" />
/// must satisfy, layered on top of the general <see cref="DatedExchangeRateProviderContractTests{TProvider}" />. It
/// exercises the per-pair warm-up surface — <see cref="PairWebExchangeRateProvider{TSeries}.LoadPairAsync" /> and
/// <see cref="PairWebExchangeRateProvider{TSeries}.GetAvailablePairs" /> — that the shared base contributes, so each
/// concrete source (Yahoo, OFX, …) proves the inherited machinery against its own fixture by deriving a
/// <see langword="sealed" /> <c>[TestClass]</c> from this base.
/// </summary>
/// <typeparam name="TProvider">The concrete pair-based provider under test.</typeparam>
/// <typeparam name="TSeries">The provider's series-metadata type surfaced through <c>GetAvailablePairs</c>.</typeparam>
/// <remarks>
/// The contract is asserted against whatever pair the subclass seeds through
/// <see cref="DatedExchangeRateProviderContractTests{TProvider}.CanonicalPair" />, so it applies to any pair-based
/// source without imposing a dataset. The exact <see cref="ArgumentException" /> subtype a provider throws for an
/// inverted range is source-specific and remains covered by each provider's local tests.
/// </remarks>
public abstract class PairWebExchangeRateProviderContractTests<TProvider, TSeries>
    : DatedExchangeRateProviderContractTests<TProvider>
    where TProvider : PairWebExchangeRateProvider<TSeries>
{
    /// <summary>
    /// Verifies that explicitly warming a cold provider through <c>LoadPairAsync</c> makes the seeded known date
    /// resolve to a positive rate, exercising the per-pair fetch-and-accumulate path.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenWarmed_ShouldResolveKnownDate()
    {
        TProvider provider = CreateProvider();
        string from = CanonicalPair.FromIsoCode;
        string to = CanonicalPair.ToIsoCode;

        await provider.LoadPairAsync(from, to, RangeStart, RangeEnd);

        ExchangeRateLookupResult result = provider.GetRate(from, to, KnownDate, ExchangeRateLookupOptions.Exact);

        Assert.IsTrue(result.Rate.Rate > 0m, "the warmed known date resolves to a positive rate");
    }

    /// <summary>
    /// Verifies that re-warming the same pair and window is idempotent: a second <c>LoadPairAsync</c> over an
    /// already-covered window completes without disturbing the resolved rate.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenWindowAlreadyCovered_ShouldRemainResolvable()
    {
        TProvider provider = CreateProvider();
        string from = CanonicalPair.FromIsoCode;
        string to = CanonicalPair.ToIsoCode;

        await provider.LoadPairAsync(from, to, RangeStart, RangeEnd);
        await provider.LoadPairAsync(from, to, RangeStart, RangeEnd);

        ExchangeRateLookupResult result = provider.GetRate(from, to, KnownDate, ExchangeRateLookupOptions.Exact);

        Assert.IsTrue(result.Rate.Rate > 0m);
    }

    /// <summary>
    /// Verifies that a warmed pair is surfaced through <c>GetAvailablePairs</c>, so callers can discover the loaded
    /// series without hard-coding them.
    /// </summary>
    [TestMethod]
    public async Task GetAvailablePairs_AfterWarm_ShouldReportLoadedSeries()
    {
        TProvider provider = CreateProvider();

        Assert.AreEqual(0, provider.GetAvailablePairs().Count, "a cold provider reports no series");

        await provider.LoadPairAsync(CanonicalPair.FromIsoCode, CanonicalPair.ToIsoCode, RangeStart, RangeEnd);

        Assert.IsTrue(provider.GetAvailablePairs().Count >= 1, "the warmed pair is discoverable");
    }
}
