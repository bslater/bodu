// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImfRateProviderTests.GetAvailablePairs.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

public partial class ImfRateProviderTests
{
    /// <summary>
    /// Verifies that no pairs are reported before any month has been loaded.
    /// </summary>
    [TestMethod]
    public void GetAvailablePairs_WhenNothingLoaded_ShouldBeEmpty()
    {
        ImfRateProviderOptions options = new() { EnableDiskCache = false };
        using ImfRateProvider provider = new(new FixtureImfRateTableSource(options), options);

        Assert.AreEqual(0, provider.GetAvailablePairs().Count);
    }

    /// <summary>
    /// Verifies that after a month is loaded the discovered pairs include the USD-anchored series in the report.
    /// </summary>
    [TestMethod]
    public async Task GetAvailablePairs_WhenMonthLoaded_ShouldIncludeUsdAnchoredPairs()
    {
        using ImfRateProvider provider = await CreateLoadedProviderAsync();

        IReadOnlyCollection<ImfSeriesInfo> pairs = provider.GetAvailablePairs();

        Assert.IsGreaterThan(0, pairs.Count);
        Assert.IsTrue(pairs.Any(s => s.Pair.From == CurrencyCode.USD && s.Pair.To == CurrencyCode.JPY));
    }
}
