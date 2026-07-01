// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OfxSpotRateHistorySourceTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies that <see cref="OfxExchangeRateProvider" /> driven by the real HTTP source builds the expected request and
/// parses the response.
/// </summary>
[TestClass]
public partial class OfxSpotRateHistorySourceTests
{
    /// <summary>
    /// Verifies that the provider issues a request to the configured history path with the currency codes and the
    /// inclusive range expressed as Unix-millisecond bounds, the documented query parameters, and then resolves the
    /// parsed rate, with the Unix-millisecond timestamp mapped to the expected calendar date.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenBackedByHttp_ShouldRequestHistoryPathAndResolveRate()
    {
        StubHttpMessageHandler handler = new(OfxFixtures.ReadBytes(OfxFixtures.AudUsd));
        using HttpClient client = new(handler);
        OfxExchangeRateOptions options = new();
        OfxExchangeRateProvider provider = new(client, options);

        await provider.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));

        long expectedStart = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
        long expectedEnd = new DateTimeOffset(2023, 1, 31, 23, 59, 59, 999, TimeSpan.Zero).ToUnixTimeMilliseconds();

        Assert.AreEqual(1, handler.RequestCount);
        Assert.IsNotNull(handler.LastRequestUri);
        Assert.AreEqual("api.ofx.com", handler.LastRequestUri!.Host);
        Assert.IsTrue(
            handler.LastRequestUri.AbsolutePath.EndsWith($"/SpotRateHistory/AUD/USD/{expectedStart}/{expectedEnd}", StringComparison.Ordinal),
            handler.LastRequestUri.AbsolutePath);
        Assert.IsTrue(handler.LastRequestUri.Query.Contains("DecimalPlaces=6", StringComparison.Ordinal), handler.LastRequestUri.Query);
        Assert.IsTrue(handler.LastRequestUri.Query.Contains("ReportingInterval=daily", StringComparison.Ordinal), handler.LastRequestUri.Query);
        Assert.IsTrue(handler.LastRequestUri.Query.Contains("format=json", StringComparison.Ordinal), handler.LastRequestUri.Query);

        ExchangeRateLookupResult result = provider.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), ExchangeRateLookupOptions.Exact);
        Assert.AreEqual(0.6828m, result.Rate.Rate);
        Assert.AreEqual(new DateOnly(2023, 1, 3), result.Rate.Date);
    }

    /// <summary>
    /// Verifies that points the server returns outside the requested inclusive window are filtered out, so the in-memory
    /// store holds only observations within the loaded range.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenFixtureHasPointsOutsideRange_ShouldExcludeThem()
    {
        StubHttpMessageHandler handler = new(OfxFixtures.ReadBytes(OfxFixtures.AudUsd));
        using HttpClient client = new(handler);
        OfxExchangeRateProvider provider = new(client, new OfxExchangeRateOptions());

        await provider.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 3));

        ExchangeRateRangeResult range = provider.GetRates("AUD", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 3));

        Assert.AreEqual(2, range.Count);
        Assert.IsTrue(range.All(rate => rate.Date <= new DateOnly(2023, 1, 3)), "no observation later than the loaded window");
    }
}
