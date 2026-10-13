// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeRateProviderOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Configures how the <see cref="BoeRateProvider" /> downloads, caches, and interprets Bank of England daily spot
/// exchange-rate data.
/// </summary>
/// <remarks>
/// <para>
/// This type derives from <see cref="WebRateProviderOptions" />, which supplies the endpoint base address, the HTTP
/// contract (timeout, user agent and response-size cap), the synchronous-access, look-back and refresh behavior, the
/// advertised history, the currency-alias map, and the per-concern log levels. The constructor points
/// <see cref="WebRateProviderOptions.BaseAddress" /> at the Interactive Statistical Database (IADB), identifies the
/// requests with a Bodu user agent, sets a ten-day <see cref="WebRateProviderOptions.DefaultLookback" />, and floors
/// the advertised history at 2 January 1975. The members declared here are Bank of England-specific: the
/// <see cref="Series" /> catalogue, the IADB <see cref="QueryPath" />, and the on-disk cache settings.
/// </para>
/// <para>
/// A range request is composed from <see cref="WebRateProviderOptions.BaseAddress" />, <see cref="QueryPath" />, the
/// catalogue's series codes, and the inclusive date range. The inherited
/// <see cref="WebRateProviderOptions.RefreshInterval" /> governs both the in-memory ranges and the on-disk cache: a
/// cached range response older than the interval is downloaded again, and in memory a loaded range keeps for good every
/// day before the day it was downloaded and every later day it returned a rate for, while its other days are downloaded
/// again by a request that reaches them once the interval has passed.
/// </para>
/// <para>
/// The IADB names series, not currencies, so <see cref="WebRateProviderOptions.CurrencyAliases" /> applies to the quote
/// label of each <see cref="BoeSeries" /> in the catalogue: a label found among the keys is replaced by its value,
/// which must be the ISO 4217 code the provider serves the series under. Every member carries a working default, so the
/// options bind cleanly through <c>Microsoft.Extensions.Options</c>; the dependency-injection registration binds this
/// type from configuration and a <c>configure</c> delegate.
/// </para>
/// </remarks>
public sealed class BoeRateProviderOptions
    : WebRateProviderOptions
{
    /// <summary>The inception of the Bank of England's daily spot exchange-rate series: 2 January 1975, the first observation of the longest-running IADB <c>XUDL*</c> series.</summary>
    internal static readonly DateOnly DailySpotSeriesEpoch = new(1975, 1, 2);

    /// <summary>
    /// Initializes a new instance of the <see cref="BoeRateProviderOptions" /> class with the IADB as its base address,
    /// a Bodu user agent, a ten-day look-back, and a history floor at the 1975 inception of the daily spot series.
    /// </summary>
    /// <remarks>
    /// The IADB daily spot series (<c>XUDL*</c>) begin on 2 January 1975 for the longest-running currencies; some
    /// series start later (the euro series begins 4 January 1999, with the launch of the euro). The advertised history
    /// is advisory - it bounds the earliest date worth requesting for the default <see cref="Series" /> catalogue, not
    /// a per-series guarantee - so narrow <see cref="WebRateProviderOptions.HistoryAvailability" /> when the catalogue
    /// is restricted to later-inception series.
    /// </remarks>
    public BoeRateProviderOptions()
    {
        BaseAddress = new Uri("https://www.bankofengland.co.uk/boeapps/database/");
        UserAgent = "Bodu.Financial.ExchangeRates.Boe";
        DefaultLookback = TimeSpan.FromDays(10);
        HistoryAvailability = RateHistoryAvailability.Since(DailySpotSeriesEpoch);
    }

    /// <summary>
    /// Gets or sets the catalog of currency series to request, mapping each quote currency to its IADB series code.
    /// </summary>
    /// <value>The series catalog; defaults to <see cref="BoeSeries.Default" />.</value>
    public IReadOnlyList<BoeSeries> Series { get; set; } = BoeSeries.Default;

    /// <summary>
    /// Gets or sets the relative path of the IADB CSV column query endpoint, resolved against
    /// <see cref="WebRateProviderOptions.BaseAddress" />.
    /// </summary>
    /// <value>The query path; defaults to <c>_iadb-fromshowcolumns.asp</c>.</value>
    public string QueryPath { get; set; } = "_iadb-fromshowcolumns.asp";

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
    /// Composes the absolute IADB CSV request URL for a set of series codes over an inclusive date range.
    /// </summary>
    /// <param name="seriesCodes">The IADB series codes to request.</param>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <returns>The absolute request URL.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="seriesCodes" /> is <see langword="null" />.
    /// </exception>
    internal Uri BuildRequestUrl(IReadOnlyList<string> seriesCodes, DateOnly startDate, DateOnly endDate)
    {
        ThrowHelper.ThrowIfNull(seriesCodes);

        // Escape each series code individually and join with a literal comma so the comma stays a valid list
        // separator in the query. Escaping the joined string instead would percent-encode the separators (%2C),
        // which the IADB only tolerates incidentally.
        string codes = string.Join(",", seriesCodes.Select(Uri.EscapeDataString));
        string query =
            "?csv.x=yes" +
            "&Datefrom=" + Uri.EscapeDataString(FormatDate(startDate)) +
            "&Dateto=" + Uri.EscapeDataString(FormatDate(endDate)) +
            "&SeriesCodes=" + codes +
            "&CSVF=TN&UsingCodes=Y&VPD=Y&VFD=N";

        return new Uri(BaseAddress, QueryPath + query);
    }

    /// <summary>
    /// Maps a series' quote label to the ISO 4217 code the provider serves it under, applying any configured currency
    /// alias.
    /// </summary>
    /// <param name="quoteLabel">The quote label of a <see cref="BoeSeries" />.</param>
    /// <returns>The aliased code, or <paramref name="quoteLabel" /> when unmapped.</returns>
    internal string MapQuoteCurrency(string quoteLabel) => MapCurrency(quoteLabel);

    /// <summary>
    /// Validates the Bank of England-specific options, ensuring the series catalogue is not empty and the query path is
    /// usable.
    /// </summary>
    /// <param name="error">
    /// When this method returns <see langword="false" />, a message describing the violated invariant; otherwise
    /// <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when every Bank of England-specific invariant holds; otherwise <see langword="false" />.
    /// </returns>
    protected override bool TryValidateCore(out string? error)
    {
        if (Series is null || Series.Count == 0)
        {
            error = BoeResourceStrings.Arg_Invalid_BoeOptionsSeries;
            return false;
        }

        if (string.IsNullOrWhiteSpace(QueryPath))
        {
            error = BoeResourceStrings.Arg_Invalid_BoeOptionsQueryPath;
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Formats a date in the <c>dd/MMM/yyyy</c> form the IADB query parameters expect.
    /// </summary>
    /// <param name="date">The date to format.</param>
    /// <returns>The formatted date.</returns>
    private static string FormatDate(DateOnly date) =>
        date.ToString("dd/MMM/yyyy", CultureInfo.InvariantCulture);
}
