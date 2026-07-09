// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaExchangeRateOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Configures how the <see cref="RbaExchangeRateProvider" /> downloads, caches, and interprets RBA exchange-rate data.
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
public sealed class RbaExchangeRateOptions
{
    /// <summary>
    /// Gets or sets the base URL under which the RBA publishes its historical exchange-rate files. Should end with a
    /// trailing slash so era file names resolve as relative paths.
    /// </summary>
    /// <value>The base URL; defaults to the RBA historical statistics path.</value>
    public Uri BaseUrl { get; set; } = new("https://www.rba.gov.au/statistics/tables/xls-hist/");

    /// <summary>
    /// Gets or sets the catalogue of era files to draw from.
    /// </summary>
    /// <value>The era catalogue; defaults to <see cref="RbaEra.Default" />.</value>
    public IReadOnlyList<RbaEra> Eras { get; set; } = RbaEra.Default;

    /// <summary>
    /// Gets or sets the HTTP request timeout applied to era downloads by the dependency-injection registration.
    /// </summary>
    /// <value>The HTTP timeout; defaults to 30 seconds.</value>
    public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the <c>User-Agent</c> header applied to era downloads by the dependency-injection registration.
    /// </summary>
    /// <value>The user-agent string; defaults to a Bodu identifier.</value>
    public string UserAgent { get; set; } = "Bodu.Financial.ExchangeRates.Rba";

    /// <summary>
    /// Gets or sets a value indicating whether a synchronous lookup may block to download a missing era on demand.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to allow synchronous, blocking downloads from <see cref="IDatedRateProvider" />
    /// lookups; <see langword="false" /> to serve only already-loaded data. Defaults to <see langword="false" />, so
    /// the provider serves a snapshot of already-loaded data and a synchronous miss does not reach the network.
    /// </value>
    /// <remarks>
    /// Blocking on network I/O from a synchronous method can deadlock in environments with a single-threaded
    /// synchronization context (classic ASP.NET, WPF, WinForms), so the default is snapshot-only. Leave this
    /// <see langword="false" /> and warm the store with <see cref="RbaExchangeRateProvider.PreloadAsync" /> or
    /// <see cref="RbaExchangeRateProvider.LoadRangeAsync" /> at startup; set it to <see langword="true" /> only to opt
    /// in to a blocking on-demand fetch from the synchronous lookup path.
    /// </remarks>
    public bool AllowSynchronousNetworkAccess { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether downloaded era files are persisted to an on-disk cache.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to enable the on-disk cache; otherwise <see langword="false" />. Defaults to
    /// <see langword="false" />.
    /// </value>
    public bool EnableDiskCache { get; set; } = false;

    /// <summary>
    /// Gets or sets the directory used by the on-disk cache.
    /// </summary>
    /// <value>
    /// The cache directory, or <see langword="null" /> to use a <c>bodu-rba</c> folder under the system temporary path.
    /// </value>
    public string? CacheDirectory { get; set; }

    /// <summary>
    /// Gets or sets how long a cached copy of the open-ended current era remains fresh before it is re-downloaded.
    /// </summary>
    /// <value>The refresh interval; defaults to 12 hours. Immutable historical eras are cached indefinitely.</value>
    public TimeSpan CurrentEraRefreshInterval { get; set; } = TimeSpan.FromHours(12);

    /// <summary>
    /// Gets or sets the map from RBA units labels to ISO 4217 codes, applied while resolving a column's currency.
    /// </summary>
    /// <value>The alias map; defaults to mapping <c>SDR</c> to the ISO code <c>XDR</c>.</value>
    public IDictionary<string, string> CurrencyAliases { get; set; } =
        new Dictionary<string, string>(StringComparer.Ordinal) { ["SDR"] = "XDR" };

    /// <summary>
    /// Gets or sets the level at which the start of an era download is logged.
    /// </summary>
    /// <value>The log level; defaults to <see cref="LogLevel.Debug" />.</value>
    public LogLevel DownloadStartingLogLevel { get; set; } = LogLevel.Debug;

    /// <summary>
    /// Gets or sets the level at which a completed era download (with its observation count) is logged.
    /// </summary>
    /// <value>The log level; defaults to <see cref="LogLevel.Information" />.</value>
    public LogLevel DownloadCompletedLogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Gets or sets the level at which a failed era download is logged.
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
    /// Thrown when <see cref="BaseUrl" /> is <see langword="null" />; <see cref="Eras" /> is <see langword="null" /> or
    /// empty; <see cref="HttpTimeout" /> or <see cref="CurrentEraRefreshInterval" /> is not greater than zero;
    /// <see cref="CurrencyAliases" /> is <see langword="null" />; or any <c>*LogLevel</c> is not a defined
    /// <see cref="LogLevel" />.
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
        if (BaseUrl is null)
        {
            error = RbaResourceStrings.Arg_Invalid_RbaOptionsBaseUrl;
            return false;
        }

        if (Eras is null || Eras.Count == 0)
        {
            error = RbaResourceStrings.Arg_Invalid_RbaOptionsEras;
            return false;
        }

        if (HttpTimeout <= TimeSpan.Zero)
        {
            error = RbaResourceStrings.Arg_Invalid_RbaOptionsHttpTimeout;
            return false;
        }

        if (CurrentEraRefreshInterval <= TimeSpan.Zero)
        {
            error = RbaResourceStrings.Arg_Invalid_RbaOptionsCurrentEraRefreshInterval;
            return false;
        }

        if (CurrencyAliases is null)
        {
            error = RbaResourceStrings.Arg_Invalid_RbaOptionsCurrencyAliases;
            return false;
        }

        if (!AreLogLevelsDefined())
        {
            error = RbaResourceStrings.Arg_Invalid_RbaOptionsLogLevel;
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
