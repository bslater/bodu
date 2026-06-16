// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DatedExchangeRateProviderContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial;

namespace Bodu.Financial.ExchangeRates.Testing;

/// <summary>
/// Provides the shared contract every <see cref="IDatedExchangeRateProvider" /> must satisfy, so a cache-fronted
/// provider is indistinguishable from a direct one at the API surface. A test project asserts the contract for a
/// concrete provider by deriving a <see langword="sealed" /> <c>[TestClass]</c> from this base and supplying the seed
/// knowledge through the abstract members.
/// </summary>
/// <typeparam name="TProvider">The concrete provider under test.</typeparam>
/// <remarks>
/// <para>
/// The contract is asserted against whatever data the subclass seeds, not a dataset this base imposes, so it applies
/// equally to providers with a fixed base currency (for example RBA quoting AUD) and to providers that serve arbitrary
/// pairs. Each subclass exposes the pair it is seeded for, a date that resolves, a date that does not, and the range to
/// exercise; behaviour that not every provider offers (inverse fallback, same-currency identity) is gated behind a
/// capability flag.
/// </para>
/// <para>
/// "The same contract" means the same shape and invariants on every path — synchronous results equal their asynchronous
/// counterparts, <see cref="IDatedExchangeRateProvider.GetRate(string, string, DateOnly, ExchangeRateLookupOptions)" />
/// throws exactly where the <c>TryGet</c> form returns <see langword="false" />, and every result carries a
/// self-consistent <see cref="ExchangeRateProvenance" />. It does not mean identical provenance values: a cache serve
/// reports <see cref="ExchangeRateOrigin.Cache" /> while a direct serve reports <see cref="ExchangeRateOrigin.Live" />,
/// which is the deliberate signal of where the rate came from.
/// </para>
/// </remarks>
public abstract class DatedExchangeRateProviderContractTests<TProvider>
    where TProvider : class, IDatedExchangeRateProvider
{
    /// <summary>
    /// Gets the currency pair the provider returned by <see cref="CreateProvider" /> has been seeded for.
    /// </summary>
    /// <returns>The seeded currency pair.</returns>
    protected abstract ExchangeRatePair CanonicalPair { get; }

    /// <summary>
    /// Gets a date for which the seeded provider has a resolvable observation under <see cref="ExchangeRateLookupOptions.Exact" />.
    /// </summary>
    /// <returns>A date that resolves for <see cref="CanonicalPair" />.</returns>
    protected abstract DateOnly KnownDate { get; }

    /// <summary>
    /// Gets a date for which the seeded provider has no observation and that does not resolve under
    /// <see cref="ExchangeRateLookupOptions.Exact" /> in either direction.
    /// </summary>
    /// <returns>A date that does not resolve for <see cref="CanonicalPair" />.</returns>
    protected abstract DateOnly UnknownDate { get; }

    /// <summary>
    /// Gets the inclusive start of the range exercised by the range contract. Defaults to <see cref="KnownDate" />.
    /// </summary>
    /// <returns>The inclusive start date of the contract range.</returns>
    protected virtual DateOnly RangeStart => KnownDate;

    /// <summary>
    /// Gets the inclusive end of the range exercised by the range contract. Defaults to <see cref="KnownDate" />.
    /// </summary>
    /// <returns>The inclusive end date of the contract range.</returns>
    protected virtual DateOnly RangeEnd => KnownDate;

    /// <summary>
    /// Gets a value indicating whether the provider resolves a reverse-direction (inverse) lookup. Defaults to
    /// <see langword="true" />.
    /// </summary>
    /// <returns><see langword="true" /> when inverse lookups are supported; otherwise <see langword="false" />.</returns>
    protected virtual bool SupportsInverseLookup => true;

    /// <summary>
    /// Gets a value indicating whether the provider returns a synthetic identity rate for a same-currency lookup.
    /// Defaults to <see langword="true" />.
    /// </summary>
    /// <returns><see langword="true" /> when same-currency identity rates are supported; otherwise <see langword="false" />.</returns>
    protected virtual bool SupportsIdentityRate => true;

    /// <summary>
    /// Verifies that resolving the same known date through the synchronous and asynchronous single-date surfaces yields
    /// the same rate and the same provenance lineage, so a caller cannot observe a different result merely by awaiting.
    /// </summary>
    [TestMethod]
    public async Task GetRate_WhenResolvedSyncAndAsync_ShouldReturnEquivalentResult()
    {
        TProvider provider = CreateWarmedProvider();
        string from = CanonicalPair.FromIsoCode;
        string to = CanonicalPair.ToIsoCode;

        ExchangeRateLookupResult sync = provider.GetRate(from, to, KnownDate, ExchangeRateLookupOptions.Exact);
        ExchangeRateLookupResult async = await provider.GetRateAsync(from, to, KnownDate, ExchangeRateLookupOptions.Exact);

        Assert.AreEqual(sync.Rate.Rate, async.Rate.Rate, "rate");
        Assert.AreEqual(sync.Rate.Date, async.Rate.Date, "rate date");
        Assert.AreEqual(sync.Rate.Provider, async.Rate.Provider, "rate provider");
        Assert.AreEqual(sync.RequestedDate, async.RequestedDate, "requested date");
        Assert.AreEqual(sync.Resolution, async.Resolution, "resolution");
        Assert.AreEqual(sync.OffsetDays, async.OffsetDays, "offset days");
        Assert.AreEqual(sync.Provenance.Origin, async.Provenance.Origin, "provenance origin");
        Assert.AreEqual(sync.Provenance.Provider, async.Provenance.Provider, "provenance provider");
        Assert.AreEqual(sync.Provenance.Backend, async.Provenance.Backend, "provenance backend");
        Assert.AreEqual(sync.Provenance.CachedAtUtc, async.Provenance.CachedAtUtc, "provenance cached-at");
    }

    /// <summary>
    /// Verifies that resolving the same range through the synchronous and asynchronous range surfaces yields the same
    /// observations in the same order.
    /// </summary>
    [TestMethod]
    public async Task GetRates_WhenResolvedSyncAndAsync_ShouldReturnEquivalentRange()
    {
        TProvider provider = CreateWarmedProvider();
        string from = CanonicalPair.FromIsoCode;
        string to = CanonicalPair.ToIsoCode;

        ExchangeRateRangeResult sync = provider.GetRates(from, to, RangeStart, RangeEnd);
        ExchangeRateRangeResult async = await provider.GetRatesAsync(from, to, RangeStart, RangeEnd);

        Assert.AreEqual(sync.Count, async.Count, "range count");
        for (int i = 0; i < sync.Count; i++)
        {
            Assert.AreEqual(sync[i].Date, async[i].Date, $"date at {i}");
            Assert.AreEqual(sync[i].Rate, async[i].Rate, $"rate at {i}");
        }
    }

    /// <summary>
    /// Verifies that the <c>TryGet</c> single-date surface reports failure for a date that does not resolve, rather than
    /// throwing.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenDateUnavailable_ShouldReturnFalse()
    {
        TProvider provider = CreateProvider();

        bool resolved = provider.TryGetRate(
            CanonicalPair.FromIsoCode,
            CanonicalPair.ToIsoCode,
            UnknownDate,
            ExchangeRateLookupOptions.Exact,
            out _);

        Assert.IsFalse(resolved);
    }

    /// <summary>
    /// Verifies that the throwing single-date surface throws <see cref="KeyNotFoundException" /> for the same date the
    /// <c>TryGet</c> surface reports as unavailable, so the two surfaces agree on the unresolved case.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenDateUnavailable_ShouldThrowKeyNotFoundException()
    {
        TProvider provider = CreateProvider();

        _ = Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = provider.GetRate(CanonicalPair.FromIsoCode, CanonicalPair.ToIsoCode, UnknownDate, ExchangeRateLookupOptions.Exact);
        });
    }

    /// <summary>
    /// Verifies that a resolved result carries a self-consistent provenance: a cache serve reports a backend, a cache
    /// instant, and a non-negative age, while a live serve reports none of them.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenResolved_ShouldReportConsistentProvenance()
    {
        TProvider provider = CreateWarmedProvider();

        ExchangeRateLookupResult result = provider.GetRate(
            CanonicalPair.FromIsoCode,
            CanonicalPair.ToIsoCode,
            KnownDate,
            ExchangeRateLookupOptions.Exact);

        AssertProvenanceConsistent(result.Provenance);
    }

    /// <summary>
    /// Verifies that omitting the lookup options resolves the known date, matching the documented safe default of an
    /// exact match.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenOptionsNull_ShouldResolveKnownDate()
    {
        TProvider provider = CreateWarmedProvider();
        string from = CanonicalPair.FromIsoCode;
        string to = CanonicalPair.ToIsoCode;

        ExchangeRateLookupResult withDefault = provider.GetRate(from, to, KnownDate, options: null);
        ExchangeRateLookupResult withExact = provider.GetRate(from, to, KnownDate, ExchangeRateLookupOptions.Exact);

        Assert.AreEqual(withExact.Rate.Rate, withDefault.Rate.Rate);
        Assert.AreEqual(withExact.Rate.Date, withDefault.Rate.Date);
    }

    /// <summary>
    /// Verifies that a reverse-direction lookup resolves to the reciprocal of the direct rate when the provider supports
    /// inverse fallback.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenInverseRequested_ShouldReturnReciprocalRate()
    {
        if (!SupportsInverseLookup)
        {
            Assert.Inconclusive("Provider does not support inverse lookups.");
            return;
        }

        TProvider provider = CreateProvider();
        string from = CanonicalPair.FromIsoCode;
        string to = CanonicalPair.ToIsoCode;

        decimal direct = provider.GetRate(from, to, KnownDate, ExchangeRateLookupOptions.Exact).Rate.Rate;
        decimal inverse = provider.GetRate(to, from, KnownDate, ExchangeRateLookupOptions.Exact).Rate.Rate;

        Assert.IsTrue(direct > 0m && inverse > 0m, "both directions resolve to a positive rate");
        Assert.IsTrue(Math.Abs((direct * inverse) - 1m) < 0.0001m, $"product {direct * inverse} should be ~1");
    }

    /// <summary>
    /// Verifies that a same-currency lookup returns the synthetic identity rate of <c>1</c> when the provider supports
    /// it.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenSameCurrency_ShouldReturnIdentityRate()
    {
        if (!SupportsIdentityRate)
        {
            Assert.Inconclusive("Provider does not synthesize same-currency identity rates.");
            return;
        }

        TProvider provider = CreateProvider();
        string code = CanonicalPair.FromIsoCode;

        ExchangeRateLookupResult result = provider.GetRate(code, code, KnownDate, ExchangeRateLookupOptions.Exact);

        Assert.AreEqual(1m, result.Rate.Rate);
    }

    /// <summary>
    /// Creates a cold, seeded provider for the contract under test.
    /// </summary>
    /// <returns>A newly constructed provider seeded so that <see cref="CanonicalPair" /> resolves on <see cref="KnownDate" />.</returns>
    /// <remarks>
    /// Each call returns an independent instance so a test never observes state left by another. A cache-backed provider
    /// is returned with an empty cache; <see cref="CreateWarmedProvider" /> drives it into a steady served-from-cache
    /// state when a test needs one.
    /// </remarks>
    protected abstract TProvider CreateProvider();

    /// <summary>
    /// Creates a provider and primes it so a cache-backed implementation is in a steady served-from-cache state for the
    /// known date and the contract range; a direct provider is returned unchanged.
    /// </summary>
    /// <returns>A primed provider.</returns>
    private TProvider CreateWarmedProvider()
    {
        TProvider provider = CreateProvider();
        string from = CanonicalPair.FromIsoCode;
        string to = CanonicalPair.ToIsoCode;

        // A single-date miss populates the per-row cache; a range fetch establishes the contiguous coverage a range
        // serve requires. Both are harmless no-ops for a provider without a cache.
        _ = provider.TryGetRate(from, to, KnownDate, ExchangeRateLookupOptions.Exact, out _);
        _ = provider.GetRates(from, to, RangeStart, RangeEnd);

        return provider;
    }

    /// <summary>
    /// Asserts that the supplied provenance is internally consistent for its origin.
    /// </summary>
    /// <param name="provenance">The provenance to validate.</param>
    private static void AssertProvenanceConsistent(ExchangeRateProvenance provenance)
    {
        Assert.IsFalse(string.IsNullOrEmpty(provenance.Provider), "provenance carries a provider name");

        if (provenance.Origin == ExchangeRateOrigin.Cache)
        {
            Assert.IsNotNull(provenance.Backend, "cache serve carries a backend");
            Assert.IsNotNull(provenance.CachedAtUtc, "cache serve carries a cache instant");
            Assert.IsNotNull(provenance.Age, "cache serve carries an age");
            Assert.IsTrue(provenance.Age >= TimeSpan.Zero, "cache-serve age is never negative");
        }
        else
        {
            Assert.AreEqual(ExchangeRateOrigin.Live, provenance.Origin, "origin is Live when not Cache");
            Assert.IsNull(provenance.Backend, "live serve carries no backend");
            Assert.IsNull(provenance.CachedAtUtc, "live serve carries no cache instant");
            Assert.IsNull(provenance.Age, "live serve carries no age");
        }
    }
}
