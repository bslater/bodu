// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OfxExchangeRateOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Ofx;

/// <summary>
/// Configures how the <see cref="OfxExchangeRateProvider" /> addresses and interprets the OFX public spot-rate-history
/// REST service.
/// </summary>
/// <remarks>
/// <para>
/// This type derives from <see cref="WebExchangeRateProviderOptions" />, which supplies the endpoint base address, the
/// HTTP contract, the synchronous-access and look-back behaviour, the currency-alias map, and the per-concern log
/// levels. The members declared here are OFX-specific: the <see cref="HistoryPath" /> template into which the reporting
/// interval and currency codes are substituted, the requested decimal precision, and the reporting interval. OFX serves
/// a server-determined historical window per request, so the response is range-filtered to the requested dates.
/// </para>
/// <para>
/// Every member carries a working default, so the options bind cleanly through <c>Microsoft.Extensions.Options</c> and
/// require no configuration for the common case. The dependency-injection package binds this type from configuration
/// and a <c>configure</c> delegate.
/// </para>
/// </remarks>
public sealed class OfxExchangeRateOptions
    : WebExchangeRateProviderOptions
{
    /// <summary>The placeholder token replaced by the source-currency code when building a path from <see cref="HistoryPath" />.</summary>
    internal const string FromPlaceholder = "{from}";

    /// <summary>The placeholder token replaced by the destination-currency code when building a path from <see cref="HistoryPath" />.</summary>
    internal const string ToPlaceholder = "{to}";

    /// <summary>The placeholder token replaced by the reporting interval when building a path from <see cref="HistoryPath" />.</summary>
    internal const string IntervalPlaceholder = "{interval}";

    /// <summary>
    /// Initializes a new instance of the <see cref="OfxExchangeRateOptions" /> class with the OFX API host as its base
    /// address.
    /// </summary>
    public OfxExchangeRateOptions()
    {
        BaseAddress = new Uri("https://api.ofx.com/");
    }

    /// <summary>
    /// Gets or sets the relative spot-rate-history path template. The <c>{interval}</c>, <c>{from}</c>, and <c>{to}</c>
    /// placeholders are replaced by the reporting interval and the currency codes before the request is issued.
    /// </summary>
    /// <value>
    /// The history path template; defaults to <c>PublicSite.ApiService/SpotRateHistory/{interval}/{from}/{to}</c>.
    /// </value>
    public string HistoryPath { get; set; } = "PublicSite.ApiService/SpotRateHistory/{interval}/{from}/{to}";

    /// <summary>
    /// Gets or sets the number of decimal places requested from the OFX endpoint.
    /// </summary>
    /// <value>The requested decimal precision; defaults to 6.</value>
    public int DecimalPlaces { get; set; } = 6;

    /// <summary>
    /// Gets or sets the reporting interval requested from the OFX endpoint, substituted into <see cref="HistoryPath" />
    /// and sent as the <c>ReportingInterval</c> query parameter.
    /// </summary>
    /// <value>The reporting interval; defaults to <c>daily</c>.</value>
    public string ReportingInterval { get; set; } = "daily";

    /// <summary>
    /// Validates the OFX-specific options, ensuring the history path carries its placeholders, the decimal precision is
    /// in range, and the reporting interval is specified.
    /// </summary>
    /// <param name="error">
    /// When this method returns <see langword="false" />, a message describing the violated invariant; otherwise
    /// <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when every OFX-specific invariant holds; otherwise <see langword="false" />.
    /// </returns>
    protected override bool TryValidateCore(out string? error)
    {
        if (string.IsNullOrWhiteSpace(HistoryPath)
            || !HistoryPath.Contains(IntervalPlaceholder, StringComparison.Ordinal)
            || !HistoryPath.Contains(FromPlaceholder, StringComparison.Ordinal)
            || !HistoryPath.Contains(ToPlaceholder, StringComparison.Ordinal))
        {
            error = OfxResourceStrings.Arg_Invalid_OfxOptionsHistoryPath;
            return false;
        }

        if (DecimalPlaces is < 0 or > 15)
        {
            error = OfxResourceStrings.Arg_Invalid_OfxOptionsDecimalPlaces;
            return false;
        }

        if (string.IsNullOrWhiteSpace(ReportingInterval))
        {
            error = OfxResourceStrings.Arg_Invalid_OfxOptionsReportingInterval;
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Builds the relative spot-rate-history path for a currency pair from <see cref="HistoryPath" />, applying the
    /// reporting interval and any configured currency aliases.
    /// </summary>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <returns>
    /// The relative request path, for example <c>PublicSite.ApiService/SpotRateHistory/daily/USD/AUD</c>.
    /// </returns>
    internal string BuildPath(string fromIsoCode, string toIsoCode) =>
        HistoryPath
            .Replace(IntervalPlaceholder, ReportingInterval, StringComparison.Ordinal)
            .Replace(FromPlaceholder, MapCurrency(fromIsoCode), StringComparison.Ordinal)
            .Replace(ToPlaceholder, MapCurrency(toIsoCode), StringComparison.Ordinal);
}
