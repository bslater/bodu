// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateProviderHttpClientFactoryTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies that <see cref="RateProviderHttpClientFactory" /> applies the HTTP contract (user agent and timeout) used by
/// a provider that owns its <see cref="HttpClient" />.
/// </summary>
[TestClass]
public class RateProviderHttpClientFactoryTests
{
    /// <summary>
    /// Verifies that a non-empty user agent is set on the created client's default request headers.
    /// </summary>
    [TestMethod]
    public void Create_WhenUserAgentProvided_ShouldSetUserAgentHeader()
    {
        using HttpClient client = RateProviderHttpClientFactory.Create("test-agent/1.0", TimeSpan.FromSeconds(30));

        Assert.IsTrue(client.DefaultRequestHeaders.TryGetValues("User-Agent", out IEnumerable<string>? values));
        Assert.AreEqual("test-agent/1.0", string.Join(" ", values!));
    }

    /// <summary>
    /// Verifies that a white-space user agent leaves the header unset.
    /// </summary>
    [TestMethod]
    public void Create_WhenUserAgentWhiteSpace_ShouldNotSetUserAgentHeader()
    {
        using HttpClient client = RateProviderHttpClientFactory.Create("   ", TimeSpan.FromSeconds(30));

        Assert.IsFalse(client.DefaultRequestHeaders.Contains("User-Agent"));
    }

    /// <summary>
    /// Verifies that a positive timeout is applied to the created client.
    /// </summary>
    [TestMethod]
    public void Create_WhenTimeoutPositive_ShouldApplyTimeout()
    {
        using HttpClient client = RateProviderHttpClientFactory.Create(null, TimeSpan.FromSeconds(42));

        Assert.AreEqual(TimeSpan.FromSeconds(42), client.Timeout);
    }
}
