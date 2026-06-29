// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OandaExchangeRateOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Configures how the <see cref="OandaExchangeRateProvider" /> addresses and interprets the OANDA Historical Currency
/// Converter rate-history service.
/// </summary>
/// <remarks>
/// <para>
/// This type derives from <see cref="WebExchangeRateProviderOptions" />, which supplies the endpoint base address, the
/// HTTP contract, the synchronous-access and look-back behaviour, the currency-alias map, and the per-concern log
/// levels. The members declared here are OANDA-specific: the data and priming paths, the rate source, the price basis,
/// the reporting period, and the rate adjustment. The anonymous endpoint serves only a rolling recent window, so the
/// constructor sets <see cref="WebExchangeRateProviderOptions.HistoryAvailability" /> to a 180-day rolling window and
/// the response is range-filtered to the requested dates.
/// </para>
/// <para>
/// Every member carries a working default, so the options bind cleanly through <c>Microsoft.Extensions.Options</c> and
/// require no configuration for the common case. The dependency-injection package binds this type from configuration
/// and a <c>configure</c> delegate.
/// </para>
/// </remarks>
public sealed class OandaExchangeRateOptions
    : WebExchangeRateProviderOptions
{
    /// <summary>The number of days of history the anonymous OANDA endpoint serves, declared as the provider's rolling availability window.</summary>
    internal const int AnonymousHistoryWindowDays = 180;

    /// <summary>The set of price bases the OANDA endpoint accepts.</summary>
    private static readonly string[] s_validPrices = { "bid", "mid", "ask" };

    /// <summary>
    /// Initializes a new instance of the <see cref="OandaExchangeRateOptions" /> class with the OANDA Historical
    /// Currency Converter host as its base address and a 180-day rolling history window.
    /// </summary>
    public OandaExchangeRateOptions()
    {
        BaseAddress = new Uri("https://fxds-hcc.oanda.com/");
        HistoryAvailability = ExchangeRateHistoryAvailability.RollingDays(AnonymousHistoryWindowDays);
    }

    /// <summary>
    /// Gets or sets the relative path of the rate-history data endpoint.
    /// </summary>
    /// <value>The data path; defaults to <c>api/data/update/</c>.</value>
    public string UpdatePath { get; set; } = "api/data/update/";

    /// <summary>
    /// Gets or sets the relative path requested once to obtain the session cookie the data endpoint requires.
    /// </summary>
    /// <value>
    /// The priming path; defaults to the empty string, which resolves to
    /// <see cref="WebExchangeRateProviderOptions.BaseAddress" />.
    /// </value>
    public string PrimePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rate source requested from the OANDA endpoint.
    /// </summary>
    /// <value>The <c>source</c> query value; defaults to <c>OANDA</c> (OANDA Rates).</value>
    public string Source { get; set; } = "OANDA";

    /// <summary>
    /// Gets or sets the price basis requested from the OANDA endpoint.
    /// </summary>
    /// <value>
    /// The <c>price</c> query value; one of <c>bid</c>, <c>mid</c>, or <c>ask</c>. Defaults to <c>mid</c>.
    /// </value>
    public string Price { get; set; } = "mid";

    /// <summary>
    /// Gets or sets the reporting period requested from the OANDA endpoint.
    /// </summary>
    /// <value>
    /// The <c>period</c> query value, for example <c>daily</c>, <c>weekly</c>, or <c>monthly</c>. Defaults to
    /// <c>daily</c>.
    /// </value>
    public string Period { get; set; } = "daily";

    /// <summary>
    /// Gets or sets the rate adjustment requested from the OANDA endpoint.
    /// </summary>
    /// <value>The <c>adjustment</c> query value; defaults to 0.</value>
    public int Adjustment { get; set; }

    /// <summary>
    /// Validates the OANDA-specific options, ensuring the data path, source, and period are specified and the price
    /// basis is recognized.
    /// </summary>
    /// <param name="error">
    /// When this method returns <see langword="false" />, a message describing the violated invariant; otherwise
    /// <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when every OANDA-specific invariant holds; otherwise <see langword="false" />.
    /// </returns>
    protected override bool TryValidateCore(out string? error)
    {
        if (string.IsNullOrWhiteSpace(UpdatePath))
        {
            error = OandaResourceStrings.Arg_Invalid_OandaOptionsUpdatePath;
            return false;
        }

        if (string.IsNullOrWhiteSpace(Source))
        {
            error = OandaResourceStrings.Arg_Invalid_OandaOptionsSource;
            return false;
        }

        if (string.IsNullOrWhiteSpace(Period))
        {
            error = OandaResourceStrings.Arg_Invalid_OandaOptionsPeriod;
            return false;
        }

        if (Array.IndexOf(s_validPrices, Price) < 0)
        {
            error = OandaResourceStrings.Arg_Invalid_OandaOptionsPrice;
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Builds the query string for a rate-history request, applying the reporting parameters and any configured
    /// currency aliases.
    /// </summary>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="startDate">The inclusive start of the requested range.</param>
    /// <param name="endDate">The inclusive end of the requested range.</param>
    /// <returns>The query string, without the leading <c>?</c>.</returns>
    internal string BuildQuery(string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate)
    {
        StringBuilder query = new();
        query.Append("source=").Append(Uri.EscapeDataString(Source));
        query.Append("&adjustment=").Append(Adjustment.ToString(CultureInfo.InvariantCulture));
        query.Append("&base_currency=").Append(Uri.EscapeDataString(MapCurrency(fromIsoCode)));
        query.Append("&quote_currency_0=").Append(Uri.EscapeDataString(MapCurrency(toIsoCode)));
        query.Append("&price=").Append(Uri.EscapeDataString(Price));
        query.Append("&period=").Append(Uri.EscapeDataString(Period));
        query.Append("&view=graph");
        query.Append("&start_date=").Append(FormatDate(startDate));
        query.Append("&end_date=").Append(FormatDate(endDate));

        return query.ToString();
    }

    /// <summary>
    /// Formats a date as the unpadded <c>YYYY-M-D</c> the OANDA endpoint expects.
    /// </summary>
    /// <param name="date">The date to format.</param>
    /// <returns>The formatted date.</returns>
    private static string FormatDate(DateOnly date) =>
        string.Format(CultureInfo.InvariantCulture, "{0}-{1}-{2}", date.Year, date.Month, date.Day);
}
