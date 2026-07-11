// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImfResponseParser.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Parses an IMF SDMX-JSON data-message response into a <see cref="PairRateData{TSeries}" />.
/// </summary>
/// <remarks>
/// The SDMX-JSON message carries the observation-dimension time values under
/// <c>data.structures[].dimensions.observation</c> (an ordered list of <c>TIME_PERIOD</c> ids) and the observations
/// under <c>data.dataSets[].series[].observations</c>, keyed by the integer index into that time list; each observation
/// value is the first element of its array. Daily <c>TIME_PERIOD</c> ids are <c>YYYY-MM-DD</c>. Non-positive,
/// unparseable, or out-of-range observations are dropped. A response that is not valid JSON, or that omits the expected
/// data/structure path, is translated into an <see cref="ExchangeRateFormatException" />. The singular
/// <c>data.structure</c> and series-less <c>dataSets[].observations</c> shapes are tolerated for robustness.
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
    /// Thrown when the response is not valid JSON or omits the expected SDMX rate data.
    /// </exception>
    public static PairRateData<ImfSeriesInfo> Parse(byte[] json, CurrencyPairRequest request, string seriesKey)
    {
        ThrowHelper.ThrowIfNull(json);
        ThrowHelper.ThrowIfNull(seriesKey);

        using JsonDocument document = ParseDocument(json, request);
        JsonElement root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("data", out JsonElement data) || data.ValueKind != JsonValueKind.Object)
            throw NoData(request);

        IReadOnlyList<DateOnly?> periods = ReadPeriods(data);
        if (periods.Count == 0 || !data.TryGetProperty("dataSets", out JsonElement dataSets) || dataSets.ValueKind != JsonValueKind.Array)
            throw NoData(request);

        var observations = new List<RateObservation>();

        foreach (JsonElement dataSet in dataSets.EnumerateArray())
            ReadDataSet(dataSet, periods, request, observations);

        ImfSeriesInfo info = new(request.Pair, seriesKey);

        return new PairRateData<ImfSeriesInfo>(request.Pair, observations, info);
    }

    /// <summary>
    /// Reads the ordered observation-dimension <c>TIME_PERIOD</c> values into a date list, indexed by observation key.
    /// </summary>
    /// <param name="data">The <c>data</c> object.</param>
    /// <returns>
    /// The parsed dates; an entry is <see langword="null" /> when its id is not a <c>YYYY-MM-DD</c> date.
    /// </returns>
    private static IReadOnlyList<DateOnly?> ReadPeriods(JsonElement data)
    {
        if (!TryGetStructure(data, out JsonElement structure)
            || !structure.TryGetProperty("dimensions", out JsonElement dimensions)
            || !dimensions.TryGetProperty("observation", out JsonElement observationDimensions)
            || observationDimensions.ValueKind != JsonValueKind.Array
            || observationDimensions.GetArrayLength() == 0)
        {
            return Array.Empty<DateOnly?>();
        }

        JsonElement timeDimension = SelectTimeDimension(observationDimensions);
        if (!timeDimension.TryGetProperty("values", out JsonElement values) || values.ValueKind != JsonValueKind.Array)
            return Array.Empty<DateOnly?>();

        var periods = new List<DateOnly?>(values.GetArrayLength());
        foreach (JsonElement value in values.EnumerateArray())
        {
            periods.Add(
                value.ValueKind == JsonValueKind.Object
                && value.TryGetProperty("id", out JsonElement id)
                && id.ValueKind == JsonValueKind.String
                && DateOnly.TryParseExact(id.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date)
                    ? date
                    : null);
        }

        return periods;
    }

    /// <summary>
    /// Selects the structure element, tolerating both the <c>structures</c> array and the singular <c>structure</c>
    /// object.
    /// </summary>
    /// <param name="data">The <c>data</c> object.</param>
    /// <param name="structure">When this method returns <see langword="true" />, the structure object.</param>
    /// <returns><see langword="true" /> when a structure was found; otherwise <see langword="false" />.</returns>
    private static bool TryGetStructure(JsonElement data, out JsonElement structure)
    {
        if (data.TryGetProperty("structures", out JsonElement structures)
            && structures.ValueKind == JsonValueKind.Array
            && structures.GetArrayLength() > 0)
        {
            structure = structures[0];
            return true;
        }

        if (data.TryGetProperty("structure", out structure) && structure.ValueKind == JsonValueKind.Object)
            return true;

        structure = default;
        return false;
    }

    /// <summary>
    /// Selects the <c>TIME_PERIOD</c> observation dimension, falling back to the first observation dimension.
    /// </summary>
    /// <param name="observationDimensions">The observation-dimension array.</param>
    /// <returns>The selected dimension element.</returns>
    private static JsonElement SelectTimeDimension(JsonElement observationDimensions)
    {
        foreach (JsonElement dimension in observationDimensions.EnumerateArray())
        {
            if (dimension.TryGetProperty("id", out JsonElement id)
                && id.ValueKind == JsonValueKind.String
                && string.Equals(id.GetString(), "TIME_PERIOD", StringComparison.Ordinal))
            {
                return dimension;
            }
        }

        return observationDimensions[0];
    }

    /// <summary>
    /// Reads the in-range observations of a data set (its series, or a series-less observations map) into the
    /// accumulator.
    /// </summary>
    /// <param name="dataSet">The data-set object.</param>
    /// <param name="periods">The observation-index-to-date list.</param>
    /// <param name="request">The originating request, supplying the inclusive date range.</param>
    /// <param name="observations">The accumulator to append matching observations to.</param>
    private static void ReadDataSet(JsonElement dataSet, IReadOnlyList<DateOnly?> periods, CurrencyPairRequest request, List<RateObservation> observations)
    {
        if (dataSet.ValueKind != JsonValueKind.Object)
            return;

        if (dataSet.TryGetProperty("series", out JsonElement series) && series.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty seriesEntry in series.EnumerateObject())
            {
                if (seriesEntry.Value.TryGetProperty("observations", out JsonElement seriesObs))
                    ReadObservations(seriesObs, periods, request, observations);
            }
        }

        if (dataSet.TryGetProperty("observations", out JsonElement flatObs))
            ReadObservations(flatObs, periods, request, observations);
    }

    /// <summary>
    /// Reads an observations map (keyed by observation index) into the accumulator, resolving each date from
    /// <paramref name="periods" />.
    /// </summary>
    /// <param name="observationsElement">The observations object.</param>
    /// <param name="periods">The observation-index-to-date list.</param>
    /// <param name="request">The originating request, supplying the inclusive date range.</param>
    /// <param name="observations">The accumulator to append matching observations to.</param>
    private static void ReadObservations(JsonElement observationsElement, IReadOnlyList<DateOnly?> periods, CurrencyPairRequest request, List<RateObservation> observations)
    {
        if (observationsElement.ValueKind != JsonValueKind.Object)
            return;

        foreach (JsonProperty entry in observationsElement.EnumerateObject())
        {
            if (!int.TryParse(entry.Name, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index)
                || index < 0 || index >= periods.Count
                || periods[index] is not DateOnly date
                || date < request.StartDate || date > request.EndDate
                || !TryReadValue(entry.Value, out decimal rate))
            {
                continue;
            }

            observations.Add(new RateObservation(date, rate));
        }
    }

    /// <summary>
    /// Reads a strictly positive observation value — the first element of the observation array — tolerating a number
    /// or a numeric string.
    /// </summary>
    /// <param name="observation">The observation array.</param>
    /// <param name="rate">When this method returns <see langword="true" />, the positive value.</param>
    /// <returns><see langword="true" /> when a positive value was read; otherwise <see langword="false" />.</returns>
    private static bool TryReadValue(JsonElement observation, out decimal rate)
    {
        rate = 0m;

        if (observation.ValueKind != JsonValueKind.Array || observation.GetArrayLength() == 0)
            return false;

        JsonElement value = observation[0];
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
