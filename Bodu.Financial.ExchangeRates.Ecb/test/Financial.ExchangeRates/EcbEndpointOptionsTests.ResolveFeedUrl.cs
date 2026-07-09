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
}
