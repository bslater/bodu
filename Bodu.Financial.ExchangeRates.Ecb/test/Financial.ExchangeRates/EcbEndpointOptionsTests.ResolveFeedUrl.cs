// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbEndpointOptionsTests.ResolveFeedUrl.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class EcbEndpointOptionsTests
{
    /// <summary>
    /// Verifies that a feed's absolute URL is composed from the base URL and the feed's relative file name.
    /// </summary>
    [TestMethod]
    public void ResolveFeedUrl_ShouldCombineBaseUrlAndFileName()
    {
        EcbEndpointOptions endpoint = new() { BaseUrl = new Uri("https://mirror.example/fx/") };

        Uri url = endpoint.ResolveFeedUrl(EcbRateFeed.Full);

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
    /// Verifies that a feed whose file name is not a plain relative name — a parent-directory reference, a rooted
    /// path, or an absolute URL — is rejected rather than retargeting the request outside the configured base URL.
    /// </summary>
    /// <param name="fileName">The crafted feed file name.</param>
    [TestMethod]
    [DataRow("../evil.xml")]
    [DataRow("/rooted.xml")]
    [DataRow("http://evil.example/feed.xml")]
    [DataRow("//evil.example/feed.xml")]
    public void ResolveFeedUrl_WhenFeedFileNameEscapesBase_ShouldThrowArgumentException(string fileName)
    {
        EcbEndpointOptions endpoint = new();
        EcbRateFeed feed = new("crafted", fileName, null);

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = endpoint.ResolveFeedUrl(feed);
        });

        Assert.AreEqual("feed", ex.ParamName);
    }
}
