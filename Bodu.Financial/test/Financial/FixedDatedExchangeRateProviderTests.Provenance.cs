// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixedDatedExchangeRateProviderTests.Provenance.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class FixedDatedExchangeRateProviderTests
{
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
