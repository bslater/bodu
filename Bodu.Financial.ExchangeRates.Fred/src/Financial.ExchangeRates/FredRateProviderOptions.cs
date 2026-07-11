// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FredRateProviderOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Configures how the <see cref="FredRateProvider" /> addresses and interprets the FRED (Federal Reserve Bank of St.
/// Louis) foreign-exchange REST service.
/// </summary>
/// <remarks>
/// <para>
/// This type derives from <see cref="WebRateProviderOptions" />, which supplies the endpoint base address, the HTTP
/// contract, the synchronous-access and look-back behaviour, the currency-alias map, and the per-concern log levels.
/// The members declared here are FRED-specific: the required <see cref="ApiKey" /> presented as the <c>api_key</c>
/// query parameter, the <see cref="ObservationsPath" /> template, and the <see cref="SeriesMap" /> that maps each
/// currency pair to a single FRED series identifier.
/// </para>
/// <para>
/// FRED publishes each foreign-exchange rate as an independent time series quoted in a fixed direction (for example
/// <c>DEXUSEU</c> quotes US dollars per euro, that is <c>EUR/USD</c>). <see cref="SeriesMap" /> is keyed <c>FROM/TO</c>
/// in the exact direction the FRED series is quoted, so no inversion is required; the reverse direction is served by
/// the base class's inverse-lookup fallback. A pair with no mapped series yields no data rather than a fetch. Every
/// member other than <see cref="ApiKey" /> carries a working default, so the options bind cleanly through
/// <c>Microsoft.Extensions.Options</c>; the dependency-injection package binds this type from configuration and a
/// <c>configure</c> delegate.
/// </para>
/// </remarks>
public sealed class FredRateProviderOptions
    : WebRateProviderOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FredRateProviderOptions" /> class with the FRED host as its base
    /// address, an unbounded history window, and the built-in currency-pair series map seeded.
    /// </summary>
    public FredRateProviderOptions()
    {
        BaseAddress = new Uri("https://api.stlouisfed.org/fred/");
        HistoryAvailability = RateHistoryAvailability.Unbounded;

        SeriesMap = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["EUR/USD"] = "DEXUSEU",
            ["GBP/USD"] = "DEXUSUK",
            ["AUD/USD"] = "DEXUSAL",
            ["NZD/USD"] = "DEXUSNZ",
            ["USD/CAD"] = "DEXCAUS",
            ["USD/JPY"] = "DEXJPUS",
            ["USD/CHF"] = "DEXSZUS",
            ["USD/CNY"] = "DEXCHUS",
            ["USD/MXN"] = "DEXMXUS",
            ["USD/INR"] = "DEXINUS",
            ["USD/SEK"] = "DEXSDUS",
            ["USD/NOK"] = "DEXNOUS",
            ["USD/DKK"] = "DEXDNUS",
            ["USD/ZAR"] = "DEXSFUS",
            ["USD/BRL"] = "DEXBZUS",
            ["USD/KRW"] = "DEXKOUS",
            ["USD/HKD"] = "DEXHKUS",
            ["USD/SGD"] = "DEXSIUS",
            ["USD/TWD"] = "DEXTAUS",
        };
    }

    /// <summary>
    /// Gets or sets the FRED API key presented as the <c>api_key</c> query parameter on every request.
    /// </summary>
    /// <value>The API key. There is no default; a value is required.</value>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the relative series-observations endpoint path used to fetch a series' observations.
    /// </summary>
    /// <value>The observations path; defaults to <c>series/observations</c>.</value>
    public string ObservationsPath { get; set; } = "series/observations";

    /// <summary>
    /// Gets or sets the map from currency pair to FRED series identifier.
    /// </summary>
    /// <value>
    /// A dictionary keyed <c>FROM/TO</c> in the direction the FRED series is quoted, seeded with the built-in defaults.
    /// </value>
    /// <remarks>
    /// Add or replace entries to support additional pairs. Each key is the pair's <c>FROM/TO</c> ISO codes in the exact
    /// direction the FRED series is quoted; the reverse direction is served by the base class's inverse-lookup
    /// fallback.
    /// </remarks>
    public IDictionary<string, string> SeriesMap { get; set; }

    /// <summary>
    /// Validates the FRED-specific options, ensuring the API key, observations path, and series map are usable.
    /// </summary>
    /// <param name="error">
    /// When this method returns <see langword="false" />, a message describing the violated invariant; otherwise
    /// <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when every FRED-specific invariant holds; otherwise <see langword="false" />.
    /// </returns>
    protected override bool TryValidateCore(out string? error)
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            error = FredResourceStrings.Arg_Invalid_FredOptionsApiKey;
            return false;
        }

        if (string.IsNullOrWhiteSpace(ObservationsPath))
        {
            error = FredResourceStrings.Arg_Invalid_FredOptionsObservationsPath;
            return false;
        }

        if (SeriesMap is null)
        {
            error = FredResourceStrings.Arg_Invalid_FredOptionsSeriesMap;
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Looks up the FRED series identifier mapped to a currency pair.
    /// </summary>
    /// <param name="pair">The currency pair to resolve.</param>
    /// <param name="seriesId">
    /// When this method returns <see langword="true" />, the mapped FRED series identifier; otherwise an empty string.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when <see cref="SeriesMap" /> contains a series for the pair; otherwise
    /// <see langword="false" />.
    /// </returns>
    internal bool TryGetSeriesId(CurrencyPair pair, out string seriesId)
    {
        if (SeriesMap is not null && SeriesMap.TryGetValue($"{pair.From}/{pair.To}", out string? mapped) && !string.IsNullOrEmpty(mapped))
        {
            seriesId = mapped;
            return true;
        }

        seriesId = string.Empty;
        return false;
    }
}
