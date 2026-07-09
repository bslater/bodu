// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XeScrapingAuthTokenProviderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Net;
using System.Text;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies that <see cref="XeScrapingAuthTokenProvider" /> discovers and base64-encodes the authorization credential by
/// scanning the XE website's script chunks, caches it, and re-scans only when a refresh is requested or no chunk yields
/// the credential.
/// </summary>
[TestClass]
public class XeScrapingAuthTokenProviderTests
{
    /// <summary>The credential embedded in the fixtures (<c>btoa("lodestar:pugsnax")</c>).</summary>
    private static readonly string ExpectedToken = Convert.ToBase64String(Encoding.ASCII.GetBytes("lodestar:pugsnax"));

    /// <summary>A script chunk that builds the credential the current way — a template literal whose value is a single <c>btoa</c> literal.</summary>
    private const string AuthSnippet = "let h=new Headers;h.set(\"Authorization\",`Basic ${btoa(\"lodestar:pugsnax\")}`);";

    /// <summary>A benign script chunk that carries no credential.</summary>
    private const string Benign = "(function(){return 1})();";

    /// <summary>
    /// Verifies that the scan returns the base64 credential extracted from the always-loaded <c>_app</c> entry chunk,
    /// fetching only the bootstrap page and that chunk.
    /// </summary>
    [TestMethod]
    public async Task GetTokenAsync_WhenScraped_ShouldReturnBase64Credential()
    {
        RoutedHttpMessageHandler handler = new((request, index) => RouteAppChunk(request));
        using HttpClient client = new(handler);
        XeScrapingAuthTokenProvider provider = new(client, new XeRateProviderOptions());

        string token = await provider.GetTokenAsync(forceRefresh: false);

        Assert.AreEqual(ExpectedToken, token);
        Assert.HasCount(2, handler.Requests);
    }

    /// <summary>
    /// Verifies that a second non-refresh request reuses the cached token without scanning again.
    /// </summary>
    [TestMethod]
    public async Task GetTokenAsync_WhenCalledTwice_ShouldScrapeOnce()
    {
        RoutedHttpMessageHandler handler = new((request, index) => RouteAppChunk(request));
        using HttpClient client = new(handler);
        XeScrapingAuthTokenProvider provider = new(client, new XeRateProviderOptions());

        string first = await provider.GetTokenAsync(forceRefresh: false);
        string second = await provider.GetTokenAsync(forceRefresh: false);

        Assert.AreEqual(first, second);
        Assert.HasCount(2, handler.Requests);
    }

    /// <summary>
    /// Verifies that a forced refresh re-scans the website even when a token is already cached.
    /// </summary>
    [TestMethod]
    public async Task GetTokenAsync_WhenForceRefresh_ShouldScrapeAgain()
    {
        RoutedHttpMessageHandler handler = new((request, index) => RouteAppChunk(request));
        using HttpClient client = new(handler);
        XeScrapingAuthTokenProvider provider = new(client, new XeRateProviderOptions());

        _ = await provider.GetTokenAsync(forceRefresh: false);
        _ = await provider.GetTokenAsync(forceRefresh: true);

        Assert.HasCount(4, handler.Requests);
    }

    /// <summary>
    /// Verifies that the credential is discovered in a bootstrap-referenced chunk other than <c>_app</c> when the
    /// <c>_app</c> chunk does not carry it.
    /// </summary>
    [TestMethod]
    public async Task GetTokenAsync_WhenCredentialInNonAppChunk_ShouldDiscover()
    {
        RoutedHttpMessageHandler handler = new((request, index) =>
        {
            string url = request.RequestUri!.AbsoluteUri;
            if (url.Contains("currencycharts", StringComparison.Ordinal))
                return Ok(XeFixtures.ReadText(XeFixtures.Bootstrap));

            return url.Contains("currency-chart", StringComparison.Ordinal) ? Ok(AuthSnippet) : Ok(Benign);
        });
        using HttpClient client = new(handler);
        XeScrapingAuthTokenProvider provider = new(client, new XeRateProviderOptions());

        string token = await provider.GetTokenAsync(forceRefresh: false);

        Assert.AreEqual(ExpectedToken, token);
    }

    /// <summary>
    /// Verifies that the credential is discovered in a lazily-loaded chunk reachable only by reconstructing its URL from
    /// the webpack runtime chunk's identifier-to-hash map.
    /// </summary>
    [TestMethod]
    public async Task GetTokenAsync_WhenCredentialOnlyInLazyChunk_ShouldDiscoverViaRuntimeMap()
    {
        const string runtimeMap = "self.__BUILD_MANIFEST=1;var u=e=>\"static/chunks/\"+e+\".\"+{777:\"deadbeefcafe12\",888:\"0123456789abcd\"}[e]+\".js\";";

        RoutedHttpMessageHandler handler = new((request, index) =>
        {
            string url = request.RequestUri!.AbsoluteUri;
            if (url.Contains("currencycharts", StringComparison.Ordinal))
                return Ok(XeFixtures.ReadText(XeFixtures.Bootstrap));

            if (url.Contains("webpack-", StringComparison.Ordinal))
                return Ok(runtimeMap);

            return url.Contains("777.deadbeefcafe12", StringComparison.Ordinal) ? Ok(AuthSnippet) : Ok(Benign);
        });
        using HttpClient client = new(handler);
        XeScrapingAuthTokenProvider provider = new(client, new XeRateProviderOptions());

        string token = await provider.GetTokenAsync(forceRefresh: false);

        Assert.AreEqual(ExpectedToken, token);
    }

    /// <summary>
    /// Verifies that a bootstrap page that references no script chunk throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public async Task GetTokenAsync_WhenBootstrapNamesNoChunk_ShouldThrowInvalidOperationException()
    {
        RoutedHttpMessageHandler handler = new((request, index) =>
            Ok("<html><body>no chunk here</body></html>"));
        using HttpClient client = new(handler);
        XeScrapingAuthTokenProvider provider = new(client, new XeRateProviderOptions());

        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
        {
            _ = await provider.GetTokenAsync(forceRefresh: false);
        });
    }

    /// <summary>
    /// Verifies that a bundle in which no chunk carries the credential throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public async Task GetTokenAsync_WhenNoChunkHasCredential_ShouldThrowInvalidOperationException()
    {
        RoutedHttpMessageHandler handler = new((request, index) =>
        {
            string url = request.RequestUri!.AbsoluteUri;
            return url.Contains("currencycharts", StringComparison.Ordinal)
                ? Ok(XeFixtures.ReadText(XeFixtures.Bootstrap))
                : Ok(Benign);
        });
        using HttpClient client = new(handler);
        XeScrapingAuthTokenProvider provider = new(client, new XeRateProviderOptions());

        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
        {
            _ = await provider.GetTokenAsync(forceRefresh: false);
        });
    }

    private static HttpResponseMessage RouteAppChunk(HttpRequestMessage request)
    {
        string url = request.RequestUri!.AbsoluteUri;
        if (url.Contains("currencycharts", StringComparison.Ordinal))
            return Ok(XeFixtures.ReadText(XeFixtures.Bootstrap));

        return url.Contains("/pages/_app", StringComparison.Ordinal)
            ? Ok(XeFixtures.ReadText(XeFixtures.AppChunk))
            : Ok(Benign);
    }

    private static HttpResponseMessage Ok(string content) =>
        new(HttpStatusCode.OK) { Content = new StringContent(content) };
}
