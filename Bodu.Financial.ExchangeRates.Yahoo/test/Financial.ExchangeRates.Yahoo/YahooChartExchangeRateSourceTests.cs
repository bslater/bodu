// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YahooChartExchangeRateSourceTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Yahoo;

/// <summary>
/// Verifies that <see cref="YahooExchangeRateProvider" /> driven by the real HTTP source builds the expected request
/// and parses the response.
/// </summary>
[TestClass]
public class YahooChartExchangeRateSourceTests
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
        YahooExchangeRateOptions options = new();
        YahooExchangeRateProvider provider = new(client, options);

        await provider.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));

        Assert.AreEqual(1, handler.RequestCount);
        Assert.IsNotNull(handler.LastRequestUri);
        Assert.AreEqual("query1.finance.yahoo.com", handler.LastRequestUri!.Host);
        Assert.IsTrue(handler.LastRequestUri.AbsolutePath.EndsWith("/v8/finance/chart/AUDUSD=X", StringComparison.Ordinal),
            handler.LastRequestUri.AbsolutePath);
        Assert.IsTrue(handler.LastRequestUri.Query.Contains("interval=1d", StringComparison.Ordinal), handler.LastRequestUri.Query);

        ExchangeRateLookupResult result = provider.GetRate("AUD", "USD", new DateOnly(2023, 1, 3));
        Assert.AreEqual(0.6828m, result.Rate.Rate);
    }

    /// <summary>
    /// Verifies that the source applies the configured <c>User-Agent</c> header to chart requests, so a directly
    /// constructed provider presents a recognizable agent rather than being answered with <c>429 Too Many Requests</c>.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenBackedByHttp_ShouldSendConfiguredUserAgentHeader()
    {
        StubHttpMessageHandler handler = new(YahooFixtures.ReadBytes(YahooFixtures.AudUsd));
        using HttpClient client = new(handler);
        YahooExchangeRateOptions options = new() { UserAgent = "test-agent/1.0" };
        YahooExchangeRateProvider provider = new(client, options);

        await provider.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));

        Assert.AreEqual("test-agent/1.0", handler.LastUserAgent);
    }

    /// <summary>
    /// Verifies that the default options send a non-empty <c>User-Agent</c> header, the absence of which causes the
    /// Yahoo Finance endpoint to reject requests with <c>429 Too Many Requests</c>.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenUsingDefaultOptions_ShouldSendNonEmptyUserAgentHeader()
    {
        StubHttpMessageHandler handler = new(YahooFixtures.ReadBytes(YahooFixtures.AudUsd));
        using HttpClient client = new(handler);
        YahooExchangeRateProvider provider = new(client, new YahooExchangeRateOptions());

        await provider.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));

        Assert.IsFalse(string.IsNullOrWhiteSpace(handler.LastUserAgent));
    }
}
