// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WebRateProviderExtensionsTests.HttpClient.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.ExchangeRates;

public partial class WebRateProviderExtensionsTests
{
    /// <summary>
    /// Verifies that the registration configures the named client rather than the default one, so a provider's HTTP
    /// configuration cannot leak onto unrelated clients in the same container.
    /// </summary>
    [TestMethod]
    public void AddWebRateProvider_WhenRegistered_ShouldConfigureTheNamedClientOnly()
    {
        using ServiceProvider provider = CreateServices(configure: o =>
        {
            DefaultConfigure(o);
            o.UserAgent = "bodu-test-agent";
        }).BuildServiceProvider();

        var factory = provider.GetRequiredService<IHttpClientFactory>();
        using HttpClient named = factory.CreateClient(TestHttpClientName);
        using HttpClient unrelated = factory.CreateClient("some-other-client");

        Assert.IsTrue(named.DefaultRequestHeaders.UserAgent.Count > 0);
        Assert.AreEqual(0, unrelated.DefaultRequestHeaders.UserAgent.Count);
    }

    /// <summary>
    /// Verifies that the configured user agent is applied to the named client's default request headers.
    /// </summary>
    [TestMethod]
    public void AddWebRateProvider_WhenUserAgentConfigured_ShouldApplyItToTheNamedClient()
    {
        const string expected = "bodu-test-agent/1.0";
        using ServiceProvider provider = CreateServices(configure: o =>
        {
            DefaultConfigure(o);
            o.UserAgent = expected;
        }).BuildServiceProvider();

        using HttpClient client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(TestHttpClientName);

        Assert.AreEqual(expected, client.DefaultRequestHeaders.UserAgent.ToString());
    }

    /// <summary>
    /// Verifies that a blank user agent adds no header at all, rather than an empty one.
    /// </summary>
    /// <param name="userAgent">The blank user-agent value under test.</param>
    [TestMethod]
    [DataRow("")]
    [DataRow(" ")]
    public void AddWebRateProvider_WhenUserAgentIsBlank_ShouldNotAddUserAgentHeader(string userAgent)
    {
        using ServiceProvider provider = CreateServices(configure: o =>
        {
            DefaultConfigure(o);
            o.UserAgent = userAgent;
        }).BuildServiceProvider();

        using HttpClient client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(TestHttpClientName);

        Assert.AreEqual(0, client.DefaultRequestHeaders.UserAgent.Count);
    }

    /// <summary>
    /// Verifies that the client's own timeout is disabled, because the per-attempt and total-request timeouts are
    /// owned by the resilience pipeline; a competing <see cref="HttpClient.Timeout" /> would cancel a request the
    /// pipeline still intended to retry.
    /// </summary>
    [TestMethod]
    public void AddWebRateProvider_WhenRegistered_ShouldDisableHttpClientTimeoutInFavourOfResilience()
    {
        using ServiceProvider provider = CreateServices().BuildServiceProvider();

        using HttpClient client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(TestHttpClientName);

        Assert.AreEqual(Timeout.InfiniteTimeSpan, client.Timeout);
    }
}
