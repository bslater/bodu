// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbEndpointOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Configures the provider's connection to the European Central Bank's <c>eurofxref</c> endpoints: where the feed files
/// are published and how the HTTP requests that fetch them are shaped.
/// </summary>
/// <remarks>
/// <para>
/// This object isolates the network-facing settings from the behavioural options on
/// <see cref="EcbRateProviderOptions" />, so the host, transport timeout, and request identity can be pointed at a
/// mirror or proxy of the ECB feeds — or simply tuned — without touching caching, feed-selection, or alias
/// configuration. The absolute URL of a feed is composed from <see cref="BaseUrl" /> and the feed's relative
/// <see cref="EcbRateFeed.FileName" /> through <see cref="ResolveFeedUrl(EcbRateFeed)" />.
/// </para>
/// <para>
/// Every member carries a working default, so the object binds cleanly through <c>Microsoft.Extensions.Options</c> and
/// requires no configuration for the common case.
/// </para>
/// </remarks>
public sealed class EcbEndpointOptions
{
    /// <summary>
    /// Gets or sets the base URL under which the ECB publishes its <c>eurofxref</c> reference-rate files. Should end
    /// with a trailing slash so feed file names resolve as relative paths.
    /// </summary>
    /// <value>The base URL; defaults to the ECB <c>eurofxref</c> statistics path.</value>
    public Uri BaseUrl { get; set; } = new("https://www.ecb.europa.eu/stats/eurofxref/");

    /// <summary>
    /// Gets or sets the HTTP request timeout applied to feed downloads by the dependency-injection registration.
    /// </summary>
    /// <value>The HTTP timeout; defaults to 30 seconds.</value>
    public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the <c>User-Agent</c> header applied to feed downloads by the dependency-injection registration.
    /// </summary>
    /// <value>The user-agent string; defaults to a Bodu identifier.</value>
    public string UserAgent { get; set; } = "Bodu.Financial.ExchangeRates.Ecb";

    /// <summary>
    /// Resolves the absolute download URL for a feed by combining <see cref="BaseUrl" /> with the feed's relative file
    /// name.
    /// </summary>
    /// <param name="feed">The feed whose URL is required.</param>
    /// <returns>The absolute feed URL.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="feed" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="BaseUrl" /> has not been configured.
    /// </exception>
    public Uri ResolveFeedUrl(EcbRateFeed feed)
    {
        ThrowHelper.ThrowIfNull(feed);
        if (BaseUrl is null)
            throw new InvalidOperationException(EcbResourceStrings.Op_Invalid_EcbEndpointBaseUrl);
        if (!IsSafeRelativeFeedFileName(feed.FileName))
            throw new ArgumentException(
                string.Format(System.Globalization.CultureInfo.CurrentCulture, EcbResourceStrings.Arg_Invalid_EcbFeedFileName, feed.FileName),
                nameof(feed));

        return new Uri(BaseUrl, feed.FileName);
    }

    /// <summary>
    /// Determines whether a feed file name is a plain relative name safe to resolve against <see cref="BaseUrl" />.
    /// </summary>
    /// <param name="fileName">The feed file name to test.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="fileName" /> is a non-empty relative name with no rooted, scheme,
    /// or parent-directory component; otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// <see cref="EcbRateFeed" /> exposes a public constructor, so a feed's file name can be supplied by configuration.
    /// Resolving an absolute URL, a rooted path, a protocol-relative reference, or a <c>..</c> segment against
    /// <see cref="BaseUrl" /> would retarget the request to a different host or path, so such names are rejected before
    /// the request URL is composed.
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

    /// <summary>
    /// Validates the endpoint options, throwing when a required value is missing or an invariant is violated.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <see cref="BaseUrl" /> is <see langword="null" />, or <see cref="HttpTimeout" /> is not greater than
    /// zero.
    /// </exception>
    public void Validate()
    {
        if (!TryValidate(out string? error))
            throw new ArgumentException(error);
    }

    /// <summary>
    /// Attempts to validate the endpoint options without throwing, reporting the first invariant that is violated.
    /// </summary>
    /// <param name="error">
    /// When this method returns <see langword="false" />, a message describing the first violated invariant; otherwise
    /// <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when every invariant holds; otherwise <see langword="false" />.</returns>
    public bool TryValidate(out string? error)
    {
        if (BaseUrl is null)
        {
            error = EcbResourceStrings.Arg_Invalid_EcbOptionsBaseUrl;
            return false;
        }

        if (HttpTimeout <= TimeSpan.Zero)
        {
            error = EcbResourceStrings.Arg_Invalid_EcbEndpointHttpTimeout;
            return false;
        }

        error = null;
        return true;
    }
}
