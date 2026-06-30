// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XeExchangeRateProviderTests.LoadPairAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class XeExchangeRateProviderTests
{
    /// <summary>
    /// Verifies that warming a pair makes the seeded known date resolve to its decoded rate from the fixture.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Smoke)]
    public async Task LoadPairAsync_WhenWarmed_ShouldResolveDecodedRate()
    {
        XeExchangeRateOptions options = new();
        FixtureXeExchangeRateSource source = new(options);
        XeExchangeRateProvider provider = new(source, options);

        await provider.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 2), new DateOnly(2023, 1, 6));

        ExchangeRateLookupResult result = provider.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), ExchangeRateLookupOptions.Exact);

        Assert.AreEqual(0.6828m, result.Rate.Rate);
        Assert.AreEqual(new DateOnly(2023, 1, 3), result.Rate.Date);
        Assert.AreEqual(XeExchangeRateProvider.ProviderName, result.Rate.Provider);
    }

    /// <summary>
    /// Verifies that a range read returns only the decoded observations within the loaded window.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenWarmed_ShouldReturnRangeWithinWindow()
    {
        XeExchangeRateOptions options = new();
        FixtureXeExchangeRateSource source = new(options);
        XeExchangeRateProvider provider = new(source, options);

        await provider.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 2), new DateOnly(2023, 1, 4));

        ExchangeRateRangeResult range = await provider.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 2), new DateOnly(2023, 1, 4));

        Assert.AreEqual(3, range.Count);
        Assert.IsTrue(range.All(rate => rate.Date >= new DateOnly(2023, 1, 2) && rate.Date <= new DateOnly(2023, 1, 4)));
    }

    /// <summary>
    /// Verifies that an inverted date range throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenRangeInverted_ShouldThrowArgumentException()
    {
        XeExchangeRateOptions options = new();
        FixtureXeExchangeRateSource source = new(options);
        XeExchangeRateProvider provider = new(source, options);

        _ = await Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
        {
            await provider.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 6), new DateOnly(2023, 1, 2));
        });
    }
}
