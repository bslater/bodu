// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeExchangeRateOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Boe;

using Microsoft.Extensions.Logging;

/// <summary>
/// Configures how the <see cref="BoeExchangeRateProvider" /> downloads, caches, and interprets Bank of England daily
/// spot exchange-rate data.
/// </summary>
/// <remarks>
/// Every member carries a working default, so the options bind cleanly through <c>Microsoft.Extensions.Options</c> and
/// require no configuration for the common case. The dependency-injection package binds this type from configuration
/// and a <c>configure</c> delegate.
/// </remarks>
public sealed class BoeExchangeRateOptions
{
    /// <summary>
    /// Gets or sets the endpoint options describing the provider's connection to the Bank of England IADB — the base
    /// URL, query path, transport timeout, and request identity.
    /// </summary>
    /// <value>The endpoint options; defaults to a new <see cref="BoeEndpointOptions" /> targeting the IADB.</value>
    public BoeEndpointOptions Endpoint { get; set; } = new();

    /// <summary>
    /// Gets or sets the catalogue of currency series to request, mapping each quote currency to its IADB series code.
    /// </summary>
    /// <value>The series catalogue; defaults to <see cref="BoeSeries.Default" />.</value>
    public IReadOnlyList<BoeSeries> Series { get; set; } = BoeSeries.Default;

    /// <summary>
    /// Gets or sets a value indicating whether a synchronous lookup may block to download a missing range on demand.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to allow synchronous, blocking downloads from <see cref="IDatedExchangeRateProvider" />
    /// lookups; <see langword="false" /> to serve only already-loaded data. Defaults to <see langword="true" />.
    /// </value>
    /// <remarks>
    /// Blocking on network I/O from a synchronous method can deadlock in environments with a single-threaded
    /// synchronization context (classic ASP.NET, WPF, WinForms). In those environments, set this to
    /// <see langword="false" /> and warm the cache with <see cref="BoeExchangeRateProvider.LoadRangeAsync" /> at
    /// startup.
    /// </remarks>
    public bool AllowSynchronousNetworkAccess { get; set; } = true;

    /// <summary>
    /// Gets or sets the number of days on each side of a requested date that an on-demand load fetches.
    /// </summary>
    /// <value>The on-demand window radius in days; defaults to 10.</value>
    /// <remarks>
    /// A synchronous lookup that misses loads the inclusive range from the requested date minus this many days to the
    /// requested date plus this many days (clamped to the current date), so a date-resolution tolerance can still find
    /// a neighbouring business day without downloading the entire history.
    /// </remarks>
    public int OnDemandWindowDays { get; set; } = 10;

    /// <summary>
    /// Gets or sets a value indicating whether downloaded ranges are persisted to an on-disk cache.
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
    /// The cache directory, or <see langword="null" /> to use a <c>bodu-boe</c> folder under the system temporary path.
    /// </value>
    public string? CacheDirectory { get; set; }

    /// <summary>
    /// Gets or sets how long a cached range response remains fresh before it is re-downloaded.
    /// </summary>
    /// <value>The refresh interval; defaults to 12 hours.</value>
    /// <remarks>
    /// A range that ends on or near the current date can gain a new observation each business day, so cached responses
    /// are treated as refreshable rather than immutable.
    /// </remarks>
    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromHours(12);

    /// <summary>
    /// Gets or sets the level at which the start of a range download is logged.
    /// </summary>
    /// <value>The log level; defaults to <see cref="LogLevel.Debug" />.</value>
    public LogLevel DownloadStartingLogLevel { get; set; } = LogLevel.Debug;

    /// <summary>
    /// Gets or sets the level at which a completed range download (with its observation count) is logged.
    /// </summary>
    /// <value>The log level; defaults to <see cref="LogLevel.Information" />.</value>
    public LogLevel DownloadCompletedLogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Gets or sets the level at which a failed range download is logged.
    /// </summary>
    /// <value>The log level; defaults to <see cref="LogLevel.Warning" />.</value>
    public LogLevel DownloadFailedLogLevel { get; set; } = LogLevel.Warning;

    /// <summary>
    /// Gets or sets the level at which each individual ingested rate observation is logged.
    /// </summary>
    /// <value>The log level; defaults to <see cref="LogLevel.Trace" />.</value>
    public LogLevel ObservationIngestedLogLevel { get; set; } = LogLevel.Trace;

    /// <summary>
    /// Validates the options, throwing when a required value is missing.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <see cref="Endpoint" /> is <see langword="null" /> or fails validation, or when
    /// <see cref="Series" /> is <see langword="null" /> or empty.
    /// </exception>
    public void Validate()
    {
        if (Endpoint is null)
            throw new ArgumentException(BoeResourceStrings.Arg_Invalid_BoeOptionsEndpoint, nameof(Endpoint));

        Endpoint.Validate();

        if (Series is null || Series.Count == 0)
            throw new ArgumentException(BoeResourceStrings.Arg_Invalid_BoeOptionsSeries, nameof(Series));
    }
}
