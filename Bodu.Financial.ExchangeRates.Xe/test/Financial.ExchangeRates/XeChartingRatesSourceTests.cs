// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XeChartingRatesSourceTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Net;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies that <see cref="XeChartingRatesSource" /> builds the expected request, attaches the authorization token,
/// retries once on a rejected token, and that the whole provider stack resolves a rate over HTTP including the token
/// scrape.
/// </summary>
[TestClass]
public class XeChartingRatesSourceTests
{
    private static readonly CurrencyPair AudUsd = new(CurrencyCode.AUD, CurrencyCode.USD);

    /// <summary>
    /// Verifies that the source requests the charting-rates path with the currency query parameters and attaches the
    /// token from the token provider as a <c>Basic</c> authorization header.
    /// </summary>
    [TestMethod]
    public async Task GetPairAsync_WhenBackedByHttp_ShouldRequestChartingRatesPathWithAuthorization()
    {
        StubHttpMessageHandler handler = new(XeFixtures.ReadBytes(XeFixtures.AudUsd));
        using HttpClient client = new(handler);
        XeRateProviderOptions options = new();
        StubXeAuthTokenProvider tokens = new("TESTTOKEN");
        XeChartingRatesSource source = new(client, options, tokens);

        PairRateData<XeSeriesInfo> data = await source.GetPairAsync(Request(new(2023, 1, 2), new(2023, 1, 6)));

        Assert.AreEqual(1, handler.RequestCount);
        Assert.IsNotNull(handler.LastRequestUri);
        Assert.AreEqual("www.xe.com", handler.LastRequestUri!.Host);
        Assert.IsTrue(handler.LastRequestUri.AbsolutePath.Contains("charting-rates", StringComparison.Ordinal), handler.LastRequestUri.AbsolutePath);
        Assert.IsTrue(handler.LastRequestUri.Query.Contains("fromCurrency=AUD", StringComparison.Ordinal), handler.LastRequestUri.Query);
        Assert.IsTrue(handler.LastRequestUri.Query.Contains("toCurrency=USD", StringComparison.Ordinal), handler.LastRequestUri.Query);
        Assert.IsTrue(handler.LastRequestUri.Query.Contains("isExtended=true", StringComparison.Ordinal), handler.LastRequestUri.Query);

        Assert.IsNotNull(handler.LastAuthorization);
        Assert.AreEqual("Basic", handler.LastAuthorization!.Scheme);
        Assert.AreEqual("TESTTOKEN", handler.LastAuthorization.Parameter);
        Assert.IsGreaterThan(0, data.Observations.Count);
    }

    /// <summary>
    /// Verifies that a rejected token (<c>401</c>) triggers a single token refresh and one retry, after which the
    /// request succeeds.
    /// </summary>
    [TestMethod]
    public async Task GetPairAsync_WhenTokenRejectedOnce_ShouldRefreshAndRetry()
    {
        byte[] fixture = XeFixtures.ReadBytes(XeFixtures.AudUsd);
        RoutedHttpMessageHandler handler = new((request, index) =>
            index == 0
                ? new HttpResponseMessage(HttpStatusCode.Unauthorized)
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(fixture) });
        using HttpClient client = new(handler);
        XeRateProviderOptions options = new();
        StubXeAuthTokenProvider tokens = new();
        XeChartingRatesSource source = new(client, options, tokens);

        PairRateData<XeSeriesInfo> data = await source.GetPairAsync(Request(new(2023, 1, 2), new(2023, 1, 6)));

        Assert.HasCount(2, handler.Requests);
        Assert.AreEqual(1, tokens.RefreshCount);
        Assert.IsGreaterThan(0, data.Observations.Count);
    }

    /// <summary>
    /// Verifies that a persistently rejected token surfaces as an <see cref="HttpRequestException" /> after the single
    /// retry, rather than retrying without bound.
    /// </summary>
    [TestMethod]
    public async Task GetPairAsync_WhenTokenRejectedTwice_ShouldThrowAfterOneRetry()
    {
        RoutedHttpMessageHandler handler = new((request, index) => new HttpResponseMessage(HttpStatusCode.Forbidden));
        using HttpClient client = new(handler);
        XeRateProviderOptions options = new();
        StubXeAuthTokenProvider tokens = new();
        XeChartingRatesSource source = new(client, options, tokens);

        _ = await Assert.ThrowsExactlyAsync<HttpRequestException>(async () =>
        {
            _ = await source.GetPairAsync(Request(new(2023, 1, 2), new(2023, 1, 6)));
        });

        Assert.HasCount(2, handler.Requests);
    }

    /// <summary>
    /// Verifies that the full provider stack — token scrape, authorized charting-rates request, and delta decode —
    /// resolves a rate over HTTP without any test-only seams.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenBackedByHttp_ShouldScrapeTokenAndResolveRate()
    {
        byte[] fixture = XeFixtures.ReadBytes(XeFixtures.AudUsd);
        RoutedHttpMessageHandler handler = new((request, index) => Route(request, fixture));
        using HttpClient client = new(handler);
        XeRateProvider provider = new(client, new XeRateProviderOptions());

        await provider.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 2), new DateOnly(2023, 1, 6));

        RateLookupResult result = provider.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact);

        Assert.AreEqual(0.6828m, result.Rate.Rate);

        HttpRequestMessage? chartingRequest = handler.Requests
            .FirstOrDefault(r => r.RequestUri!.AbsolutePath.Contains("charting-rates", StringComparison.Ordinal));
        Assert.IsNotNull(chartingRequest, "the charting-rates endpoint was requested");

        // The token scraped from the _app chunk fixture is btoa("lodestar:pugsnax").
        string expectedToken = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes("lodestar:pugsnax"));
        Assert.IsNotNull(chartingRequest.Headers.Authorization);
        Assert.AreEqual("Basic", chartingRequest.Headers.Authorization!.Scheme);
        Assert.AreEqual(expectedToken, chartingRequest.Headers.Authorization.Parameter);
    }

    private static HttpResponseMessage Route(HttpRequestMessage request, byte[] fixture)
    {
        string url = request.RequestUri!.AbsoluteUri;

        if (url.Contains("currencycharts", StringComparison.Ordinal))
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(XeFixtures.ReadText(XeFixtures.Bootstrap)) };

        if (url.Contains("/_next/static/chunks/pages/", StringComparison.Ordinal))
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(XeFixtures.ReadText(XeFixtures.AppChunk)) };

        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(fixture) };
    }

    private static CurrencyPairRequest Request(DateOnly startDate, DateOnly endDate) =>
        new(AudUsd, startDate, endDate);
}
