// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YahooChartRateSourceTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.ExchangeRates.Testing;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies that <see cref="YahooRateProvider" /> driven by the real HTTP source builds the expected request
/// and parses the response.
/// </summary>
[TestClass]
public class YahooChartRateSourceTests
{
    /// <summary>
    /// Verifies that the provider issues a request to the configured chart path with the ticker and date window, then
    /// resolves the parsed rate.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenBackedByHttp_ShouldRequestChartPathAndResolveRate()
    {
        StubHttpMessageHandler handler = new(YahooFixtures.ReadBytes(YahooFixtures.AudUsd));
        using HttpClient client = new(handler);
        YahooRateProviderOptions options = new();
        YahooRateProvider provider = new(client, options);

        await provider.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));

        Assert.AreEqual(1, handler.RequestCount);
        Assert.IsNotNull(handler.LastRequestUri);
        Assert.AreEqual("query1.finance.yahoo.com", handler.LastRequestUri!.Host);
        Assert.IsTrue(handler.LastRequestUri.AbsolutePath.EndsWith("/v8/finance/chart/AUDUSD=X", StringComparison.Ordinal),
            handler.LastRequestUri.AbsolutePath);
        Assert.IsTrue(handler.LastRequestUri.Query.Contains("interval=1d", StringComparison.Ordinal), handler.LastRequestUri.Query);

        RateLookupResult result = provider.GetRate("AUD", "USD", new DateOnly(2023, 1, 3));
        Assert.AreEqual(0.6828m, result.Rate.Rate);
    }

    /// <summary>
    /// Verifies that the provider expresses the requested window as Unix seconds at midnight UTC: <c>period1</c> at the
    /// day before the start date, so a bar stamped the evening before in UTC is included, and <c>period2</c> at the day
    /// after the end date, which the source treats as exclusive.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenBackedByHttp_ShouldRequestTheWindowInUnixSecondsAtMidnightUtc()
    {
        StubHttpMessageHandler handler = new(YahooFixtures.ReadBytes(YahooFixtures.AudUsd));
        using HttpClient client = new(handler);
        YahooRateProviderOptions options = new();
        YahooRateProvider provider = new(client, options);

        await provider.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));

        // 2022-12-31T00:00:00Z and 2023-02-01T00:00:00Z.
        Assert.IsNotNull(handler.LastRequestUri);
        Assert.IsTrue(
            handler.LastRequestUri!.Query.Contains("period1=1672444800&period2=1675209600", StringComparison.Ordinal),
            handler.LastRequestUri.Query);
    }

    /// <summary>
    /// Verifies that for a summer start date the request window opens no later than the start date's bar, which a
    /// London-quoted pair stamps at midnight British Summer Time, 23:00 UTC on the day before.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenStartDateBarIsStampedTheDayBeforeInUtc_ShouldRequestFromBeforeThatBar()
    {
        WindowedChartHttpMessageHandler handler = CreateLondonSummerChart();
        using HttpClient client = new(handler);
        YahooRateProvider provider = new(client, new YahooRateProviderOptions());

        await provider.LoadPairAsync("AUD", "USD", new DateOnly(2023, 7, 3), new DateOnly(2023, 7, 5));

        Assert.IsNotNull(handler.LastRequestUri);
        Assert.IsLessThanOrEqualTo(1688338800L, WindowedChartHttpMessageHandler.ReadQueryValue(handler.LastRequestUri!, "period1"));
    }

    /// <summary>
    /// Verifies that the start date of a summer range is loaded when the source serves only the bars inside the
    /// requested window, its bar being stamped at 23:00 UTC on the day before.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenStartDateBarIsStampedTheDayBeforeInUtc_ShouldLoadTheStartDate()
    {
        WindowedChartHttpMessageHandler handler = CreateLondonSummerChart();
        using HttpClient client = new(handler);
        YahooRateProvider provider = new(client, new YahooRateProviderOptions());

        await provider.LoadPairAsync("AUD", "USD", new DateOnly(2023, 7, 3), new DateOnly(2023, 7, 5));

        Assert.IsTrue(provider.TryGetRate("AUD", "USD", new DateOnly(2023, 7, 3), RateLookupOptions.Exact, out RateLookupResult result));
        Assert.AreEqual(0.6656m, result.Rate.Rate);
    }

    /// <summary>
    /// Verifies that a range ending on <see cref="DateOnly.MaxValue" /> is requested through the end of that day, with
    /// <c>period2</c> at the Unix seconds of the midnight that follows it, rather than failing to compute the day after.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenEndDateIsMaxValue_ShouldRequestThroughTheLastDay()
    {
        StubHttpMessageHandler handler = new(YahooFixtures.ReadBytes(YahooFixtures.AudUsd));
        using HttpClient client = new(handler);
        YahooRateProvider provider = new(client, new YahooRateProviderOptions());

        await provider.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 1), DateOnly.MaxValue);

        // 2022-12-31T00:00:00Z and 10000-01-01T00:00:00Z, the midnight after 9999-12-31.
        Assert.IsNotNull(handler.LastRequestUri);
        Assert.AreEqual(253402300800L, WindowedChartHttpMessageHandler.ReadQueryValue(handler.LastRequestUri!, "period2"));
        Assert.AreEqual(1672444800L, WindowedChartHttpMessageHandler.ReadQueryValue(handler.LastRequestUri!, "period1"));
    }

    /// <summary>
    /// Creates a windowed chart for AUD/USD quoted on London time, holding the bars for 3, 4 and 5 July 2023, each
    /// stamped at midnight British Summer Time.
    /// </summary>
    /// <returns>The chart handler.</returns>
    private static WindowedChartHttpMessageHandler CreateLondonSummerChart() =>
        new(
            """{ "currency": "USD", "symbol": "AUDUSD=X", "gmtoffset": 3600, "exchangeTimezoneName": "Europe/London" }""",
            [(1688338800, 0.6656m), (1688425200, 0.6680m), (1688511600, 0.6705m)]);
}
