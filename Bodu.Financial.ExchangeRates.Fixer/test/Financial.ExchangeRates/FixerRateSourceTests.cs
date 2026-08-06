// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixerRateSourceTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.ExchangeRates.Testing;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies that <see cref="FixerRateProvider" /> driven by the real HTTP source builds the expected request and
/// parses the response.
/// </summary>
/// <remarks>
/// The contract tests substitute <see cref="FixtureFixerRateSource" />, which parses an embedded fixture directly.
/// That leaves the request the provider actually issues unasserted, so these tests drive the real source over a stub
/// handler instead.
/// </remarks>
[TestClass]
public sealed class FixerRateSourceTests
{
    /// <summary>
    /// Verifies that a multi-day request targets the time-series endpoint and carries the access key, both symbols,
    /// and the inclusive date bounds.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenRangeSpansMultipleDays_ShouldRequestTimeSeriesEndpoint()
    {
        StubHttpMessageHandler handler = new(FixerFixtures.ReadBytes(FixerFixtures.EurUsdTimeSeries));
        using HttpClient client = new(handler);
        FixerRateProvider provider = new(client, new FixerRateProviderOptions { ApiKey = "test-key" });

        await provider.LoadPairAsync("EUR", "USD", new DateOnly(2023, 1, 2), new DateOnly(2023, 1, 4));

        Assert.AreEqual(1, handler.RequestCount);
        Assert.IsNotNull(handler.LastRequestUri);
        Assert.IsTrue(
            handler.LastRequestUri!.AbsolutePath.EndsWith("/timeseries", StringComparison.Ordinal),
            handler.LastRequestUri.AbsolutePath);

        string query = handler.LastRequestUri.Query;
        Assert.Contains("access_key=test-key", query);
        Assert.Contains("base=EUR", query);
        Assert.Contains("symbols=USD", query);
        Assert.Contains("start_date=2023-01-02", query);
        Assert.Contains("end_date=2023-01-04", query);
    }

    /// <summary>
    /// Verifies that a single-day request targets the historical endpoint with the date substituted into the path,
    /// and omits the range bounds the time-series endpoint requires.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenRangeIsSingleDay_ShouldRequestHistoricalEndpointWithDateInPath()
    {
        StubHttpMessageHandler handler = new(FixerFixtures.ReadBytes(FixerFixtures.EurUsdHistorical));
        using HttpClient client = new(handler);
        FixerRateProvider provider = new(client, new FixerRateProviderOptions { ApiKey = "test-key" });

        await provider.LoadPairAsync("EUR", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 3));

        Assert.AreEqual(1, handler.RequestCount);
        Assert.IsNotNull(handler.LastRequestUri);
        Assert.IsTrue(
            handler.LastRequestUri!.AbsolutePath.EndsWith("/2023-01-03", StringComparison.Ordinal),
            handler.LastRequestUri.AbsolutePath);

        string query = handler.LastRequestUri.Query;
        Assert.Contains("access_key=test-key", query);
        Assert.DoesNotContain("start_date=", query);
        Assert.DoesNotContain("end_date=", query);
    }

    /// <summary>
    /// Verifies that an access key containing URL-significant characters is escaped into the query string.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenApiKeyRequiresEscaping_ShouldEscapeItInQuery()
    {
        StubHttpMessageHandler handler = new(FixerFixtures.ReadBytes(FixerFixtures.EurUsdTimeSeries));
        using HttpClient client = new(handler);
        FixerRateProvider provider = new(client, new FixerRateProviderOptions { ApiKey = "key with spaces&more" });

        await provider.LoadPairAsync("EUR", "USD", new DateOnly(2023, 1, 2), new DateOnly(2023, 1, 4));

        Assert.IsNotNull(handler.LastRequestUri);
        Assert.Contains("access_key=key%20with%20spaces%26more", handler.LastRequestUri!.Query);
    }

    /// <summary>
    /// Verifies that the rate parsed from the downloaded response is resolvable after the load, confirming the
    /// source hands the response body to the parser unchanged.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenBackedByHttp_ShouldResolveParsedRate()
    {
        StubHttpMessageHandler handler = new(FixerFixtures.ReadBytes(FixerFixtures.EurUsdTimeSeries));
        using HttpClient client = new(handler);
        FixerRateProvider provider = new(client, new FixerRateProviderOptions { ApiKey = "test-key" });

        await provider.LoadPairAsync("EUR", "USD", new DateOnly(2023, 1, 2), new DateOnly(2023, 1, 4));

        Assert.IsTrue(provider.TryGetRate("EUR", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact, out RateLookupResult result));
        Assert.AreEqual(new DateOnly(2023, 1, 3), result.Rate.Date);
    }
}
