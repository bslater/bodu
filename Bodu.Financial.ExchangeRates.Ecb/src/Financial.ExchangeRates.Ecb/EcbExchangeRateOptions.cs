// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbExchangeRateOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Ecb;

using Microsoft.Extensions.Logging;

/// <summary>
/// Configures how the <see cref="EcbExchangeRateProvider" /> downloads, caches, and interprets ECB euro reference-rate
/// data.
/// </summary>
/// <remarks>
/// Every member carries a working default, so the options bind cleanly through <c>Microsoft.Extensions.Options</c> and
/// require no configuration for the common case. The dependency-injection package binds this type from configuration
/// and a <c>configure</c> delegate.
/// </remarks>
public sealed class EcbExchangeRateOptions
{
    /// <summary>
    /// Gets or sets the endpoint options describing the provider's connection to the ECB <c>eurofxref</c> endpoints —
    /// the base URL, transport timeout, and request identity.
    /// </summary>
    /// <value>The endpoint options; defaults to a new <see cref="EcbEndpointOptions" /> targeting the ECB.</value>
    public EcbEndpointOptions Endpoint { get; set; } = new();

    /// <summary>
    /// Gets or sets the catalogue of feeds to draw from, ordered from the narrowest look-back to the widest.
    /// </summary>
    /// <value>The feed catalogue; defaults to <see cref="EcbExchangeRateFeed.Default" />.</value>
    public IReadOnlyList<EcbExchangeRateFeed> Feeds { get; set; } = EcbExchangeRateFeed.Default;

    /// <summary>
    /// Gets or sets a value indicating whether a synchronous lookup may block to download a missing feed on demand.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to allow synchronous, blocking downloads from <see cref="IDatedExchangeRateProvider" />
    /// lookups; <see langword="false" /> to serve only already-loaded data. Defaults to <see langword="true" />.
    /// </value>
    /// <remarks>
    /// Blocking on network I/O from a synchronous method can deadlock in environments with a single-threaded
    /// synchronization context (classic ASP.NET, WPF, WinForms). In those environments, set this to
    /// <see langword="false" /> and warm the cache with <see cref="EcbExchangeRateProvider.PreloadAsync" /> or
    /// <see cref="EcbExchangeRateProvider.LoadRangeAsync" /> at startup.
    /// </remarks>
    public bool AllowSynchronousNetworkAccess { get; set; } = true;

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
    /// Gets or sets how long a cached copy of a feed remains fresh before it is re-downloaded.
    /// </summary>
    /// <value>The refresh interval; defaults to 12 hours.</value>
    /// <remarks>
    /// Every ECB feed extends to the most recent business day, so all feeds are treated as refreshable rather than
    /// immutable: a cached feed older than this interval is re-downloaded on the next load.
    /// </remarks>
    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromHours(12);

    /// <summary>
    /// Gets or sets the map from ECB currency labels to ISO 4217 codes, applied while normalizing a quoted currency.
    /// </summary>
    /// <value>The alias map; defaults to an empty map because the ECB publishes ISO 4217 codes directly.</value>
    public IDictionary<string, string> CurrencyAliases { get; set; } =
        new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets the level at which the start of a feed download is logged.
    /// </summary>
    /// <value>The log level; defaults to <see cref="LogLevel.Debug" />.</value>
    public LogLevel DownloadStartingLogLevel { get; set; } = LogLevel.Debug;

    /// <summary>
    /// Gets or sets the level at which a completed feed download (with its observation count) is logged.
    /// </summary>
    /// <value>The log level; defaults to <see cref="LogLevel.Information" />.</value>
    public LogLevel DownloadCompletedLogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Gets or sets the level at which a failed feed download is logged.
    /// </summary>
    /// <value>The log level; defaults to <see cref="LogLevel.Warning" />.</value>
    public LogLevel DownloadFailedLogLevel { get; set; } = LogLevel.Warning;

    /// <summary>
    /// Gets or sets the level at which each individual ingested rate observation is logged.
    /// </summary>
    /// <value>The log level; defaults to <see cref="LogLevel.Trace" />.</value>
    public LogLevel ObservationIngestedLogLevel { get; set; } = LogLevel.Trace;

    /// <summary>
    /// Gets or sets the level at which a synchronous, blocking on-demand network fetch is logged.
    /// </summary>
    /// <value>The log level; defaults to <see cref="LogLevel.Warning" />.</value>
    public LogLevel SynchronousNetworkFetchLogLevel { get; set; } = LogLevel.Warning;

    /// <summary>
    /// Validates the options, throwing when a required value is missing.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <see cref="Endpoint" /> is <see langword="null" /> or fails validation, or when <see cref="Feeds" />
    /// is <see langword="null" /> or empty.
    /// </exception>
    public void Validate()
    {
        if (Endpoint is null)
            throw new ArgumentException(EcbResourceStrings.Arg_Invalid_EcbOptionsEndpoint, nameof(Endpoint));

        Endpoint.Validate();

        if (Feeds is null || Feeds.Count == 0)
            throw new ArgumentException(EcbResourceStrings.Arg_Invalid_EcbOptionsFeeds, nameof(Feeds));
    }
}
