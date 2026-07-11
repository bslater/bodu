// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImfRateProviderOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Configures how the <see cref="ImfRateProvider" /> addresses, caches, and interprets the IMF Representative Exchange
/// Rates monthly report.
/// </summary>
/// <remarks>
/// <para>
/// This type derives from <see cref="WebRateProviderOptions" />, which supplies the endpoint base address, the HTTP
/// contract, the synchronous-access and look-back behaviour, and the per-concern log levels. The members declared here
/// are report-specific: the <see cref="ReportPath" /> and <see cref="ReportType" /> that address the monthly report,
/// the on-disk cache settings, and the <see cref="CurrencyNames" /> map that resolves an IMF currency label to its ISO
/// 4217 code. The report is keyless, so no API key is required.
/// </para>
/// <para>
/// <strong>USD-anchored, bulk.</strong> The report quotes every reported currency against the US dollar for each
/// business day of the month, so a single download warms the store for all currencies and all business days at once.
/// Only <c>USD/X</c> and <c>X/USD</c> pairs are serviceable; the reverse direction is served by the base inverse-lookup
/// fallback. Every member carries a working default, so the options bind cleanly through
/// <c>Microsoft.Extensions.Options</c>.
/// </para>
/// </remarks>
public sealed class ImfRateProviderOptions
    : WebRateProviderOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImfRateProviderOptions" /> class with the IMF report host as its
    /// base address, an unbounded history depth, and the default currency-name map.
    /// </summary>
    public ImfRateProviderOptions()
    {
        BaseAddress = new Uri("https://www.imf.org/external/np/fin/data/");
        HistoryAvailability = RateHistoryAvailability.Unbounded;

        CurrencyNames = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Chinese yuan"] = "CNY",
            ["Euro"] = "EUR",
            ["Japanese yen"] = "JPY",
            ["U.K. pound"] = "GBP",
            ["Algerian dinar"] = "DZD",
            ["Australian dollar"] = "AUD",
            ["Botswana pula"] = "BWP",
            ["Brazilian real"] = "BRL",
            ["Brunei dollar"] = "BND",
            ["Canadian dollar"] = "CAD",
            ["Chilean peso"] = "CLP",
            ["Czech koruna"] = "CZK",
            ["Danish krone"] = "DKK",
            ["Indian rupee"] = "INR",
            ["Israeli New Shekel"] = "ILS",
            ["Korean won"] = "KRW",
            ["Kuwaiti dinar"] = "KWD",
            ["Malaysian ringgit"] = "MYR",
            ["Mauritian rupee"] = "MUR",
            ["Mexican peso"] = "MXN",
            ["New Zealand dollar"] = "NZD",
            ["Norwegian krone"] = "NOK",
            ["Omani rial"] = "OMR",
            ["Peruvian sol"] = "PEN",
            ["Philippine peso"] = "PHP",
            ["Polish zloty"] = "PLN",
            ["Qatari riyal"] = "QAR",
            ["Saudi Arabian riyal"] = "SAR",
            ["Singapore dollar"] = "SGD",
            ["Swedish krona"] = "SEK",
            ["Swiss franc"] = "CHF",
            ["Thai baht"] = "THB",
            ["Trinidadian dollar"] = "TTD",
            ["U.A.E. dirham"] = "AED",
            ["Uruguayan peso"] = "UYU",
        };
    }

    /// <summary>
    /// Gets or sets the relative report resource path appended to the base address.
    /// </summary>
    /// <value>The report path; defaults to <c>rms_mth.aspx</c>.</value>
    public string ReportPath { get; set; } = "rms_mth.aspx";

    /// <summary>
    /// Gets or sets the report-type selector supplied to the endpoint.
    /// </summary>
    /// <value>The report type; defaults to <c>REP</c> (representative rates).</value>
    public string ReportType { get; set; } = "REP";

    /// <summary>
    /// Gets or sets a value indicating whether downloaded report files are persisted to an on-disk cache.
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
    /// The cache directory, or <see langword="null" /> to use a <c>bodu-imf</c> folder under the system temporary path.
    /// </value>
    public string? CacheDirectory { get; set; }

    /// <summary>
    /// Gets or sets how long a cached copy of the current month's report remains fresh before it is re-downloaded.
    /// </summary>
    /// <value>The refresh interval; defaults to 12 hours.</value>
    /// <remarks>
    /// A closed month's report is immutable and is served from cache regardless of age; only the current, still-growing
    /// month is subject to this interval.
    /// </remarks>
    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromHours(12);

    /// <summary>
    /// Gets or sets the map from an IMF currency label (footnote-stripped and trimmed) to its ISO 4217 code, applied
    /// while normalizing a report row.
    /// </summary>
    /// <value>The currency-name map; seeded in the constructor with the currencies the report commonly quotes.</value>
    /// <remarks>
    /// A label absent from this map is resolved directly against the ISO 4217 catalogue, and a label that resolves
    /// neither way is skipped. Extend the map to add labels the IMF publishes that are not seeded here.
    /// </remarks>
    public IDictionary<string, string> CurrencyNames { get; set; }

    /// <summary>
    /// Attempts to resolve an IMF currency label to its ISO 4217 code, consulting <see cref="CurrencyNames" /> first
    /// and then the ISO 4217 catalogue.
    /// </summary>
    /// <param name="label">The footnote-stripped, trimmed currency label.</param>
    /// <param name="code">
    /// When this method returns <see langword="true" />, the resolved currency code; otherwise the default.
    /// </param>
    /// <returns><see langword="true" /> when the label resolved; otherwise <see langword="false" />.</returns>
    internal bool TryResolveCurrency(string label, out CurrencyCode code)
    {
        if (CurrencyNames.TryGetValue(label, out string? iso))
            return CurrencyInfo.TryGetCurrencyCode(iso, out code);

        return CurrencyInfo.TryGetCurrencyCode(label, out code);
    }

    /// <summary>
    /// Validates the report-specific options, ensuring the report path, report type, and currency-name map are usable.
    /// </summary>
    /// <param name="error">
    /// When this method returns <see langword="false" />, a message describing the violated invariant; otherwise
    /// <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when every report-specific invariant holds; otherwise <see langword="false" />.
    /// </returns>
    protected override bool TryValidateCore(out string? error)
    {
        if (string.IsNullOrWhiteSpace(ReportPath))
        {
            error = ImfResourceStrings.Arg_Invalid_ImfOptionsReportPath;
            return false;
        }

        if (string.IsNullOrWhiteSpace(ReportType))
        {
            error = ImfResourceStrings.Arg_Invalid_ImfOptionsReportType;
            return false;
        }

        if (CurrencyNames is null)
        {
            error = ImfResourceStrings.Arg_Invalid_ImfOptionsCurrencyNames;
            return false;
        }

        error = null;
        return true;
    }
}
