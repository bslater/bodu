// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImfResponseParser.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Parses an IMF SDMX-JSON CompactData response into a <see cref="PairRateData{TSeries}" />.
/// </summary>
/// <remarks>
/// A CompactData response nests <c>CompactData</c> → <c>DataSet</c> → <c>Series</c>. The <c>Series</c> node may be a
/// single object or an array of series objects, and each series carries an <c>Obs</c> node that is likewise a single
/// object or an array. Each observation exposes a <c>@TIME_PERIOD</c> in <c>YYYY-MM</c> form and an <c>@OBS_VALUE</c>
/// (a string or number). Observations map to the first day of their month; non-positive, unparseable, or out-of-range
/// observations are dropped. A response that is not valid JSON, or that omits the expected CompactData/DataSet/Series
/// path, is translated into an <see cref="ExchangeRateFormatException" />.
/// </remarks>
internal static class ImfResponseParser
{
    /// <summary>
    /// Parses an IMF SDMX-JSON response, restricting observations to the request's inclusive date range.
    /// </summary>
    /// <param name="json">The UTF-8 JSON response bytes.</param>
    /// <param name="request">The originating request, supplying the pair and date range.</param>
    /// <param name="seriesKey">The SDMX series key queried, recorded on the series metadata.</param>
    /// <returns>The parsed, range-restricted observations and series metadata.</returns>
    /// <exception cref="ExchangeRateFormatException">
    /// Thrown when the response is not valid JSON or omits the expected CompactData rate data.
    /// </exception>
    public static PairRateData<ImfSeriesInfo> Parse(byte[] json, CurrencyPairRequest request, string seriesKey)
    {
        ThrowHelper.ThrowIfNull(json);
        ThrowHelper.ThrowIfNull(seriesKey);

        using JsonDocument document = ParseDocument(json, request);
        JsonElement root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("CompactData", out JsonElement compactData)
            || compactData.ValueKind != JsonValueKind.Object
            || !compactData.TryGetProperty("DataSet", out JsonElement dataSet)
            || dataSet.ValueKind != JsonValueKind.Object
            || !dataSet.TryGetProperty("Series", out JsonElement series))
        {
            throw NoData(request);
        }

        var observations = new List<RateObservation>();

        foreach (JsonElement seriesElement in EnumerateNodes(series))
            ReadObservations(seriesElement, request, observations);

        ImfSeriesInfo info = new(request.Pair, seriesKey);

        return new PairRateData<ImfSeriesInfo>(request.Pair, observations, info);
    }

    /// <summary>
    /// Reads the in-range observations of a single series into the accumulator.
    /// </summary>
    /// <param name="seriesElement">The series object.</param>
    /// <param name="request">The originating request, supplying the inclusive date range.</param>
    /// <param name="observations">The accumulator to append matching observations to.</param>
    private static void ReadObservations(JsonElement seriesElement, CurrencyPairRequest request, List<RateObservation> observations)
    {
        if (seriesElement.ValueKind != JsonValueKind.Object
            || !seriesElement.TryGetProperty("Obs", out JsonElement obs))
        {
            return;
        }

        foreach (JsonElement observation in EnumerateNodes(obs))
        {
            if (observation.ValueKind == JsonValueKind.Object
                && TryReadPeriod(observation, out DateOnly date)
                && date >= request.StartDate && date <= request.EndDate
                && TryReadValue(observation, out decimal rate))
            {
                observations.Add(new RateObservation(date, rate));
            }
        }
    }

    /// <summary>
    /// Enumerates an SDMX node that may be a single object or an array of objects, yielding each object element.
    /// </summary>
    /// <param name="node">The node to enumerate.</param>
    /// <returns>The object elements the node contains.</returns>
    private static IEnumerable<JsonElement> EnumerateNodes(JsonElement node)
    {
        if (node.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement element in node.EnumerateArray())
                yield return element;
        }
        else if (node.ValueKind == JsonValueKind.Object)
        {
            yield return node;
        }
    }

    /// <summary>
    /// Reads an observation's <c>@TIME_PERIOD</c> as the first day of its <c>YYYY-MM</c> month.
    /// </summary>
    /// <param name="observation">The observation object.</param>
    /// <param name="date">When this method returns <see langword="true" />, the month-start date.</param>
    /// <returns><see langword="true" /> when a month period was read; otherwise <see langword="false" />.</returns>
    private static bool TryReadPeriod(JsonElement observation, out DateOnly date)
    {
        date = default;

        return observation.TryGetProperty("@TIME_PERIOD", out JsonElement period)
            && period.ValueKind == JsonValueKind.String
            && DateOnly.TryParseExact(period.GetString(), "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    /// <summary>
    /// Reads a strictly positive observation value from an observation's <c>@OBS_VALUE</c>, tolerating a number or a
    /// numeric string.
    /// </summary>
    /// <param name="observation">The observation object.</param>
    /// <param name="rate">When this method returns <see langword="true" />, the positive value.</param>
    /// <returns><see langword="true" /> when a positive value was read; otherwise <see langword="false" />.</returns>
    private static bool TryReadValue(JsonElement observation, out decimal rate)
    {
        rate = 0m;

        if (!observation.TryGetProperty("@OBS_VALUE", out JsonElement value))
            return false;

        bool parsed = value.ValueKind switch
        {
            JsonValueKind.Number => value.TryGetDecimal(out rate),
            JsonValueKind.String => decimal.TryParse(value.GetString(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out rate),
            _ => false,
        };

        return parsed && rate > 0m;
    }

    /// <summary>
    /// Parses the response bytes into a <see cref="JsonDocument" />, translating malformed JSON into a format
    /// exception.
    /// </summary>
    /// <param name="json">The UTF-8 JSON response bytes.</param>
    /// <param name="request">The originating request, used for the error message.</param>
    /// <returns>The parsed document.</returns>
    /// <exception cref="ExchangeRateFormatException">Thrown when the bytes are not valid JSON.</exception>
    private static JsonDocument ParseDocument(byte[] json, CurrencyPairRequest request)
    {
        try
        {
            return JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new ExchangeRateFormatException(
                string.Format(CultureInfo.CurrentCulture, ImfResourceStrings.Format_Invalid_ImfNoData, request.Pair.From.ToString(), request.Pair.To.ToString()),
                ex);
        }
    }

    /// <summary>
    /// Builds the no-data format exception for a request.
    /// </summary>
    /// <param name="request">The originating request.</param>
    /// <returns>The exception to throw.</returns>
    private static ExchangeRateFormatException NoData(CurrencyPairRequest request) =>
        new(string.Format(CultureInfo.CurrentCulture, ImfResourceStrings.Format_Invalid_ImfNoData, request.Pair.From.ToString(), request.Pair.To.ToString()));
}
