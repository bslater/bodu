// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PairWebRateProviderContractTests{T,T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Testing;

/// <summary>
/// Provides the shared contract every pair-based web provider built on
/// <see cref="PairWebRateProvider{TSeries}" /> must satisfy, layered on top of the general
/// <see cref="DatedRateProviderContractTests{TProvider}" />. It exercises the per-pair warm-up surface —
/// <see cref="WebRateProvider.LoadPairAsync" /> and
/// <see cref="PairWebRateProvider{TSeries}.GetAvailablePairs" /> — that the shared base contributes, so each
/// concrete source (Yahoo, OFX, …) proves the inherited machinery against its own fixture by deriving a
/// <see langword="sealed" /> <c>[TestClass]</c> from this base.
/// </summary>
/// <typeparam name="TProvider">The concrete pair-based provider under test.</typeparam>
/// <typeparam name="TSeries">The provider's series-metadata type surfaced through <c>GetAvailablePairs</c>.</typeparam>
/// <remarks>
/// The contract is asserted against whatever pair the subclass seeds through
/// <see cref="DatedRateProviderContractTests{TProvider}.CanonicalPair" />, so it applies to any pair-based
/// source without imposing a dataset. The exact <see cref="ArgumentException" /> subtype a provider throws for an
/// inverted range is source-specific and remains covered by each provider's local tests.
/// </remarks>
public abstract class PairWebRateProviderContractTests<TProvider, TSeries>
    : DatedRateProviderContractTests<TProvider>
    where TProvider : PairWebRateProvider<TSeries>
{
    /// <summary>
    /// Gets the history availability the provider is expected to advertise — the declared depth of the upstream source.
    /// Every pair provider must declare one deliberately (an intentional
    /// <see cref="RateHistoryAvailability.Unbounded" /> included), so this contract fails when a new provider
    /// forgets to set the value in its options.
    /// </summary>
    protected abstract RateHistoryAvailability ExpectedHistoryAvailability { get; }

    /// <summary>
    /// Verifies that the provider advertises the expected history availability, forwarded from its options, so the
    /// caching and aggregation layers can rely on every pair provider declaring its source's depth.
    /// </summary>
    [TestMethod]
    public void HistoryAvailability_ShouldMatchDeclaredExpectation()
    {
        TProvider provider = CreateProvider();

        Assert.AreEqual(ExpectedHistoryAvailability, provider.HistoryAvailability);
    }

    /// <summary>
    /// Verifies that explicitly warming a cold provider through <c>LoadPairAsync</c> makes the seeded known date
    /// resolve to a positive rate, exercising the per-pair fetch-and-accumulate path.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenWarmed_ShouldResolveKnownDate()
    {
        TProvider provider = CreateProvider();
        string from = CanonicalPair.From.ToString();
        string to = CanonicalPair.To.ToString();

        await provider.LoadPairAsync(from, to, RangeStart, RangeEnd);

        RateLookupResult result = provider.GetRate(from, to, KnownDate, RateLookupOptions.Exact);

        Assert.IsGreaterThan(0m, result.Rate.Rate, "the warmed known date resolves to a positive rate");
    }

    /// <summary>
    /// Verifies that re-warming the same pair and window is idempotent: a second <c>LoadPairAsync</c> over an
    /// already-covered window completes without disturbing the resolved rate.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenWindowAlreadyCovered_ShouldRemainResolvable()
    {
        TProvider provider = CreateProvider();
        string from = CanonicalPair.From.ToString();
        string to = CanonicalPair.To.ToString();

        await provider.LoadPairAsync(from, to, RangeStart, RangeEnd);
        await provider.LoadPairAsync(from, to, RangeStart, RangeEnd);

        RateLookupResult result = provider.GetRate(from, to, KnownDate, RateLookupOptions.Exact);

        Assert.IsGreaterThan(0m, result.Rate.Rate);
    }

    /// <summary>
    /// Verifies that a warmed pair is surfaced through <c>GetAvailablePairs</c>, so callers can discover the loaded
    /// series without hard-coding them.
    /// </summary>
    [TestMethod]
    public async Task GetAvailablePairs_AfterWarm_ShouldReportLoadedSeries()
    {
        TProvider provider = CreateProvider();

        Assert.IsEmpty(provider.GetAvailablePairs(), "a cold provider reports no series");

        await provider.LoadPairAsync(CanonicalPair.From.ToString(), CanonicalPair.To.ToString(), RangeStart, RangeEnd);

        Assert.IsGreaterThanOrEqualTo(1, provider.GetAvailablePairs().Count, "the warmed pair is discoverable");
    }
}
