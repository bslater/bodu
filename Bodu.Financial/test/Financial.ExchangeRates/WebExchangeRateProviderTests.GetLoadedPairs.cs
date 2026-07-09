// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WebExchangeRateProviderTests.GetLoadedPairs.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

public partial class WebExchangeRateProviderTests
{
    /// <summary>
    /// Verifies that a cold provider reports no loaded pairs.
    /// </summary>
    [TestMethod]
    public void GetLoadedPairs_WhenCold_ShouldBeEmpty()
    {
        TestBulkWebExchangeRateProvider provider = CreateProvider();

        Assert.IsEmpty(provider.GetLoadedPairs());
    }

    /// <summary>
    /// Verifies that a warmed pair is surfaced through <c>GetLoadedPairs</c>, so any provider can be introspected for
    /// its loaded pairs uniformly.
    /// </summary>
    [TestMethod]
    public async Task GetLoadedPairs_AfterLoad_ShouldContainLoadedPair()
    {
        TestBulkWebExchangeRateProvider provider = CreateProvider();

        await provider.LoadPairAsync("AUD", "USD", RangeStart, RangeEnd);

        CollectionAssert.Contains(provider.GetLoadedPairs().ToList(), new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD));
    }

    /// <summary>
    /// Verifies that the warm-up and discovery surface is reachable through the shared
    /// <see cref="IPairRateLoader" /> contract, so a caller can treat any provider uniformly without knowing the
    /// concrete type.
    /// </summary>
    [TestMethod]
    public async Task GetLoadedPairs_WhenAccessedThroughInterface_ShouldReportLoadedPair()
    {
        IPairRateLoader loader = CreateProvider();

        await loader.LoadPairAsync("AUD", "USD", RangeStart, RangeEnd);

        CollectionAssert.Contains(loader.GetLoadedPairs().ToList(), new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD));
    }
}
