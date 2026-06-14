// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YahooExchangeRateOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Yahoo;

/// <summary>
/// Configures how the <see cref="YahooExchangeRateProvider" /> addresses and interprets the Yahoo Finance chart REST
/// service.
/// </summary>
/// <remarks>
/// <para>
/// Every member carries a working default, so the options bind cleanly through <c>Microsoft.Extensions.Options</c> and
/// require no configuration for the common case. The dependency-injection package binds this type from configuration
/// and a <c>configure</c> delegate.
/// </para>
/// <para>
/// The options focus on <em>endpoint configuration</em>: the <see cref="BaseAddress" />, the <see cref="ChartPath" />
/// template, and the <see cref="SymbolFormat" /> used to build the foreign-exchange ticker. The chart bar interval is
/// fixed at one day, and the date range is supplied per call through the provider's lookup and range methods.
/// </para>
/// </remarks>
public sealed class YahooExchangeRateOptions
{
    /// <summary>
    /// The placeholder token replaced by the ticker symbol when building a request path from <see cref="ChartPath" />.
    /// </summary>
    internal const string SymbolPlaceholder = "{symbol}";

    /// <summary>
    /// The placeholder token replaced by the source-currency code when building a ticker from
    /// <see cref="SymbolFormat" />.
    /// </summary>
    internal const string FromPlaceholder = "{from}";

    /// <summary>
    /// The placeholder token replaced by the destination-currency code when building a ticker from
    /// <see cref="SymbolFormat" />.
    /// </summary>
    internal const string ToPlaceholder = "{to}";

    /// <summary>
    /// Gets or sets the base address of the Yahoo Finance API host. Should end with a trailing slash so the chart path
    /// resolves as a relative path.
    /// </summary>
    /// <value>The base address; defaults to <c>https://query1.finance.yahoo.com/</c>.</value>
    public Uri BaseAddress { get; set; } = new("https://query1.finance.yahoo.com/");

    /// <summary>
    /// Gets or sets the relative chart-endpoint path template. The <c>{symbol}</c> placeholder is replaced by the
    /// foreign-exchange ticker before the request is issued.
    /// </summary>
    /// <value>The chart path template; defaults to <c>v8/finance/chart/{symbol}</c>.</value>
    public string ChartPath { get; set; } = "v8/finance/chart/{symbol}";

    /// <summary>
    /// Gets or sets the template used to build a Yahoo foreign-exchange ticker from a currency pair. The <c>{from}</c>
    /// and <c>{to}</c> placeholders are replaced by the source and destination currency codes.
    /// </summary>
    /// <value>The ticker template; defaults to <c>{from}{to}=X</c> (for example, <c>AUDUSD=X</c>).</value>
    public string SymbolFormat { get; set; } = "{from}{to}=X";

    /// <summary>
    /// Gets or sets the HTTP request timeout applied to chart requests by the dependency-injection registration.
    /// </summary>
    /// <value>The HTTP timeout; defaults to 30 seconds.</value>
    public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the <c>User-Agent</c> header applied to chart requests by the dependency-injection registration.
    /// </summary>
    /// <value>
    /// The user-agent string; defaults to a browser-like identifier, because the Yahoo Finance endpoint rejects
    /// requests that do not present a recognizable user agent.
    /// </value>
    public string UserAgent { get; set; } =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

    /// <summary>
    /// Gets or sets a value indicating whether a synchronous lookup may block to fetch a missing pair on demand.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to allow synchronous, blocking fetches from <see cref="IDatedExchangeRateProvider" />
    /// lookups; <see langword="false" /> to serve only already-loaded data. Defaults to <see langword="true" />.
    /// </value>
    /// <remarks>
    /// Blocking on network I/O from a synchronous method can deadlock in environments with a single-threaded
    /// synchronization context (classic ASP.NET, WPF, WinForms). In those environments, set this to
    /// <see langword="false" /> and warm the cache with <see cref="YahooExchangeRateProvider.LoadPairAsync" /> at
    /// startup.
    /// </remarks>
    public bool AllowSynchronousNetworkAccess { get; set; } = true;

    /// <summary>
    /// Gets or sets the look-back window used when a synchronous or undated lookup must fetch a pair on demand. The
    /// provider fetches the window ending on the requested date and spanning this duration.
    /// </summary>
    /// <value>The look-back window; defaults to 7 days.</value>
    public TimeSpan DefaultLookback { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Gets or sets the map from ISO 4217 codes to the symbol component Yahoo Finance uses, applied while building a
    /// ticker. Codes absent from the map pass through unchanged.
    /// </summary>
    /// <value>The alias map; defaults to an empty map.</value>
    public IDictionary<string, string> CurrencyAliases { get; set; } =
        new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// Validates the options, throwing when a required value is missing or a template lacks its placeholder.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <see cref="BaseAddress" /> is <see langword="null" />; when <see cref="ChartPath" /> is empty or
    /// omits the <c>{symbol}</c> placeholder; or when <see cref="SymbolFormat" /> is empty or omits the <c>{from}</c>
    /// or <c>{to}</c> placeholder.
    /// </exception>
    public void Validate()
    {
        if (BaseAddress is null)
            throw new ArgumentException(YahooResourceStrings.Arg_Invalid_YahooOptionsBaseAddress, nameof(BaseAddress));

        if (string.IsNullOrWhiteSpace(ChartPath) || !ChartPath.Contains(SymbolPlaceholder, StringComparison.Ordinal))
            throw new ArgumentException(YahooResourceStrings.Arg_Invalid_YahooOptionsChartPath, nameof(ChartPath));

        if (string.IsNullOrWhiteSpace(SymbolFormat)
            || !SymbolFormat.Contains(FromPlaceholder, StringComparison.Ordinal)
            || !SymbolFormat.Contains(ToPlaceholder, StringComparison.Ordinal))
        {
            throw new ArgumentException(YahooResourceStrings.Arg_Invalid_YahooOptionsSymbolFormat, nameof(SymbolFormat));
        }
    }

    /// <summary>
    /// Builds the Yahoo Finance ticker for a currency pair from <see cref="SymbolFormat" />, applying any configured
    /// currency aliases.
    /// </summary>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <returns>The ticker symbol, for example <c>AUDUSD=X</c>.</returns>
    internal string BuildSymbol(string fromIsoCode, string toIsoCode) =>
        SymbolFormat
            .Replace(FromPlaceholder, MapCurrency(fromIsoCode), StringComparison.Ordinal)
            .Replace(ToPlaceholder, MapCurrency(toIsoCode), StringComparison.Ordinal);

    /// <summary>
    /// Maps an ISO code through <see cref="CurrencyAliases" />, returning the code unchanged when no alias exists.
    /// </summary>
    /// <param name="isoCode">The ISO code to map.</param>
    /// <returns>The aliased symbol component, or <paramref name="isoCode" /> when unmapped.</returns>
    private string MapCurrency(string isoCode) =>
        CurrencyAliases.TryGetValue(isoCode, out var alias) ? alias : isoCode;
}
