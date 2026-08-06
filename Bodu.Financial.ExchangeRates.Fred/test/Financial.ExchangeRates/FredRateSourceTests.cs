// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FredRateSourceTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.ExchangeRates.Testing;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies that <see cref="FredRateProvider" /> driven by the real HTTP source builds the expected request and
/// parses the response.
/// </summary>
/// <remarks>
/// The contract tests substitute <see cref="FixtureFredRateSource" />, which parses an embedded fixture directly.
/// That leaves the request the provider actually issues unasserted, so these tests drive the real source over a stub
/// handler instead.
/// </remarks>
[TestClass]
public sealed class FredRateSourceTests
{
    /// <summary>
    /// Verifies that a mapped pair resolves to its FRED series identifier and requests the observations endpoint
    /// with the api key, file type, inclusive observation bounds, and ascending sort order.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenPairIsMapped_ShouldRequestObservationsForMappedSeries()
    {
        StubHttpMessageHandler handler = new(FredFixtures.ReadBytes(FredFixtures.DexUsEu));
        using HttpClient client = new(handler);
        FredRateProvider provider = new(client, new FredRateProviderOptions { ApiKey = "test-key" });

        await provider.LoadPairAsync("EUR", "USD", new DateOnly(2023, 1, 2), new DateOnly(2023, 1, 4));

        Assert.AreEqual(1, handler.RequestCount);
        Assert.IsNotNull(handler.LastRequestUri);

        string query = handler.LastRequestUri!.Query;
        Assert.Contains("series_id=DEXUSEU", query);
        Assert.Contains("api_key=test-key", query);
        Assert.Contains("file_type=json", query);
        Assert.Contains("observation_start=2023-01-02", query);
        Assert.Contains("observation_end=2023-01-04", query);
        Assert.Contains("sort_order=asc", query);
    }

    /// <summary>
    /// Verifies that an unmapped pair short-circuits to an empty result without issuing a request, so an
    /// unconfigured pair costs nothing rather than producing a malformed call.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenPairIsUnmapped_ShouldNotIssueRequest()
    {
        StubHttpMessageHandler handler = new(FredFixtures.ReadBytes(FredFixtures.DexUsEu));
        using HttpClient client = new(handler);
        FredRateProvider provider = new(client, new FredRateProviderOptions { ApiKey = "test-key" });

        await provider.LoadPairAsync("SEK", "NOK", new DateOnly(2023, 1, 2), new DateOnly(2023, 1, 4));

        Assert.AreEqual(0, handler.RequestCount);
        Assert.IsNull(handler.LastRequestUri);
    }

    /// <summary>
    /// Verifies that an api key containing URL-significant characters is escaped into the query string.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenApiKeyRequiresEscaping_ShouldEscapeItInQuery()
    {
        StubHttpMessageHandler handler = new(FredFixtures.ReadBytes(FredFixtures.DexUsEu));
        using HttpClient client = new(handler);
        FredRateProvider provider = new(client, new FredRateProviderOptions { ApiKey = "key with spaces&more" });

        await provider.LoadPairAsync("EUR", "USD", new DateOnly(2023, 1, 2), new DateOnly(2023, 1, 4));

        Assert.IsNotNull(handler.LastRequestUri);
        Assert.Contains("api_key=key%20with%20spaces%26more", handler.LastRequestUri!.Query);
    }

    /// <summary>
    /// Verifies that a pair added to the series map is honoured, confirming the map rather than a hard-coded table
    /// drives series resolution.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenSeriesMapExtended_ShouldRequestTheConfiguredSeries()
    {
        StubHttpMessageHandler handler = new(FredFixtures.ReadBytes(FredFixtures.DexUsEu));
        using HttpClient client = new(handler);
        var options = new FredRateProviderOptions { ApiKey = "test-key" };
        options.SeriesMap["SEK/NOK"] = "CUSTOMSERIES";
        FredRateProvider provider = new(client, options);

        await provider.LoadPairAsync("SEK", "NOK", new DateOnly(2023, 1, 2), new DateOnly(2023, 1, 4));

        Assert.AreEqual(1, handler.RequestCount);
        Assert.IsNotNull(handler.LastRequestUri);
        Assert.Contains("series_id=CUSTOMSERIES", handler.LastRequestUri!.Query);
    }

    /// <summary>
    /// Verifies that the rate parsed from the downloaded response is resolvable after the load, confirming the
    /// source hands the response body to the parser unchanged.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenBackedByHttp_ShouldResolveParsedRate()
    {
        StubHttpMessageHandler handler = new(FredFixtures.ReadBytes(FredFixtures.DexUsEu));
        using HttpClient client = new(handler);
        FredRateProvider provider = new(client, new FredRateProviderOptions { ApiKey = "test-key" });

        await provider.LoadPairAsync("EUR", "USD", new DateOnly(2023, 1, 2), new DateOnly(2023, 1, 4));

        Assert.IsTrue(provider.TryGetRate("EUR", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact, out RateLookupResult result));
        Assert.AreEqual(new DateOnly(2023, 1, 3), result.Rate.Date);
    }
}
