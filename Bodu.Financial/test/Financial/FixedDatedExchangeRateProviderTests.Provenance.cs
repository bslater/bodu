// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixedDatedExchangeRateProviderTests.Provenance.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class FixedDatedExchangeRateProviderTests
{
    /// <summary>
    /// A representative fetch instant used by the provenance stamping cases.
    /// </summary>
    private static readonly DateTimeOffset s_fetchedAt = new(2024, 1, 3, 7, 45, 0, TimeSpan.Zero);

    /// <summary>
    /// Builds a provider over a book whose single USD/AUD series carries <see cref="s_fetchedAt" /> as its fetch
    /// instant.
    /// </summary>
    /// <returns>The provider backed by the stamped series.</returns>
    private static FixedDatedExchangeRateProvider StampedProvider()
    {
        ExchangeRateTableBuilder builder = new();
        builder.Upsert(new ExchangeRatePair("USD", "AUD"), "RBA", s_d1, 1.50m, s_fetchedAt);
        return new FixedDatedExchangeRateProvider(builder.ToBook());
    }

    /// <summary>
    /// Verifies that a single-date lookup stamps the served rate with the resolving series' fetch instant.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenSeriesHasFetchInstant_ShouldStampServedRate()
    {
        FixedDatedExchangeRateProvider table = StampedProvider();

        Assert.IsTrue(table.TryGetRate("USD", "AUD", s_d1, ExchangeRateLookupOptions.Exact, out ExchangeRateLookupResult result));

        Assert.AreEqual(s_fetchedAt, result.Rate.FetchedAtUtc);
    }

    /// <summary>
    /// Verifies that an inverse single-date lookup stamps the served rate with the resolving series' fetch instant.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenInverseSeriesHasFetchInstant_ShouldStampServedRate()
    {
        FixedDatedExchangeRateProvider table = StampedProvider();

        // USD->AUD is stored; AUD->USD is resolved by inverting it and inherits the series' fetch instant.
        Assert.IsTrue(table.TryGetRate("AUD", "USD", s_d1, ExchangeRateLookupOptions.Exact, out ExchangeRateLookupResult result));

        Assert.IsTrue(result.Rate.IsInverted);
        Assert.AreEqual(s_fetchedAt, result.Rate.FetchedAtUtc);
    }

    /// <summary>
    /// Verifies that <see cref="FixedDatedExchangeRateProvider.GetRatesAsync" /> stamps every rate in a resolved range
    /// with the resolving series' fetch instant.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenSeriesHasFetchInstant_ShouldStampEveryRate()
    {
        ExchangeRateTableBuilder builder = new();
        builder.Upsert(new ExchangeRatePair("USD", "AUD"), "RBA", new DateOnly(2024, 1, 2), 1.49m, s_fetchedAt);
        builder.Upsert(new ExchangeRatePair("USD", "AUD"), "RBA", new DateOnly(2024, 1, 3), 1.50m, s_fetchedAt);
        FixedDatedExchangeRateProvider table = new(builder.ToBook());

        IReadOnlyList<ExchangeRate> rates = await table.GetRatesAsync("USD", "AUD", new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 31));

        Assert.AreEqual(2, rates.Count);
        Assert.IsTrue(rates.All(r => r.FetchedAtUtc == s_fetchedAt));
    }

    /// <summary>
    /// Verifies that a same-currency identity lookup leaves the served rate's fetch instant <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenSameCurrencyIdentity_ShouldLeaveFetchInstantNull()
    {
        FixedDatedExchangeRateProvider table = new([]);

        Assert.IsTrue(table.TryGetRate("USD", "USD", s_d1, ExchangeRateLookupOptions.Exact, out ExchangeRateLookupResult result));

        Assert.IsNull(result.Rate.FetchedAtUtc);
    }

    /// <summary>
    /// Verifies that a rate resolved directly from the table reports <see cref="ExchangeRateOrigin.Live" /> provenance
    /// attributed to the observation's provider, with no cache backend, cache instant, or age.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenResolvedFromTable_ShouldReportLiveProvenanceWithSourceProviderAndNoCacheFields()
    {
        FixedDatedExchangeRateProvider table = new(SingleRate());

        Assert.IsTrue(table.TryGetRate("USD", "AUD", s_d1, ExchangeRateLookupOptions.Exact, out ExchangeRateLookupResult result));

        Assert.AreEqual(ExchangeRateOrigin.Live, result.Provenance.Origin);
        Assert.AreEqual("RBA", result.Provenance.Provider);
        Assert.IsNull(result.Provenance.Backend);
        Assert.IsNull(result.Provenance.CachedAtUtc);
        Assert.IsNull(result.Provenance.Age);
    }

    /// <summary>
    /// Verifies that the result returned by <see cref="FixedDatedExchangeRateProvider.GetRate" /> carries the same
    /// <see cref="ExchangeRateOrigin.Live" /> provenance as the matching <c>TryGetRate</c> call.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenResolvedFromTable_ShouldReportLiveProvenance()
    {
        FixedDatedExchangeRateProvider table = new(SingleRate());

        ExchangeRateLookupResult result = table.GetRate("USD", "AUD", s_d1, ExchangeRateLookupOptions.Exact);

        Assert.AreEqual(ExchangeRateOrigin.Live, result.Provenance.Origin);
        Assert.AreEqual("RBA", result.Provenance.Provider);
        Assert.IsNull(result.Provenance.Backend);
    }

    /// <summary>
    /// Verifies that an inverse resolution reports <see cref="ExchangeRateOrigin.Live" /> provenance attributed to the
    /// observation's provider, with no cache fields populated.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenResolvedFromInverseSeries_ShouldReportLiveProvenance()
    {
        FixedDatedExchangeRateProvider table = new(SingleRate());

        // USD->AUD is stored; AUD->USD is resolved by inverting it.
        Assert.IsTrue(table.TryGetRate("AUD", "USD", s_d1, ExchangeRateLookupOptions.Exact, out ExchangeRateLookupResult result));

        Assert.IsTrue(result.Rate.IsInverted);
        Assert.AreEqual(ExchangeRateOrigin.Live, result.Provenance.Origin);
        Assert.AreEqual("RBA", result.Provenance.Provider);
        Assert.IsNull(result.Provenance.Backend);
        Assert.IsNull(result.Provenance.CachedAtUtc);
        Assert.IsNull(result.Provenance.Age);
    }

    /// <summary>
    /// Verifies that a same-currency identity rate reports <see cref="ExchangeRateOrigin.Live" /> provenance attributed
    /// to the synthetic <see cref="FixedDatedExchangeRateProvider.IdentityProviderName" /> source, with no cache fields
    /// populated.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenSameCurrencyIdentity_ShouldReportLiveProvenanceWithIdentityProvider()
    {
        FixedDatedExchangeRateProvider table = new([]);

        Assert.IsTrue(table.TryGetRate("USD", "USD", s_d1, ExchangeRateLookupOptions.Exact, out ExchangeRateLookupResult result));

        Assert.AreEqual(ExchangeRateOrigin.Live, result.Provenance.Origin);
        Assert.AreEqual(FixedDatedExchangeRateProvider.IdentityProviderName, result.Provenance.Provider);
        Assert.AreEqual("Identity", result.Provenance.Provider);
        Assert.IsNull(result.Provenance.Backend);
        Assert.IsNull(result.Provenance.CachedAtUtc);
        Assert.IsNull(result.Provenance.Age);
    }
}
