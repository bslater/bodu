// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XeScrapingAuthTokenProviderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Net;
using System.Text;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies that <see cref="XeScrapingAuthTokenProvider" /> discovers and base64-encodes the authorization credential
/// from the XE website, caches it, and re-scrapes only when a refresh is requested or the bundle cannot be parsed.
/// </summary>
[TestClass]
public class XeScrapingAuthTokenProviderTests
{
    /// <summary>The credential embedded in the <c>xe-app-chunk.js</c> fixture (<c>"lodestar" + ":" + "s3cr3t"</c>).</summary>
    private static readonly string ExpectedToken = Convert.ToBase64String(Encoding.ASCII.GetBytes("lodestar:s3cr3t"));

    /// <summary>
    /// Verifies that the two-step scrape returns the base64 credential extracted from the application script chunk.
    /// </summary>
    [TestMethod]
    public async Task GetTokenAsync_WhenScraped_ShouldReturnBase64Credential()
    {
        RoutedHttpMessageHandler handler = new((request, index) => RouteSuccess(request));
        using HttpClient client = new(handler);
        XeScrapingAuthTokenProvider provider = new(client, new XeExchangeRateOptions());

        string token = await provider.GetTokenAsync(forceRefresh: false);

        Assert.AreEqual(ExpectedToken, token);
        Assert.HasCount(2, handler.Requests);
    }

    /// <summary>
    /// Verifies that a second non-refresh request reuses the cached token without scraping again.
    /// </summary>
    [TestMethod]
    public async Task GetTokenAsync_WhenCalledTwice_ShouldScrapeOnce()
    {
        RoutedHttpMessageHandler handler = new((request, index) => RouteSuccess(request));
        using HttpClient client = new(handler);
        XeScrapingAuthTokenProvider provider = new(client, new XeExchangeRateOptions());

        string first = await provider.GetTokenAsync(forceRefresh: false);
        string second = await provider.GetTokenAsync(forceRefresh: false);

        Assert.AreEqual(first, second);
        Assert.HasCount(2, handler.Requests);
    }

    /// <summary>
    /// Verifies that a forced refresh re-scrapes the website even when a token is already cached.
    /// </summary>
    [TestMethod]
    public async Task GetTokenAsync_WhenForceRefresh_ShouldScrapeAgain()
    {
        RoutedHttpMessageHandler handler = new((request, index) => RouteSuccess(request));
        using HttpClient client = new(handler);
        XeScrapingAuthTokenProvider provider = new(client, new XeExchangeRateOptions());

        _ = await provider.GetTokenAsync(forceRefresh: false);
        _ = await provider.GetTokenAsync(forceRefresh: true);

        Assert.HasCount(4, handler.Requests);
    }

    /// <summary>
    /// Verifies that a bootstrap page that names no application script chunk throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public async Task GetTokenAsync_WhenBootstrapNamesNoChunk_ShouldThrowInvalidOperationException()
    {
        RoutedHttpMessageHandler handler = new((request, index) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<html><body>no chunk here</body></html>") });
        using HttpClient client = new(handler);
        XeScrapingAuthTokenProvider provider = new(client, new XeExchangeRateOptions());

        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
        {
            _ = await provider.GetTokenAsync(forceRefresh: false);
        });
    }

    /// <summary>
    /// Verifies that an application script chunk without the authorization marker throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public async Task GetTokenAsync_WhenChunkLacksAuthorizationMarker_ShouldThrowInvalidOperationException()
    {
        RoutedHttpMessageHandler handler = new((request, index) =>
        {
            string url = request.RequestUri!.AbsoluteUri;
            return url.Contains("currencycharts", StringComparison.Ordinal)
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(XeFixtures.ReadText(XeFixtures.Bootstrap)) }
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("(function(){return 1})();") };
        });
        using HttpClient client = new(handler);
        XeScrapingAuthTokenProvider provider = new(client, new XeExchangeRateOptions());

        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
        {
            _ = await provider.GetTokenAsync(forceRefresh: false);
        });
    }

    private static HttpResponseMessage RouteSuccess(HttpRequestMessage request)
    {
        string url = request.RequestUri!.AbsoluteUri;

        return url.Contains("currencycharts", StringComparison.Ordinal)
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(XeFixtures.ReadText(XeFixtures.Bootstrap)) }
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(XeFixtures.ReadText(XeFixtures.AppChunk)) };
    }
}
