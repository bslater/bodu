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
    /// Verifies that the configured <see cref="WebRateProviderOptions.MaxResponseContentBufferSize" /> is applied to the
    /// named client, as it is to a provider-owned client.
    /// </summary>
    [TestMethod]
    public void AddWebRateProvider_WhenMaxResponseContentBufferSizeConfigured_ShouldApplyItToTheNamedClient()
    {
        const long expected = 1024;
        using ServiceProvider provider = CreateServices(configure: o =>
        {
            DefaultConfigure(o);
            o.MaxResponseContentBufferSize = expected;
        }).BuildServiceProvider();

        using HttpClient client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(TestHttpClientName);

        Assert.AreEqual(expected, client.MaxResponseContentBufferSize);
    }

    /// <summary>
    /// Verifies that the named client is capped at
    /// <see cref="RateProviderHttpClientFactory.DefaultMaxResponseContentBufferSize" /> when the options leave the cap at
    /// its default.
    /// </summary>
    [TestMethod]
    public void AddWebRateProvider_WhenMaxResponseContentBufferSizeNotConfigured_ShouldApplyTheDefaultCap()
    {
        using ServiceProvider provider = CreateServices().BuildServiceProvider();

        using HttpClient client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(TestHttpClientName);

        Assert.AreEqual(RateProviderHttpClientFactory.DefaultMaxResponseContentBufferSize, client.MaxResponseContentBufferSize);
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

    /// <summary>
    /// Verifies that the explicit-selector overload applies the user agent its selector returns to the named client.
    /// </summary>
    [TestMethod]
    public void AddWebRateProvider_WhenUserAgentSelectorSupplied_ForExplicitSelectors_ShouldApplyItToTheNamedClient()
    {
        const string expected = "bodu-plain-agent/1.0";
        using ServiceProvider provider = CreateExplicitSelectorServices(configure: o =>
        {
            PlainDefaultConfigure(o);
            o.UserAgent = expected;
        }).BuildServiceProvider();

        using HttpClient client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(TestHttpClientName);

        Assert.AreEqual(expected, client.DefaultRequestHeaders.UserAgent.ToString());
    }

    /// <summary>
    /// Verifies that an options type outside the <see cref="WebRateProviderOptions" /> hierarchy leaves the named
    /// client's response cap at the default a bare <see cref="HttpClient" /> has, since such options carry no cap
    /// to apply.
    /// </summary>
    [TestMethod]
    public void AddWebRateProvider_WhenOptionsAreOutsideTheWebRateProviderOptionsHierarchy_ShouldLeaveTheClientResponseCapAtItsDefault()
    {
        using ServiceProvider provider = CreateExplicitSelectorServices().BuildServiceProvider();
        using var bare = new HttpClient();

        using HttpClient client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(TestHttpClientName);

        Assert.AreEqual(bare.MaxResponseContentBufferSize, client.MaxResponseContentBufferSize);
    }
}
