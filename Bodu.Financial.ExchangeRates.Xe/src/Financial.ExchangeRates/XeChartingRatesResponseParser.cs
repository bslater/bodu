// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XeChartingRatesResponseParser.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Parses an XE.com charting-rates JSON response into a <see cref="PairRateData{TSeries}" />.
/// </summary>
/// <remarks>
/// <para>
/// The response carries a <c>batchList</c> array; the first batch supplies a <c>startTime</c> (a Unix timestamp in
/// milliseconds, interpreted in UTC), an <c>interval</c> (the spacing between successive points, in milliseconds), and
/// a <c>rates</c> array. XE encodes the series as deltas from a baseline carried in the first element of the
/// <c>rates</c> array: the published rate for each subsequent point is <c>(value - baseline)</c>, rounded the way the
/// XE site rounds it. The first decoded point is dated at <c>startTime</c>, and each subsequent point advances by
/// <c>interval</c>.
/// </para>
/// <para>
/// XE returns a server-determined window, so points outside the request's inclusive range are skipped, as are
/// non-positive rates. When the window has sub-daily granularity, multiple points can map to the same calendar day;
/// because the points are emitted in chronological order and the accumulator upserts by date, the last point of each
/// day is the one retained.
/// </para>
/// </remarks>
internal static class XeChartingRatesResponseParser
{
    /// <summary>The vanishingly small constant the XE site folds into its rounding expression. It is below the precision of <see cref="decimal" /> and therefore contributes nothing to the result; it is preserved verbatim so the decode matches the site's published formula exactly.</summary>
    private const decimal Epsilon = 4.94065645841247E-324m;

    /// <summary>
    /// Parses a charting-rates response, restricting observations to the request's inclusive date range.
    /// </summary>
    /// <param name="json">The UTF-8 JSON response bytes.</param>
    /// <param name="request">The originating request, supplying the pair and date range.</param>
    /// <param name="options">The provider options used to build the series metadata.</param>
    /// <returns>The parsed, range-restricted observations and series metadata.</returns>
    /// <exception cref="ExchangeRateFormatException">
    /// Thrown when the response is not valid JSON or omits the expected charting-rate data.
    /// </exception>
    public static PairRateData<XeSeriesInfo> Parse(byte[] json, CurrencyPairRequest request, XeRateProviderOptions options)
    {
        ThrowHelper.ThrowIfNull(json);
        ThrowHelper.ThrowIfNull(options);

        using JsonDocument document = ParseDocument(json, request);
        JsonElement root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("batchList", out JsonElement batchList)
            || batchList.ValueKind != JsonValueKind.Array
            || batchList.GetArrayLength() == 0)
        {
            throw NoData(request);
        }

        JsonElement batch = batchList[0];

        if (!TryGetInt64(batch, "startTime", out long startTimeMs)
            || !TryGetInt64(batch, "interval", out long intervalMs)
            || !batch.TryGetProperty("rates", out JsonElement rates)
            || rates.ValueKind != JsonValueKind.Array
            || rates.GetArrayLength() == 0
            || !rates[0].TryGetDecimal(out decimal baseline))
        {
            throw NoData(request);
        }

        int count = rates.GetArrayLength();
        var observations = new List<RateObservation>(count);

        // An attacker-controlled startTime or interval can drive the timestamp arithmetic out of the representable
        // DateTime/TimeSpan range (ArgumentOutOfRangeException / OverflowException). Map any such out-of-range value
        // to the documented no-data failure rather than letting it crash the fetch.
        try
        {
            DateTime point = DateTimeOffset.FromUnixTimeMilliseconds(startTimeMs).UtcDateTime;
            var interval = TimeSpan.FromMilliseconds(intervalMs);

            // The first element is the baseline offset, not a data point; the published series starts at the second element.
            for (int i = 1; i < count; i++)
            {
                if (rates[i].TryGetDecimal(out decimal value))
                {
                    decimal rate = DecodeRate(value, baseline);
                    var date = DateOnly.FromDateTime(point);

                    if (rate > 0m && date >= request.StartDate && date <= request.EndDate)
                        observations.Add(new RateObservation(date, rate));
                }

                point += interval;
            }
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or OverflowException)
        {
            throw NoData(request);
        }

        XeSeriesInfo series = new(request.Pair, request.Pair.To.ToString());
        return new PairRateData<XeSeriesInfo>(request.Pair, observations, series);
    }

    /// <summary>
    /// Decodes a single delta-encoded value into its published rate using the same rounding the XE site applies.
    /// </summary>
    /// <param name="value">The delta-encoded value read from the <c>rates</c> array.</param>
    /// <param name="baseline">The baseline offset carried in the first <c>rates</c> element.</param>
    /// <returns>The decoded rate, rounded to six decimal places.</returns>
    private static decimal DecodeRate(decimal value, decimal baseline) =>
        Math.Round(Math.Round((value - baseline + Epsilon) * 10000000000m) / 10000000000m, 6);

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
                string.Format(CultureInfo.CurrentCulture, XeResourceStrings.Format_Invalid_XeResponse, request.Pair.From.ToString(), request.Pair.To.ToString()),
                ex);
        }
    }

    /// <summary>
    /// Reads a named <see cref="long" /> member from a batch element.
    /// </summary>
    /// <param name="batch">The batch element.</param>
    /// <param name="propertyName">The member name to read.</param>
    /// <param name="value">When this method returns <see langword="true" />, the parsed value.</param>
    /// <returns>
    /// <see langword="true" /> when the member was present and numeric; otherwise <see langword="false" />.
    /// </returns>
    private static bool TryGetInt64(JsonElement batch, string propertyName, out long value)
    {
        value = 0;

        return batch.TryGetProperty(propertyName, out JsonElement element)
            && element.ValueKind == JsonValueKind.Number
            && element.TryGetInt64(out value);
    }

    /// <summary>
    /// Builds the no-data format exception for a request.
    /// </summary>
    /// <param name="request">The originating request.</param>
    /// <returns>The exception to throw.</returns>
    private static ExchangeRateFormatException NoData(CurrencyPairRequest request) =>
        new(string.Format(CultureInfo.CurrentCulture, XeResourceStrings.Format_Invalid_XeNoData, request.Pair.From.ToString(), request.Pair.To.ToString()));
}
