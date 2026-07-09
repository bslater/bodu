// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OfxRateProviderOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Configures how the <see cref="OfxRateProvider" /> addresses and interprets the OFX public spot-rate-history
/// REST service.
/// </summary>
/// <remarks>
/// <para>
/// This type derives from <see cref="WebRateProviderOptions" />, which supplies the endpoint base address, the
/// HTTP contract, the synchronous-access and look-back behaviour, the currency-alias map, and the per-concern log
/// levels. The members declared here are OFX-specific: the <see cref="HistoryPath" /> template into which the currency
/// codes and the requested inclusive date range (as Unix-millisecond bounds) are substituted, the requested decimal
/// precision, and the reporting interval sent as the <c>ReportingInterval</c> query parameter. The response is
/// additionally range-filtered to the requested dates as a defensive measure.
/// </para>
/// <para>
/// Every member carries a working default, so the options bind cleanly through <c>Microsoft.Extensions.Options</c> and
/// require no configuration for the common case. The dependency-injection package binds this type from configuration
/// and a <c>configure</c> delegate.
/// </para>
/// </remarks>
public sealed class OfxRateProviderOptions
    : WebRateProviderOptions
{
    /// <summary>The placeholder token replaced by the source-currency code when building a path from <see cref="HistoryPath" />.</summary>
    internal const string FromPlaceholder = "{from}";

    /// <summary>The placeholder token replaced by the destination-currency code when building a path from <see cref="HistoryPath" />.</summary>
    internal const string ToPlaceholder = "{to}";

    /// <summary>The placeholder token replaced by the inclusive range start (Unix milliseconds) when building a path from <see cref="HistoryPath" />.</summary>
    internal const string StartPlaceholder = "{start}";

    /// <summary>The placeholder token replaced by the inclusive range end (Unix milliseconds) when building a path from <see cref="HistoryPath" />.</summary>
    internal const string EndPlaceholder = "{end}";

    /// <summary>
    /// Initializes a new instance of the <see cref="OfxRateProviderOptions" /> class with the OFX API host as its base
    /// address and a deliberately unbounded history declaration.
    /// </summary>
    /// <remarks>
    /// OFX publishes multi-decade spot-rate history ("20+ years") but no fixed inception date, so the advertised
    /// <see cref="WebRateProviderOptions.HistoryAvailability" /> is deliberately
    /// <see cref="RateHistoryAvailability.Unbounded" /> — there is no known floor worth pre-empting a request
    /// for. Set the property when a concrete floor matters for the pairs in use.
    /// </remarks>
    public OfxRateProviderOptions()
    {
        BaseAddress = new Uri("https://api.ofx.com/");
        HistoryAvailability = RateHistoryAvailability.Unbounded;
    }

    /// <summary>
    /// Gets or sets the relative spot-rate-history path template. The <c>{from}</c>, <c>{to}</c>, <c>{start}</c>, and
    /// <c>{end}</c> placeholders are replaced by the currency codes and the inclusive request range (as
    /// Unix-millisecond bounds) before the request is issued.
    /// </summary>
    /// <value>
    /// The history path template; defaults to <c>PublicSite.ApiService/SpotRateHistory/{from}/{to}/{start}/{end}</c>.
    /// </value>
    public string HistoryPath { get; set; } = "PublicSite.ApiService/SpotRateHistory/{from}/{to}/{start}/{end}";

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
            || !HistoryPath.Contains(FromPlaceholder, StringComparison.Ordinal)
            || !HistoryPath.Contains(ToPlaceholder, StringComparison.Ordinal)
            || !HistoryPath.Contains(StartPlaceholder, StringComparison.Ordinal)
            || !HistoryPath.Contains(EndPlaceholder, StringComparison.Ordinal))
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
    /// Builds the relative spot-rate-history path for a currency pair and inclusive date range from
    /// <see cref="HistoryPath" />, applying any configured currency aliases.
    /// </summary>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="startUnixMilliseconds">The inclusive range start, expressed as Unix milliseconds.</param>
    /// <param name="endUnixMilliseconds">The inclusive range end, expressed as Unix milliseconds.</param>
    /// <returns>
    /// The relative request path, for example one ending in <c>USD/AUD/1672531200000/1675209599999</c>.
    /// </returns>
    internal string BuildPath(string fromIsoCode, string toIsoCode, long startUnixMilliseconds, long endUnixMilliseconds) =>
        HistoryPath
            .Replace(FromPlaceholder, MapCurrency(fromIsoCode), StringComparison.Ordinal)
            .Replace(ToPlaceholder, MapCurrency(toIsoCode), StringComparison.Ordinal)
            .Replace(StartPlaceholder, startUnixMilliseconds.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace(EndPlaceholder, endUnixMilliseconds.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
}
