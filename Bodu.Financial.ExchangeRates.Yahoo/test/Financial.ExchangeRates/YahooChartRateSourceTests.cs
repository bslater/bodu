// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YahooChartRateSourceTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
}
