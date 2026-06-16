// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbEndpointOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Ecb;

/// <summary>
/// Verifies the connection configuration carried by <see cref="EcbEndpointOptions" />.
/// </summary>
[TestClass]
public class EcbEndpointOptionsTests
{
    /// <summary>
    /// Verifies that the defaults target the ECB <c>eurofxref</c> path with a working timeout and user-agent.
    /// </summary>
    [TestMethod]
    public void Defaults_ShouldTargetEcbEurofxref()
    {
        EcbEndpointOptions endpoint = new();

        Assert.AreEqual(new Uri("https://www.ecb.europa.eu/stats/eurofxref/"), endpoint.BaseUrl);
        Assert.AreEqual(TimeSpan.FromSeconds(30), endpoint.HttpTimeout);
        Assert.IsFalse(string.IsNullOrWhiteSpace(endpoint.UserAgent));
    }

    /// <summary>
    /// Verifies that a feed's absolute URL is composed from the base URL and the feed's relative file name.
    /// </summary>
    [TestMethod]
    public void ResolveFeedUrl_ShouldCombineBaseUrlAndFileName()
    {
        EcbEndpointOptions endpoint = new() { BaseUrl = new Uri("https://mirror.example/fx/") };

        Uri url = endpoint.ResolveFeedUrl(EcbExchangeRateFeed.Full);

        Assert.AreEqual(new Uri("https://mirror.example/fx/eurofxref-hist.xml"), url);
    }

    /// <summary>
    /// Verifies that resolving a <see langword="null" /> feed throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void ResolveFeedUrl_WhenFeedIsNull_ShouldThrowArgumentNullException()
    {
        EcbEndpointOptions endpoint = new();

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = endpoint.ResolveFeedUrl(null!);
        });
    }

    /// <summary>
    /// Verifies that the options reject a missing base URL through their own validation.
    /// </summary>
    [TestMethod]
    public void Validate_WhenBaseUrlIsNull_ShouldThrowArgumentException()
    {
        EcbEndpointOptions endpoint = new() { BaseUrl = null! };

        _ = Assert.ThrowsExactly<ArgumentException>(() => endpoint.Validate());
    }

    /// <summary>
    /// Verifies that the provider options surface a missing endpoint base URL through their aggregate validation.
    /// </summary>
    [TestMethod]
    public void ProviderOptionsValidate_WhenEndpointBaseUrlIsNull_ShouldThrowArgumentException()
    {
        EcbExchangeRateOptions options = new();
        options.Endpoint.BaseUrl = null!;

        _ = Assert.ThrowsExactly<ArgumentException>(() => options.Validate());
    }

    /// <summary>
    /// Verifies that the endpoint defaults validate successfully.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenDefault_ShouldReturnTrue()
    {
        EcbEndpointOptions endpoint = new();

        bool valid = endpoint.TryValidate(out string? error);

        Assert.IsTrue(valid);
        Assert.IsNull(error);
    }

    /// <summary>
    /// Verifies that a non-positive endpoint HTTP timeout is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenHttpTimeoutIsZero_ShouldReturnFalse()
    {
        EcbEndpointOptions endpoint = new() { HttpTimeout = TimeSpan.Zero };

        bool valid = endpoint.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }
}
