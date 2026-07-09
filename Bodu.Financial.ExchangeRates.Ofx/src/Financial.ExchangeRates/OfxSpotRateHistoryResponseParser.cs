// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OfxSpotRateHistoryResponseParser.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Parses an OFX spot-rate-history JSON response into a <see cref="PairRateData{TSeries}" />.
/// </summary>
/// <remarks>
/// The response carries a <c>HistoricalPoints</c> array; each point pairs a <c>PointInTime</c> (a Unix timestamp in
/// milliseconds, interpreted in UTC) with an <c>InterbankRate</c> (the source-to-destination multiplier). OFX returns a
/// server-determined window, so points outside the request's inclusive range are skipped, as are points with a missing
/// or non-positive rate. Numeric members are tolerated as JSON numbers or numeric strings.
/// </remarks>
internal static class OfxSpotRateHistoryResponseParser
{
    /// <summary>
    /// Parses a spot-rate-history response, restricting observations to the request's inclusive date range.
    /// </summary>
    /// <param name="json">The UTF-8 JSON response bytes.</param>
    /// <param name="request">The originating request, supplying the pair and date range.</param>
    /// <param name="options">The provider options used to build the series metadata.</param>
    /// <returns>The parsed, range-restricted observations and series metadata.</returns>
    /// <exception cref="ExchangeRateFormatException">
    /// Thrown when the response is not valid JSON or omits the expected <c>HistoricalPoints</c> array.
    /// </exception>
    public static PairRateData<OfxSeriesInfo> Parse(byte[] json, CurrencyPairRequest request, OfxExchangeRateOptions options)
    {
        ThrowHelper.ThrowIfNull(json);
        ThrowHelper.ThrowIfNull(options);

        using JsonDocument document = ParseDocument(json, request);
        JsonElement root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("HistoricalPoints", out JsonElement points)
            || points.ValueKind != JsonValueKind.Array)
        {
            throw NoData(request);
        }

        var observations = new List<RateObservation>(points.GetArrayLength());

        foreach (JsonElement point in points.EnumerateArray())
        {
            if (!TryReadUnixMilliseconds(point, out long unixMs) || !TryReadRate(point, out decimal rate))
                continue;

            // Skip a timestamp outside the range representable by DateTimeOffset rather than crashing on the
            // ArgumentOutOfRangeException it would raise (FromUnixTimeMilliseconds accepts this window only).
            if (unixMs is < -62_135_596_800_000 or > 253_402_300_799_999)
                continue;

            var date = DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeMilliseconds(unixMs).UtcDateTime);
            if (date < request.StartDate || date > request.EndDate)
                continue;

            observations.Add(new RateObservation(date, rate));
        }

        OfxSeriesInfo series = new(request.Pair, request.Pair.To.ToString());
        return new PairRateData<OfxSeriesInfo>(request.Pair, observations, series);
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
                string.Format(CultureInfo.CurrentCulture, OfxResourceStrings.Format_Invalid_OfxNoData, request.Pair.From.ToString(), request.Pair.To.ToString()),
                ex);
        }
    }

    /// <summary>
    /// Reads the <c>PointInTime</c> Unix-millisecond timestamp from a point, tolerating a JSON number or numeric
    /// string.
    /// </summary>
    /// <param name="point">The historical-point element.</param>
    /// <param name="unixMs">When this method returns <see langword="true" />, the Unix-millisecond timestamp.</param>
    /// <returns><see langword="true" /> when a timestamp was read; otherwise <see langword="false" />.</returns>
    private static bool TryReadUnixMilliseconds(JsonElement point, out long unixMs)
    {
        unixMs = 0;

        if (!point.TryGetProperty("PointInTime", out JsonElement element))
            return false;

        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetInt64(out unixMs),
            JsonValueKind.String => long.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out unixMs),
            _ => false,
        };
    }

    /// <summary>
    /// Reads the strictly positive <c>InterbankRate</c> from a point, tolerating a JSON number or numeric string.
    /// </summary>
    /// <param name="point">The historical-point element.</param>
    /// <param name="rate">When this method returns <see langword="true" />, the positive rate.</param>
    /// <returns><see langword="true" /> when a positive rate was read; otherwise <see langword="false" />.</returns>
    private static bool TryReadRate(JsonElement point, out decimal rate)
    {
        rate = 0m;

        if (!point.TryGetProperty("InterbankRate", out JsonElement element))
            return false;

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
    private static ExchangeRateFormatException NoData(CurrencyPairRequest request) =>
        new(string.Format(CultureInfo.CurrentCulture, OfxResourceStrings.Format_Invalid_OfxNoData, request.Pair.From.ToString(), request.Pair.To.ToString()));
}
