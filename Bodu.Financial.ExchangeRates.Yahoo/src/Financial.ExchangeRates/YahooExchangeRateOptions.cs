// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YahooExchangeRateOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Configures how the <see cref="YahooExchangeRateProvider" /> addresses and interprets the Yahoo Finance chart REST
/// service.
/// </summary>
/// <remarks>
/// <para>
/// This type derives from <see cref="WebExchangeRateProviderOptions" />, which supplies the endpoint base address, the
/// HTTP contract, the synchronous-access and look-back behaviour, the currency-alias map, and the per-concern log
/// levels. The members declared here are Yahoo-specific: the <see cref="ChartPath" /> template and the
/// <see cref="SymbolFormat" /> used to build the foreign-exchange ticker. The chart bar interval is fixed at one day,
/// and the date range is supplied per call through the provider's lookup and range methods.
/// </para>
/// <para>
/// Every member carries a working default, so the options bind cleanly through <c>Microsoft.Extensions.Options</c> and
/// require no configuration for the common case. The dependency-injection package binds this type from configuration
/// and a <c>configure</c> delegate.
/// </para>
/// </remarks>
public sealed class YahooExchangeRateOptions
    : WebExchangeRateProviderOptions
{
    /// <summary>The placeholder token replaced by the ticker symbol when building a request path from <see cref="ChartPath" />.</summary>
    internal const string SymbolPlaceholder = "{symbol}";

    /// <summary>The placeholder token replaced by the source-currency code when building a ticker from <see cref="SymbolFormat" />.</summary>
    internal const string FromPlaceholder = "{from}";

    /// <summary>The placeholder token replaced by the destination-currency code when building a ticker from <see cref="SymbolFormat" />.</summary>
    internal const string ToPlaceholder = "{to}";

    /// <summary>The inception of Yahoo Finance's foreign-exchange chart data: 1 December 2003, the earliest observation the chart endpoint serves for the longest-running currency pairs (for example <c>EURUSD=X</c>).</summary>
    internal static readonly DateOnly FxChartEpoch = new(2003, 12, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="YahooExchangeRateOptions" /> class with the Yahoo Finance host as
    /// its base address and a fixed history floor at the chart data's December 2003 inception.
    /// </summary>
    /// <remarks>
    /// The advertised <see cref="WebExchangeRateProviderOptions.HistoryAvailability" /> is advisory: individual pairs
    /// may start later than the December 2003 inception of the longest-running pairs. Override the property when the
    /// pairs in use are known to have a later floor.
    /// </remarks>
    public YahooExchangeRateOptions()
    {
        BaseAddress = new Uri("https://query1.finance.yahoo.com/");
        HistoryAvailability = RateHistoryAvailability.Since(FxChartEpoch);
    }

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
    /// Validates the Yahoo-specific options, ensuring the chart path and ticker template carry their placeholders.
    /// </summary>
    /// <param name="error">
    /// When this method returns <see langword="false" />, a message describing the violated invariant; otherwise
    /// <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when every Yahoo-specific invariant holds; otherwise <see langword="false" />.
    /// </returns>
    protected override bool TryValidateCore(out string? error)
    {
        if (string.IsNullOrWhiteSpace(ChartPath) || !ChartPath.Contains(SymbolPlaceholder, StringComparison.Ordinal))
        {
            error = YahooResourceStrings.Arg_Invalid_YahooOptionsChartPath;
            return false;
        }

        if (string.IsNullOrWhiteSpace(SymbolFormat)
            || !SymbolFormat.Contains(FromPlaceholder, StringComparison.Ordinal)
            || !SymbolFormat.Contains(ToPlaceholder, StringComparison.Ordinal))
        {
            error = YahooResourceStrings.Arg_Invalid_YahooOptionsSymbolFormat;
            return false;
        }

        error = null;
        return true;
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
}
