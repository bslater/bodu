// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImfRateProviderOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Configures how the <see cref="ImfRateProvider" /> addresses and interprets the IMF (International Monetary Fund)
/// SDMX Data API for daily exchange rates.
/// </summary>
/// <remarks>
/// <para>
/// This type derives from <see cref="WebRateProviderOptions" />, which supplies the endpoint base address, the HTTP
/// contract, the synchronous-access and look-back behaviour, the currency-alias map, and the per-concern log levels.
/// The members declared here are IMF-specific: the <see cref="DataPath" />, <see cref="Dataflow" />, and
/// <see cref="DataVersion" /> that address the SDMX 3.0 data resource, and the <see cref="SeriesMap" /> that resolves a
/// currency pair to an SDMX series key. The IMF Data API is keyless, so no API key is required.
/// </para>
/// <para>
/// <strong>Daily, USD/SDR-anchored.</strong> The provider requests the <b>daily</b> (<c>FREQ = D</c>) exchange-rate
/// series from the IMF Exchange Rates (<c>ER</c>) dataflow; the seeded series are the domestic-currency-per-USD rates (<c>ENDE_XDC_USD_RATE</c>).
/// Only pairs present in <see cref="SeriesMap" /> (or their reverse, served by the base inverse-lookup fallback) are
/// serviceable; an unmapped pair yields no data. Extend <see cref="SeriesMap" /> to add more pairs. Every member
/// carries a working default, so the options bind cleanly through <c>Microsoft.Extensions.Options</c>.
/// </para>
/// </remarks>
public sealed class ImfRateProviderOptions
    : WebRateProviderOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImfRateProviderOptions" /> class with the IMF SDMX Data API host as
    /// its base address, an unbounded history depth, and the default daily domestic-currency-per-USD series map.
    /// </summary>
    public ImfRateProviderOptions()
    {
        BaseAddress = new Uri("https://api.imf.org/external/sdmx/3.0/");
        HistoryAvailability = RateHistoryAvailability.Unbounded;

        SeriesMap = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["USD/GBP"] = "D.GB.ENDE_XDC_USD_RATE",
            ["USD/EUR"] = "D.U2.ENDE_XDC_USD_RATE",
            ["USD/JPY"] = "D.JP.ENDE_XDC_USD_RATE",
            ["USD/CAD"] = "D.CA.ENDE_XDC_USD_RATE",
            ["USD/CHF"] = "D.CH.ENDE_XDC_USD_RATE",
            ["USD/AUD"] = "D.AU.ENDE_XDC_USD_RATE",
            ["USD/INR"] = "D.IN.ENDE_XDC_USD_RATE",
            ["USD/ZAR"] = "D.ZA.ENDE_XDC_USD_RATE",
            ["USD/MXN"] = "D.MX.ENDE_XDC_USD_RATE",
            ["USD/KRW"] = "D.KR.ENDE_XDC_USD_RATE",
        };
    }

    /// <summary>
    /// Gets or sets the relative SDMX data-resource path segment that precedes the dataflow and series key.
    /// </summary>
    /// <value>The data path; defaults to <c>data/dataflow</c> (the SDMX 3.0 data resource).</value>
    public string DataPath { get; set; } = "data/dataflow";

    /// <summary>
    /// Gets or sets the agency-qualified SDMX dataflow identifier the series are drawn from.
    /// </summary>
    /// <value>The dataflow identifier; defaults to <c>IMF.STA/ER</c> (the IMF Exchange Rates dataflow).</value>
    public string Dataflow { get; set; } = "IMF.STA/ER";

    /// <summary>
    /// Gets or sets the dataflow version selector appended to the data path.
    /// </summary>
    /// <value>The version selector; defaults to <c>+</c> (the latest published version).</value>
    public string DataVersion { get; set; } = "+";

    /// <summary>
    /// Gets or sets the map from a <c>FROM/TO</c> pair key to the SDMX series key fragment (<c>{freq}.{area}.{indicator}</c>)
    /// that quotes it. A pair absent from the map — and whose reverse is also absent — is not serviceable and yields no
    /// data.
    /// </summary>
    /// <value>
    /// The series map; seeded in the constructor with the daily domestic-currency-per-USD series for a set of common
    /// currencies.
    /// </value>
    /// <remarks>
    /// Each key is stored in the direction the series is quoted (for example <c>USD/GBP</c> maps to the GBP-per-USD
    /// series), so no inversion is needed; the reverse direction is served by the base inverse-lookup fallback.
    /// </remarks>
    public IDictionary<string, string> SeriesMap { get; set; }

    /// <summary>
    /// Validates the IMF-specific options, ensuring the data path, dataflow, version, and series map are usable.
    /// </summary>
    /// <param name="error">
    /// When this method returns <see langword="false" />, a message describing the violated invariant; otherwise
    /// <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when every IMF-specific invariant holds; otherwise <see langword="false" />.
    /// </returns>
    protected override bool TryValidateCore(out string? error)
    {
        if (string.IsNullOrWhiteSpace(DataPath))
        {
            error = ImfResourceStrings.Arg_Invalid_ImfOptionsDataPath;
            return false;
        }

        if (string.IsNullOrWhiteSpace(Dataflow))
        {
            error = ImfResourceStrings.Arg_Invalid_ImfOptionsDataflow;
            return false;
        }

        if (string.IsNullOrWhiteSpace(DataVersion))
        {
            error = ImfResourceStrings.Arg_Invalid_ImfOptionsDataVersion;
            return false;
        }

        if (SeriesMap is null)
        {
            error = ImfResourceStrings.Arg_Invalid_ImfOptionsSeriesMap;
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Attempts to resolve the SDMX series key for a currency pair from <see cref="SeriesMap" />.
    /// </summary>
    /// <param name="pair">The currency pair to resolve.</param>
    /// <param name="seriesKey">
    /// When this method returns <see langword="true" />, the mapped SDMX series key; otherwise an empty string.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the pair is present in <see cref="SeriesMap" />; otherwise <see langword="false" />.
    /// </returns>
    internal bool TryGetSeriesKey(CurrencyPair pair, out string seriesKey)
    {
        if (SeriesMap.TryGetValue($"{pair.From}/{pair.To}", out string? mapped))
        {
            seriesKey = mapped;
            return true;
        }

        seriesKey = string.Empty;
        return false;
    }
}
