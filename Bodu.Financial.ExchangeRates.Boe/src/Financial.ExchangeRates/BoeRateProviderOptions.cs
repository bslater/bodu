// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeRateProviderOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Configures how the <see cref="BoeRateProvider" /> downloads, caches, and interprets Bank of England daily
/// spot exchange-rate data.
/// </summary>
/// <remarks>
/// <para>
/// Every member carries a working default, so the options bind cleanly through <c>Microsoft.Extensions.Options</c> and
/// require no configuration for the common case. The dependency-injection package binds this type from configuration
/// and a <c>configure</c> delegate.
/// </para>
/// <para>
/// The <c>*LogLevel</c> members set the <see cref="LogLevel" /> at which each diagnostic the provider emits is logged,
/// so consumers can re-tune verbosity per concern without category-wide log filters. Set any of them to
/// <see cref="LogLevel.None" /> to suppress that event entirely.
/// </para>
/// </remarks>
public sealed class BoeRateProviderOptions
{
    /// <summary>The inception of the Bank of England's daily spot exchange-rate series: 2 January 1975, the first observation of the longest-running IADB <c>XUDL*</c> series.</summary>
    internal static readonly DateOnly DailySpotSeriesEpoch = new(1975, 1, 2);

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
    /// <see langword="true" /> to allow synchronous, blocking downloads from <see cref="IDatedRateProvider" />
    /// lookups; <see langword="false" /> to serve only already-loaded data. Defaults to <see langword="false" />, so
    /// the provider serves a snapshot of already-loaded data and a synchronous miss does not reach the network.
    /// </value>
    /// <remarks>
    /// Blocking on network I/O from a synchronous method can deadlock in environments with a single-threaded
    /// synchronization context (classic ASP.NET, WPF, WinForms), so the default is snapshot-only. Leave this
    /// <see langword="false" /> and warm the store with <see cref="BoeRateProvider.LoadRangeAsync" /> at
    /// startup; set it to <see langword="true" /> only to opt in to a blocking on-demand fetch from the synchronous
    /// lookup path.
    /// </remarks>
    public bool AllowSynchronousNetworkAccess { get; set; }

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
    /// Gets or sets the advertised history availability for the configured series.
    /// </summary>
    /// <value>
    /// The advertised availability; defaults to a fixed floor of 2 January 1975, the inception of the Bank of England's
    /// daily spot exchange-rate series.
    /// </value>
    /// <remarks>
    /// The IADB daily spot series (<c>XUDL*</c>) begin on 2 January 1975 for the longest-running currencies; some
    /// series start later (the euro series begins 4 January 1999, with the launch of the euro). The value is advisory —
    /// it bounds the earliest date worth requesting for the configured <see cref="Series" /> catalogue, not a
    /// per-series guarantee — so narrow it when the catalogue is restricted to later-inception series.
    /// </remarks>
    public RateHistoryAvailability HistoryAvailability { get; set; } =
        RateHistoryAvailability.Since(DailySpotSeriesEpoch);

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
    /// Validates the options, throwing when a required value is missing or an invariant is violated.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <see cref="Endpoint" /> is <see langword="null" /> or fails validation; <see cref="Series" /> is
    /// <see langword="null" /> or empty; <see cref="OnDemandWindowDays" /> is negative; <see cref="RefreshInterval" />
    /// is not greater than zero; or any <c>*LogLevel</c> is not a defined <see cref="LogLevel" />.
    /// </exception>
    public void Validate()
    {
        if (!TryValidate(out string? error))
            throw new ArgumentException(error);
    }

    /// <summary>
    /// Attempts to validate the options without throwing, reporting the first invariant that is violated.
    /// </summary>
    /// <param name="error">
    /// When this method returns <see langword="false" />, a message describing the first violated invariant; otherwise
    /// <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when every invariant holds; otherwise <see langword="false" />.</returns>
    /// <remarks>
    /// The throwing <see cref="Validate" /> method is expressed in terms of this method, and the dependency-injection
    /// registration wires it into <c>ValidateOnStart</c> so misconfiguration fails fast at application startup.
    /// </remarks>
    public bool TryValidate(out string? error)
    {
        if (Endpoint is null)
        {
            error = BoeResourceStrings.Arg_Invalid_BoeOptionsEndpoint;
            return false;
        }

        if (!Endpoint.TryValidate(out error))
            return false;

        if (Series is null || Series.Count == 0)
        {
            error = BoeResourceStrings.Arg_Invalid_BoeOptionsSeries;
            return false;
        }

        if (OnDemandWindowDays < 0)
        {
            error = BoeResourceStrings.Arg_Invalid_BoeOptionsOnDemandWindowDays;
            return false;
        }

        if (RefreshInterval <= TimeSpan.Zero)
        {
            error = BoeResourceStrings.Arg_Invalid_BoeOptionsRefreshInterval;
            return false;
        }

        if (!AreLogLevelsDefined())
        {
            error = BoeResourceStrings.Arg_Invalid_BoeOptionsLogLevel;
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Reports whether every configurable log level is a defined <see cref="LogLevel" /> value.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when every <c>*LogLevel</c> property is defined; otherwise <see langword="false" />.
    /// </returns>
    private bool AreLogLevelsDefined() =>
        Enum.IsDefined(DownloadStartingLogLevel)
        && Enum.IsDefined(DownloadCompletedLogLevel)
        && Enum.IsDefined(DownloadFailedLogLevel)
        && Enum.IsDefined(ObservationIngestedLogLevel);
}
