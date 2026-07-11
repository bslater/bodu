// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FredResponseParser.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Parses a FRED series-observations JSON response into a <see cref="PairRateData{TSeries}" />.
/// </summary>
/// <remarks>
/// A FRED observations response carries an <c>observations</c> array whose elements each expose a <c>date</c> and a
/// <c>value</c>. FRED encodes a missing value as the string <c>"."</c>, which is skipped. Values are parsed as decimals
/// and non-positive values are dropped. A response that is not valid JSON, or that omits the <c>observations</c> array,
/// is translated into an <see cref="ExchangeRateFormatException" />.
/// </remarks>
internal static class FredResponseParser
{
    /// <summary>
    /// Parses a FRED observations response, restricting observations to the request's inclusive date range.
    /// </summary>
    /// <param name="json">The UTF-8 JSON response bytes.</param>
    /// <param name="request">The originating request, supplying the pair and date range.</param>
    /// <param name="seriesId">The FRED series identifier used to build the series metadata.</param>
    /// <returns>The parsed, range-restricted observations and series metadata.</returns>
    /// <exception cref="ExchangeRateFormatException">
    /// Thrown when the response is not valid JSON or omits the expected observations array.
    /// </exception>
    public static PairRateData<FredSeriesInfo> Parse(byte[] json, CurrencyPairRequest request, string seriesId)
    {
        ThrowHelper.ThrowIfNull(json);
        ThrowHelper.ThrowIfNull(seriesId);

        using JsonDocument document = ParseDocument(json, request);
        JsonElement root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("observations", out JsonElement observationsElement)
            || observationsElement.ValueKind != JsonValueKind.Array)
        {
            throw NoData(request);
        }

        var observations = new List<RateObservation>();

        foreach (JsonElement observation in observationsElement.EnumerateArray())
        {
            if (observation.ValueKind != JsonValueKind.Object)
                continue;

            if (!observation.TryGetProperty("date", out JsonElement dateElement)
                || dateElement.ValueKind != JsonValueKind.String
                || !DateOnly.TryParseExact(dateElement.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date))
            {
                continue;
            }

            if (date < request.StartDate || date > request.EndDate)
                continue;

            if (TryReadValue(observation, out decimal rate))
                observations.Add(new RateObservation(date, rate));
        }

        FredSeriesInfo series = new(request.Pair, seriesId);

        return new PairRateData<FredSeriesInfo>(request.Pair, observations, series);
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
                string.Format(CultureInfo.CurrentCulture, FredResourceStrings.Format_Invalid_FredNoData, request.Pair.From.ToString(), request.Pair.To.ToString()),
                ex);
        }
    }

    /// <summary>
    /// Reads a strictly positive value from an observation, skipping FRED's <c>"."</c> missing-value marker.
    /// </summary>
    /// <param name="observation">The observation object.</param>
    /// <param name="rate">When this method returns <see langword="true" />, the positive value.</param>
    /// <returns><see langword="true" /> when a positive value was read; otherwise <see langword="false" />.</returns>
    private static bool TryReadValue(JsonElement observation, out decimal rate)
    {
        rate = 0m;

        if (!observation.TryGetProperty("value", out JsonElement valueElement) || valueElement.ValueKind != JsonValueKind.String)
            return false;

        string? text = valueElement.GetString();
        if (string.IsNullOrEmpty(text) || string.Equals(text, ".", StringComparison.Ordinal))
            return false;

        return decimal.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out rate) && rate > 0m;
    }

    /// <summary>
    /// Builds the no-data format exception for a request.
    /// </summary>
    /// <param name="request">The originating request.</param>
    /// <returns>The exception to throw.</returns>
    private static ExchangeRateFormatException NoData(CurrencyPairRequest request) =>
        new(string.Format(CultureInfo.CurrentCulture, FredResourceStrings.Format_Invalid_FredNoData, request.Pair.From.ToString(), request.Pair.To.ToString()));
}
