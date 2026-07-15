// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WebRateProviderOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

using Microsoft.Extensions.Logging;

/// <summary>
/// Provides the configuration common to every pair-based web exchange-rate source: the endpoint base address, the HTTP
/// contract (user agent and timeout), the synchronous-access and look-back behaviour, the currency-alias map, and the
/// per-concern diagnostic log levels. A concrete source derives from this type to add its own endpoint members and
/// validation.
/// </summary>
/// <remarks>
/// <para>
/// Every shared member carries a working default so the options bind cleanly through
/// <c>Microsoft.Extensions.Options</c> and require no configuration for the common case. A derived type sets
/// <see cref="BaseAddress" /> in its constructor — the base leaves it unset because the host differs per source — and
/// overrides <see cref="TryValidateCore" /> to add its own invariants.
/// </para>
/// <para>
/// The <c>*LogLevel</c> members set the <see cref="LogLevel" /> at which each diagnostic the provider emits is logged,
/// so consumers can re-tune verbosity per concern without category-wide log filters. Set any of them to
/// <see cref="LogLevel.None" /> to suppress that event entirely.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.Financial.ExchangeRates;
///
/// // Concrete options types (YahooRateProviderOptions, OfxRateProviderOptions, ...) share this surface.
/// var options = new YahooRateProviderOptions
/// {
///     HttpTimeout = TimeSpan.FromSeconds(10),
///     AllowSynchronousNetworkAccess = false,     // force callers onto the asynchronous surface
///     DefaultLookback = TimeSpan.FromDays(14),   // window used by the timeless lookup surface
/// };
///
/// options.CurrencyAliases["CNH"] = "CNY";        // map a non-ISO source symbol onto its ISO code
///
/// if (!options.TryValidate(out string? error))
///     throw new InvalidOperationException(error);
///]]>
/// </code>
/// </example>
/// </remarks>
public abstract class WebRateProviderOptions
{
    /// <summary>
    /// Gets or sets the base address of the source's API host. Should end with a trailing slash so relative paths
    /// resolve as expected.
    /// </summary>
    /// <value>The base address; set by the derived source's constructor.</value>
    public Uri BaseAddress { get; set; } = null!;

    /// <summary>
    /// Gets or sets the HTTP request timeout applied to requests by the dependency-injection registration.
    /// </summary>
    /// <value>The HTTP timeout; defaults to 30 seconds.</value>
    public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the maximum number of response bytes a provider-owned <see cref="HttpClient" /> buffers, bounding
    /// the memory a single response can consume.
    /// </summary>
    /// <value>
    /// The response buffer cap, in bytes; defaults to
    /// <see cref="RateProviderHttpClientFactory.DefaultMaxResponseContentBufferSize" /> (64 MiB).
    /// </value>
    /// <remarks>
    /// This value is applied when the provider creates and owns its own <see cref="HttpClient" />. When a client is
    /// supplied to the provider directly, bounding its response size is the caller's responsibility and this value is
    /// not applied.
    /// </remarks>
    public long MaxResponseContentBufferSize { get; set; } =
        RateProviderHttpClientFactory.DefaultMaxResponseContentBufferSize;

    /// <summary>
    /// Gets or sets the <c>User-Agent</c> header applied to the HTTP client.
    /// </summary>
    /// <value>
    /// The user-agent string; defaults to a browser-like identifier, because the public exchange-rate endpoints these
    /// options address commonly reject requests that do not present a recognizable user agent.
    /// </value>
    /// <remarks>
    /// This value is applied when the provider creates and owns its own <see cref="HttpClient" /> and by the
    /// dependency-injection registration when it configures the named client. When an <see cref="HttpClient" /> is
    /// supplied to the provider directly, configuring its headers is the caller's responsibility and this value is not
    /// applied.
    /// </remarks>
    public string UserAgent { get; set; } =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

    /// <summary>
    /// Gets or sets a value indicating whether a synchronous lookup may block to fetch a missing pair on demand.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to allow synchronous, blocking fetches from <see cref="IDatedRateProvider" /> lookups;
    /// <see langword="false" /> to serve only already-loaded data. Defaults to <see langword="false" />.
    /// </value>
    /// <remarks>
    /// Blocking on network I/O from a synchronous method can deadlock in environments with a single-threaded
    /// synchronization context (classic ASP.NET, WPF, WinForms), so the default is snapshot-only. Leave this
    /// <see langword="false" /> and warm the store at startup; set it to <see langword="true" /> only to opt in to a
    /// blocking on-demand fetch from the synchronous lookup path.
    /// </remarks>
    public bool AllowSynchronousNetworkAccess { get; set; }

    /// <summary>
    /// Gets or sets the look-back window used when a synchronous or undated lookup must fetch a pair on demand. The
    /// provider fetches the window ending on the requested date and spanning this duration.
    /// </summary>
    /// <value>The look-back window; defaults to 7 days.</value>
    public TimeSpan DefaultLookback { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Gets or sets the history depth the provider advertises: how far back it can serve rates. A caller can consult it
    /// before requesting an old date.
    /// </summary>
    /// <value>
    /// The advertised availability; defaults to <see cref="RateHistoryAvailability.Unbounded" />. A source whose feed
    /// publishes only the recent past sets a rolling window in its constructor.
    /// </value>
    public RateHistoryAvailability HistoryAvailability { get; set; } = RateHistoryAvailability.Unbounded;

    /// <summary>
    /// Gets or sets the map from ISO 4217 codes to the symbol component the source uses, applied while building a
    /// request. Codes absent from the map pass through unchanged.
    /// </summary>
    /// <value>The alias map; defaults to an empty map.</value>
    public IDictionary<string, string> CurrencyAliases { get; set; } =
        new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets the level at which the start of a pair download is logged.
    /// </summary>
    /// <value>The log level; defaults to <see cref="LogLevel.Debug" />.</value>
    public LogLevel DownloadStartingLogLevel { get; set; } = LogLevel.Debug;

    /// <summary>
    /// Gets or sets the level at which a completed pair download (with its observation count) is logged.
    /// </summary>
    /// <value>The log level; defaults to <see cref="LogLevel.Information" />.</value>
    public LogLevel DownloadCompletedLogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Gets or sets the level at which a failed pair download is logged.
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
    /// Validates the options, throwing when a required value is missing or an invariant is violated.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when an invariant is violated.</exception>
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
    /// The shared invariants are checked first, then <see cref="TryValidateCore" /> is consulted for source-specific
    /// invariants. The throwing <see cref="Validate" /> method is expressed in terms of this method, and the
    /// dependency-injection registration wires it into <c>ValidateOnStart</c> so misconfiguration fails fast at
    /// application startup.
    /// </remarks>
    public bool TryValidate(out string? error)
    {
        if (BaseAddress is null)
        {
            error = ExchangeRatesResourceStrings.Arg_Invalid_WebExchangeRateOptionsBaseAddress;
            return false;
        }

        if (HttpTimeout <= TimeSpan.Zero)
        {
            error = ExchangeRatesResourceStrings.Arg_Invalid_WebExchangeRateOptionsHttpTimeout;
            return false;
        }

        if (DefaultLookback <= TimeSpan.Zero)
        {
            error = ExchangeRatesResourceStrings.Arg_Invalid_WebExchangeRateOptionsDefaultLookback;
            return false;
        }

        if (MaxResponseContentBufferSize <= 0)
        {
            error = ExchangeRatesResourceStrings.Arg_Invalid_WebExchangeRateOptionsMaxResponseContentBufferSize;
            return false;
        }

        if (CurrencyAliases is null)
        {
            error = ExchangeRatesResourceStrings.Arg_Invalid_WebExchangeRateOptionsCurrencyAliases;
            return false;
        }

        foreach (KeyValuePair<string, string> alias in CurrencyAliases)
        {
            if (!IsUrlSafeAliasValue(alias.Value))
            {
                error = string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    ExchangeRatesResourceStrings.Arg_Invalid_WebExchangeRateOptionsCurrencyAliasValue,
                    alias.Key);
                return false;
            }
        }

        if (!AreLogLevelsDefined())
        {
            error = ExchangeRatesResourceStrings.Arg_Invalid_WebExchangeRateOptionsLogLevel;
            return false;
        }

        return TryValidateCore(out error);
    }

    /// <summary>
    /// Validates the source-specific invariants. The default implementation reports success; a derived type overrides
    /// it to validate its own members and is invoked only after the shared invariants hold.
    /// </summary>
    /// <param name="error">
    /// When this method returns <see langword="false" />, a message describing the violated invariant; otherwise
    /// <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when every source-specific invariant holds; otherwise <see langword="false" />.
    /// </returns>
    protected virtual bool TryValidateCore(out string? error)
    {
        error = null;
        return true;
    }

    /// <summary>
    /// Maps an ISO code through <see cref="CurrencyAliases" />, returning the code unchanged when no alias exists.
    /// </summary>
    /// <param name="isoCode">The ISO code to map.</param>
    /// <returns>The aliased symbol component, or <paramref name="isoCode" /> when unmapped.</returns>
    protected string MapCurrency(string isoCode) =>
        CurrencyAliases.TryGetValue(isoCode, out string? alias) ? alias : isoCode;

    /// <summary>
    /// Determines whether a currency alias value is safe to substitute into a request URL.
    /// </summary>
    /// <param name="value">The alias value to test.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="value" /> is a non-empty run of ASCII letters and digits; otherwise
    /// <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Alias values are substituted verbatim into a source's request path. Constraining them to alphanumerics keeps
    /// them URL-safe — a value containing a path or query delimiter (<c>/</c>, <c>?</c>, <c>#</c>, <c>\</c>, <c>%</c>)
    /// or a parent-directory reference cannot inject an extra path or query segment into the request. Source symbol
    /// components (for example <c>USD</c> or <c>BTC</c>) are already alphanumeric.
    /// </remarks>
    private static bool IsUrlSafeAliasValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        foreach (char c in value)
        {
            if (!char.IsAsciiLetterOrDigit(c))
                return false;
        }

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
        && Enum.IsDefined(ObservationIngestedLogLevel)
        && Enum.IsDefined(SynchronousNetworkFetchLogLevel);
}
