// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbRateProviderOptionsTests.ResolveFeedUrl.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class EcbRateProviderOptionsTests
{
    /// <summary>
    /// Verifies that a feed's absolute URL is composed from the base address and the feed's relative file name.
    /// </summary>
    [TestMethod]
    public void ResolveFeedUrl_ShouldCombineBaseAddressAndFileName()
    {
        EcbRateProviderOptions options = new() { BaseAddress = new Uri("https://mirror.example/fx/") };

        Uri url = options.ResolveFeedUrl(EcbRateFeed.Full);

        Assert.AreEqual(new Uri("https://mirror.example/fx/eurofxref-hist.xml"), url);
    }

    /// <summary>
    /// Verifies that resolving a <see langword="null" /> feed throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void ResolveFeedUrl_WhenFeedIsNull_ShouldThrowArgumentNullException()
    {
        EcbRateProviderOptions options = new();

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = options.ResolveFeedUrl(null!);
        });
    }

    /// <summary>
    /// Verifies that a feed whose file name is not a plain relative name - a parent-directory reference, a rooted
    /// path, or an absolute URL - is rejected rather than retargeting the request outside the configured base address.
    /// </summary>
    /// <param name="fileName">The crafted feed file name.</param>
    [TestMethod]
    [DataRow("../evil.xml")]
    [DataRow("/rooted.xml")]
    [DataRow("http://evil.example/feed.xml")]
    [DataRow("//evil.example/feed.xml")]
    public void ResolveFeedUrl_WhenFeedFileNameEscapesBase_ShouldThrowArgumentException(string fileName)
    {
        EcbRateProviderOptions options = new();
        EcbRateFeed feed = new("crafted", fileName, null);

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = options.ResolveFeedUrl(feed);
        });

        Assert.AreEqual("feed", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a feed whose file name is empty is rejected, since an empty name resolves to the base address
    /// itself rather than to a feed beneath it.
    /// </summary>
    [TestMethod]
    public void ResolveFeedUrl_WhenFeedFileNameIsEmpty_ShouldThrowArgumentException()
    {
        EcbRateProviderOptions options = new();
        EcbRateFeed feed = new("crafted", string.Empty, null);

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = options.ResolveFeedUrl(feed);
        });

        Assert.AreEqual("feed", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a feed file name rooted with a backslash is rejected as a rooted path. A name rooted with a
    /// slash is read as an absolute file path on Unix and rejected as an absolute URI there, so the backslash is the
    /// rooted shape that reaches the rooted-path check itself on every platform.
    /// </summary>
    [TestMethod]
    public void ResolveFeedUrl_WhenFeedFileNameIsBackslashRooted_ShouldThrowArgumentException()
    {
        EcbRateProviderOptions options = new();
        EcbRateFeed feed = new("crafted", "\\rooted.xml", null);

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = options.ResolveFeedUrl(feed);
        });

        Assert.AreEqual("feed", ex.ParamName);
    }
}
