// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbEndpointOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Ecb;

/// <summary>
/// Configures the provider's connection to the European Central Bank's <c>eurofxref</c> endpoints: where the feed files
/// are published and how the HTTP requests that fetch them are shaped.
/// </summary>
/// <remarks>
/// <para>
/// This object isolates the network-facing settings from the behavioural options on
/// <see cref="EcbExchangeRateOptions" />, so the host, transport timeout, and request identity can be pointed at a
/// mirror or proxy of the ECB feeds — or simply tuned — without touching caching, feed-selection, or alias
/// configuration. The absolute URL of a feed is composed from <see cref="BaseUrl" /> and the feed's relative
/// <see cref="EcbExchangeRateFeed.FileName" /> through <see cref="ResolveFeedUrl(EcbExchangeRateFeed)" />.
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
    public Uri ResolveFeedUrl(EcbExchangeRateFeed feed)
    {
        ThrowHelper.ThrowIfNull(feed);
        if (BaseUrl is null)
            throw new InvalidOperationException(EcbResourceStrings.Op_Invalid_EcbEndpointBaseUrl);

        return new Uri(BaseUrl, feed.FileName);
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
        if (!TryValidate(out var error))
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
