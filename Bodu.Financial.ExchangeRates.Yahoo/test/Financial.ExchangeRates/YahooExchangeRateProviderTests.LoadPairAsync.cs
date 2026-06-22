// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YahooExchangeRateProviderTests.LoadPairAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class YahooExchangeRateProviderTests
{
    /// <summary>
    /// Verifies that re-requesting a pair within an already-fetched window does not issue another fetch.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenRangeAlreadyCovered_ShouldNotRefetch()
    {
        (YahooExchangeRateProvider provider, FixtureYahooExchangeRateChartSource source) = Create(allowSync: false);

        await provider.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));
        await provider.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 5), new DateOnly(2023, 1, 10));

        Assert.AreEqual(1, source.GetChartCallCount);
    }

    /// <summary>
    /// Verifies that a widened window triggers a fresh fetch.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenRangeWidens_ShouldRefetch()
    {
        (YahooExchangeRateProvider provider, FixtureYahooExchangeRateChartSource source) = Create(allowSync: false);

        await provider.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 6));
        await provider.LoadPairAsync("AUD", "USD", new DateOnly(2022, 12, 1), new DateOnly(2023, 1, 31));

        Assert.AreEqual(2, source.GetChartCallCount);
    }

    /// <summary>
    /// Verifies that a request straddling an unfetched interior gap between two loaded sub-ranges triggers a fresh
    /// fetch rather than being served from the bridged min/max envelope.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenRequestStraddlesInteriorGap_ShouldRefetch()
    {
        (YahooExchangeRateProvider provider, FixtureYahooExchangeRateChartSource source) = Create(allowSync: false);

        await provider.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 10));
        await provider.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 20), new DateOnly(2023, 1, 31));
        await provider.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 5), new DateOnly(2023, 1, 25));

        Assert.AreEqual(3, source.GetChartCallCount);
    }

    /// <summary>
    /// Verifies that a request landing wholly inside one loaded sub-range is served without a fetch even when a separate
    /// disjoint sub-range has also been loaded.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenRequestInsideOneLoadedRange_ShouldNotRefetch()
    {
        (YahooExchangeRateProvider provider, FixtureYahooExchangeRateChartSource source) = Create(allowSync: false);

        await provider.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 10));
        await provider.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 20), new DateOnly(2023, 1, 31));
        await provider.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 8));

        Assert.AreEqual(2, source.GetChartCallCount);
    }

    /// <summary>
    /// Verifies that a null ISO code throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenFromIsoCodeIsNull_ShouldThrowArgumentNullException()
    {
        (YahooExchangeRateProvider provider, _) = Create(allowSync: false);

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
        {
            await provider.LoadPairAsync(null!, "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));
        });
    }
}
