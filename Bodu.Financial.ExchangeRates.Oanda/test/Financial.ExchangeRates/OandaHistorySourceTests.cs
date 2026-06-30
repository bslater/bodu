// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OandaHistorySourceTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies that <see cref="OandaExchangeRateProvider" /> driven by the real HTTP source builds the expected request,
/// primes the session, re-primes on a challenge, and parses the response.
/// </summary>
[TestClass]
public class OandaHistorySourceTests
{
    /// <summary>
    /// Verifies that the provider issues a request to the configured data path with the source, currency, price, and
    /// period query parameters, and then resolves the parsed rate, with the Unix-millisecond timestamp mapped to the
    /// expected calendar date.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenBackedByHttp_ShouldRequestUpdatePathAndResolveRate()
    {
        StubHttpMessageHandler handler = new(OandaFixtures.ReadBytes(OandaFixtures.AudUsd));
        using HttpClient client = new(handler);
        OandaExchangeRateOptions options = new();
        OandaExchangeRateProvider provider = new(client, options);

        await provider.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));

        Assert.IsNotNull(handler.LastRequestUri);
        Assert.AreEqual("fxds-hcc.oanda.com", handler.LastRequestUri!.Host);
        Assert.IsTrue(
            handler.LastRequestUri.AbsolutePath.EndsWith("/api/data/update/", StringComparison.Ordinal),
            handler.LastRequestUri.AbsolutePath);
        Assert.IsTrue(handler.LastRequestUri.Query.Contains("source=OANDA", StringComparison.Ordinal), handler.LastRequestUri.Query);
        Assert.IsTrue(handler.LastRequestUri.Query.Contains("base_currency=AUD", StringComparison.Ordinal), handler.LastRequestUri.Query);
        Assert.IsTrue(handler.LastRequestUri.Query.Contains("quote_currency_0=USD", StringComparison.Ordinal), handler.LastRequestUri.Query);
        Assert.IsTrue(handler.LastRequestUri.Query.Contains("price=mid", StringComparison.Ordinal), handler.LastRequestUri.Query);
        Assert.IsTrue(handler.LastRequestUri.Query.Contains("period=daily", StringComparison.Ordinal), handler.LastRequestUri.Query);

        ExchangeRateLookupResult result = provider.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), ExchangeRateLookupOptions.Exact);
        Assert.AreEqual(0.6828m, result.Rate.Rate);
        Assert.AreEqual(new DateOnly(2023, 1, 3), result.Rate.Date);
    }

    /// <summary>
    /// Verifies that the inclusive request range is sent as unpadded <c>YYYY-M-D</c> start and end dates.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenBackedByHttp_ShouldSendUnpaddedDates()
    {
        StubHttpMessageHandler handler = new(OandaFixtures.ReadBytes(OandaFixtures.AudUsd));
        using HttpClient client = new(handler);
        OandaExchangeRateProvider provider = new(client, new OandaExchangeRateOptions());

        await provider.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 31));

        Assert.IsNotNull(handler.LastRequestUri);
        Assert.IsTrue(handler.LastRequestUri!.Query.Contains("start_date=2023-1-3", StringComparison.Ordinal), handler.LastRequestUri.Query);
        Assert.IsTrue(handler.LastRequestUri.Query.Contains("end_date=2023-1-31", StringComparison.Ordinal), handler.LastRequestUri.Query);
    }

    /// <summary>
    /// Verifies that rows the server returns outside the requested inclusive window are filtered out, so the in-memory
    /// store holds only observations within the loaded range.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenFixtureHasRowsOutsideRange_ShouldExcludeThem()
    {
        StubHttpMessageHandler handler = new(OandaFixtures.ReadBytes(OandaFixtures.AudUsd));
        using HttpClient client = new(handler);
        OandaExchangeRateProvider provider = new(client, new OandaExchangeRateOptions());

        await provider.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 3));

        ExchangeRateRangeResult range = provider.GetRates("AUD", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 3));

        Assert.AreEqual(2, range.Count);
        Assert.IsTrue(range.All(rate => rate.Date <= new DateOnly(2023, 1, 3)), "no observation later than the loaded window");
    }

    /// <summary>
    /// Verifies that a bot-mitigation challenge on the data request is recovered by re-priming the session and retrying,
    /// so the rate ultimately resolves.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenChallengedOnce_ShouldRePrimeAndSucceed()
    {
        ChallengeThenFixtureHttpMessageHandler handler = new(OandaFixtures.ReadBytes(OandaFixtures.AudUsd), challengesBeforeSuccess: 1);
        using HttpClient client = new(handler);
        OandaExchangeRateProvider provider = new(client, new OandaExchangeRateOptions());

        await provider.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));

        Assert.AreEqual(2, handler.DataRequestCount, "the challenged data request is retried once");
        Assert.IsGreaterThanOrEqualTo(2, handler.PrimeRequestCount, "the session is primed and re-primed");

        ExchangeRateLookupResult result = provider.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), ExchangeRateLookupOptions.Exact);
        Assert.AreEqual(0.6828m, result.Rate.Rate);
    }
}
