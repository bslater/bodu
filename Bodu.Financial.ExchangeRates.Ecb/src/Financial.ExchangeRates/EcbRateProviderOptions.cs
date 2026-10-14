// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbRateProviderOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Configures how the <see cref="EcbRateProvider" /> downloads, caches, and interprets ECB euro reference-rate data.
/// </summary>
/// <remarks>
/// <para>
/// This type derives from <see cref="WebRateProviderOptions" />, which supplies the endpoint base address, the HTTP
/// contract (timeout, user agent and response-size cap), the synchronous-access, look-back and refresh behavior, the
/// advertised history, the currency-alias map, and the per-concern log levels. The constructor points
/// <see cref="WebRateProviderOptions.BaseAddress" /> at the ECB's <c>eurofxref</c> statistics path and identifies the
/// requests with a Bodu user agent. The members declared here are ECB-specific: the <see cref="Feeds" /> catalog and
/// the on-disk cache settings.
/// </para>
/// <para>
/// The absolute URL of a feed is its <see cref="EcbRateFeed.FileName" /> resolved against
/// <see cref="WebRateProviderOptions.BaseAddress" />. An on-demand load reads the feed that covers the whole
/// <see cref="WebRateProviderOptions.DefaultLookback" /> window ending on the requested date: the narrowest feed whose
/// look-back reaches the window's first day. The inherited <see cref="WebRateProviderOptions.RefreshInterval" />
/// governs both the in-memory feeds and the on-disk cache: every ECB feed extends to the most recent business day, so a
/// cached feed older than the interval is downloaded again, and in memory a loaded feed serves for good a request that
/// ends before the day it was downloaded, or on a day it returned rates for, while a request that reaches a later day
/// downloads the feed again once the interval has passed.
/// </para>
/// <para>
/// The ECB publishes ISO 4217 codes, so <see cref="WebRateProviderOptions.CurrencyAliases" /> is empty by default; a
/// currency label in a feed that is found among its keys is replaced by its value while the feed is read. The provider
/// advertises the depth of its <see cref="Feeds" /> catalog while
/// <see cref="WebRateProviderOptions.HistoryAvailability" /> is left at
/// <see cref="RateHistoryAvailability.Unbounded" />, and any other value set there in its place. Every member carries a
/// working default, so the options bind cleanly through <c>Microsoft.Extensions.Options</c>; the dependency-injection
/// registration binds this type from configuration and a <c>configure</c> delegate.
/// </para>
/// </remarks>
public sealed class EcbRateProviderOptions
    : WebRateProviderOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EcbRateProviderOptions" /> class with the ECB <c>eurofxref</c>
    /// statistics path as its base address and a Bodu user agent.
    /// </summary>
    public EcbRateProviderOptions()
    {
        BaseAddress = new Uri("https://www.ecb.europa.eu/stats/eurofxref/");
        UserAgent = "Bodu.Financial.ExchangeRates.Ecb";
    }

    /// <summary>
    /// Gets or sets the catalog of feeds to draw from, ordered from the narrowest look-back to the widest.
    /// </summary>
    /// <value>The feed catalog; defaults to <see cref="EcbRateFeed.Default" />.</value>
    public IReadOnlyList<EcbRateFeed> Feeds { get; set; } = EcbRateFeed.Default;

    /// <summary>
    /// Gets or sets a value indicating whether downloaded feed files are persisted to an on-disk cache.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to enable the on-disk cache; otherwise <see langword="false" />. Defaults to
    /// <see langword="true" />.
    /// </value>
    public bool EnableDiskCache { get; set; } = true;

    /// <summary>
    /// Gets or sets the directory used by the on-disk cache.
    /// </summary>
    /// <value>
    /// The cache directory, or <see langword="null" /> to use a <c>bodu-ecb</c> folder under the system temporary path.
    /// </value>
    public string? CacheDirectory { get; set; }

    /// <summary>
    /// Resolves the absolute download URL for a feed by combining <see cref="WebRateProviderOptions.BaseAddress" />
    /// with the feed's relative file name.
    /// </summary>
    /// <param name="feed">The feed whose URL is required.</param>
    /// <returns>The absolute feed URL.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="feed" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the feed's file name is not a plain relative file name.
    /// </exception>
    internal Uri ResolveFeedUrl(EcbRateFeed feed)
    {
        ThrowHelper.ThrowIfNull(feed);
        if (!IsSafeRelativeFeedFileName(feed.FileName))
            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, EcbResourceStrings.Arg_Invalid_EcbFeedFileName, feed.FileName), nameof(feed));

        return new Uri(BaseAddress, feed.FileName);
    }

    /// <summary>
    /// Validates the ECB-specific options, ensuring the feed catalog is not empty.
    /// </summary>
    /// <param name="error">
    /// When this method returns <see langword="false" />, a message describing the violated invariant; otherwise
    /// <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when every ECB-specific invariant holds; otherwise <see langword="false" />.
    /// </returns>
    protected override bool TryValidateCore(out string? error)
    {
        if (Feeds is null || Feeds.Count == 0)
        {
            error = EcbResourceStrings.Arg_Invalid_EcbOptionsFeeds;
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Determines whether a feed file name is a plain relative name safe to resolve against
    /// <see cref="WebRateProviderOptions.BaseAddress" />.
    /// </summary>
    /// <param name="fileName">The feed file name to test.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="fileName" /> is a non-empty relative name with no rooted, scheme,
    /// or parent-directory component; otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// <see cref="EcbRateFeed" /> exposes a public constructor, so a feed's file name can be supplied by configuration.
    /// Resolving an absolute URL, a rooted path, a protocol-relative reference, or a <c>..</c> segment against the base
    /// address would retarget the request to a different host or path, so such names are rejected before the request
    /// URL is composed.
    /// </remarks>
    private static bool IsSafeRelativeFeedFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return false;

        // Absolute URLs (http://…, file:…) and Windows-style drive/scheme prefixes carry a colon.
        if (Uri.TryCreate(fileName, UriKind.Absolute, out _) || fileName.Contains(':', StringComparison.Ordinal))
            return false;

        // Rooted or protocol-relative ('/x', '//host', '\x') paths replace the base path or host.
        if (fileName[0] is '/' or '\\')
            return false;

        // A parent-directory reference walks out of the base path.
        return !fileName.Contains("..", StringComparison.Ordinal);
    }
}
