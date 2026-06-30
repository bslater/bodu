// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OandaHistoryResponseParser.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Parses an OANDA rate-history JSON response into a <see cref="PairRateData{TSeries}" />.
/// </summary>
/// <remarks>
/// The response carries a <c>widget</c> array, one entry per requested quote currency; each entry pairs a
/// <c>baseCurrency</c> and <c>quoteCurrency</c> with a <c>data</c> array of <c>[PointInTime, Rate]</c> rows, where
/// <c>PointInTime</c> is a Unix timestamp in milliseconds interpreted in UTC and <c>Rate</c> is the price-selected
/// rate. The OANDA anonymous endpoint serves a server-determined recent window, so rows outside the request's inclusive
/// range are skipped, as are rows with a missing or non-positive rate. Numeric members are tolerated as JSON numbers or
/// numeric strings.
/// </remarks>
internal static class OandaHistoryResponseParser
{
    /// <summary>
    /// Parses a rate-history response, restricting observations to the request's inclusive date range.
    /// </summary>
    /// <param name="json">The UTF-8 JSON response bytes.</param>
    /// <param name="request">The originating request, supplying the pair and date range.</param>
    /// <param name="options">The provider options used to build the series metadata.</param>
    /// <returns>The parsed, range-restricted observations and series metadata.</returns>
    /// <exception cref="ExchangeRateFormatException">
    /// Thrown when the response is not valid JSON or omits the expected <c>widget</c> array.
    /// </exception>
    public static PairRateData<OandaSeriesInfo> Parse(byte[] json, ExchangeRatePairRequest request, OandaExchangeRateOptions options)
    {
        ThrowHelper.ThrowIfNull(json);
        ThrowHelper.ThrowIfNull(options);

        using JsonDocument document = ParseDocument(json, request);
        JsonElement root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("widget", out JsonElement widget)
            || widget.ValueKind != JsonValueKind.Array
            || widget.GetArrayLength() == 0)
        {
            throw NoData(request);
        }

        JsonElement series = SelectSeries(widget, request.Pair);
        var observations = new List<ExchangeRateObservation>();

        if (series.TryGetProperty("data", out JsonElement data) && data.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement row in data.EnumerateArray())
            {
                if (row.ValueKind != JsonValueKind.Array || row.GetArrayLength() < 2)
                    continue;

                if (!TryReadUnixMilliseconds(row[0], out long unixMs) || !TryReadRate(row[1], out decimal rate))
                    continue;

                var date = DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeMilliseconds(unixMs).UtcDateTime);
                if (date < request.StartDate || date > request.EndDate)
                    continue;

                observations.Add(new ExchangeRateObservation(date, rate));
            }
        }

        OandaSeriesInfo info = new(request.Pair, request.Pair.To.ToString(), options.Price);
        return new PairRateData<OandaSeriesInfo>(request.Pair, observations, info);
    }

    /// <summary>
    /// Selects the widget entry that matches the requested pair, falling back to the first entry when none matches.
    /// </summary>
    /// <param name="widget">The widget array.</param>
    /// <param name="pair">The requested currency pair.</param>
    /// <returns>The matching widget entry, or the first entry when no exact match is present.</returns>
    private static JsonElement SelectSeries(JsonElement widget, ExchangeRatePair pair)
    {
        string from = pair.From.ToString();
        string to = pair.To.ToString();

        foreach (JsonElement entry in widget.EnumerateArray())
        {
            if (entry.ValueKind == JsonValueKind.Object
                && entry.TryGetProperty("baseCurrency", out JsonElement baseCurrency)
                && entry.TryGetProperty("quoteCurrency", out JsonElement quoteCurrency)
                && string.Equals(baseCurrency.GetString(), from, StringComparison.OrdinalIgnoreCase)
                && string.Equals(quoteCurrency.GetString(), to, StringComparison.OrdinalIgnoreCase))
            {
                return entry;
            }
        }

        return widget[0];
    }

    /// <summary>
    /// Parses the response bytes into a <see cref="JsonDocument" />, translating malformed JSON into a format
    /// exception.
    /// </summary>
    /// <param name="json">The UTF-8 JSON response bytes.</param>
    /// <param name="request">The originating request, used for the error message.</param>
    /// <returns>The parsed document.</returns>
    /// <exception cref="ExchangeRateFormatException">Thrown when the bytes are not valid JSON.</exception>
    private static JsonDocument ParseDocument(byte[] json, ExchangeRatePairRequest request)
    {
        try
        {
            return JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new ExchangeRateFormatException(
                string.Format(CultureInfo.CurrentCulture, OandaResourceStrings.Format_Invalid_OandaNoData, request.Pair.From.ToString(), request.Pair.To.ToString()),
                ex);
        }
    }

    /// <summary>
    /// Reads a Unix-millisecond timestamp from a row element, tolerating a JSON number or numeric string.
    /// </summary>
    /// <param name="element">The timestamp element.</param>
    /// <param name="unixMs">When this method returns <see langword="true" />, the Unix-millisecond timestamp.</param>
    /// <returns><see langword="true" /> when a timestamp was read; otherwise <see langword="false" />.</returns>
    private static bool TryReadUnixMilliseconds(JsonElement element, out long unixMs)
    {
        unixMs = 0;

        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetInt64(out unixMs),
            JsonValueKind.String => long.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out unixMs),
            _ => false,
        };
    }

    /// <summary>
    /// Reads the strictly positive rate from a row element, tolerating a JSON number or numeric string.
    /// </summary>
    /// <param name="element">The rate element.</param>
    /// <param name="rate">When this method returns <see langword="true" />, the positive rate.</param>
    /// <returns><see langword="true" /> when a positive rate was read; otherwise <see langword="false" />.</returns>
    private static bool TryReadRate(JsonElement element, out decimal rate)
    {
        rate = 0m;

        bool parsed = element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetDecimal(out rate),
            JsonValueKind.String => decimal.TryParse(element.GetString(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out rate),
            _ => false,
        };

        return parsed && rate > 0m;
    }

    /// <summary>
    /// Builds the no-data format exception for a request.
    /// </summary>
    /// <param name="request">The originating request.</param>
    /// <returns>The exception to throw.</returns>
    private static ExchangeRateFormatException NoData(ExchangeRatePairRequest request) =>
        new(string.Format(CultureInfo.CurrentCulture, OandaResourceStrings.Format_Invalid_OandaNoData, request.Pair.From.ToString(), request.Pair.To.ToString()));
}
