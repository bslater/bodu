// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YahooChartResponseParser.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Parses a Yahoo Finance <c>v8/finance/chart</c> JSON response into a <see cref="YahooExchangeRateChart" />.
/// </summary>
/// <remarks>
/// The chart response carries a parallel <c>timestamp</c> array and an <c>indicators.quote[0].close</c> array. Days
/// with no published close are emitted as <see langword="null" /> and are skipped. Timestamps are Unix seconds at the
/// start of each bar and are interpreted in UTC.
/// </remarks>
internal static class YahooChartResponseParser
{
    /// <summary>
    /// Parses a chart response, restricting observations to the request's inclusive date range.
    /// </summary>
    /// <param name="json">The UTF-8 JSON response bytes.</param>
    /// <param name="request">The originating request, supplying the pair and date range.</param>
    /// <param name="options">The provider options (reserved for future response-shaping behaviour).</param>
    /// <returns>The parsed chart.</returns>
    /// <exception cref="ExchangeRateFormatException">
    /// Thrown when the response is not valid JSON, carries a chart error, or omits the expected chart data.
    /// </exception>
    public static YahooExchangeRateChart Parse(byte[] json, YahooChartRequest request, YahooExchangeRateOptions options)
    {
        ThrowHelper.ThrowIfNull(json);
        ThrowHelper.ThrowIfNull(request);
        ThrowHelper.ThrowIfNull(options);

        using JsonDocument document = ParseDocument(json, request);
        JsonElement root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("chart", out JsonElement chart))
            throw NoData(request);

        ThrowOnChartError(chart, request);

        if (!chart.TryGetProperty("result", out JsonElement result)
            || result.ValueKind != JsonValueKind.Array
            || result.GetArrayLength() == 0)
        {
            throw NoData(request);
        }

        JsonElement first = result[0];

        if (!first.TryGetProperty("timestamp", out JsonElement timestamps) || timestamps.ValueKind != JsonValueKind.Array)
            throw NoData(request);

        if (!TryGetCloseArray(first, out JsonElement closes))
            throw NoData(request);

        string quoteIsoCode = ReadQuoteIsoCode(first, request.Pair.To.ToString());

        int count = Math.Min(timestamps.GetArrayLength(), closes.GetArrayLength());
        var observations = new List<ExchangeRateObservation>(count);

        for (int i = 0; i < count; i++)
        {
            JsonElement close = closes[i];
            if (close.ValueKind is JsonValueKind.Null)
                continue;

            if (!close.TryGetDecimal(out decimal rate) || rate <= 0m)
                continue;

            // A non-numeric or out-of-range Unix timestamp must be skipped like any other malformed point, rather
            // than crashing the fetch with an InvalidOperationException / ArgumentOutOfRangeException.
            if (!timestamps[i].TryGetInt64(out long unixSeconds) || !TryUnixSecondsToDate(unixSeconds, out DateOnly date))
                continue;

            if (date < request.StartDate || date > request.EndDate)
                continue;

            observations.Add(new ExchangeRateObservation(date, rate));
        }

        return new YahooExchangeRateChart(request.Pair, request.Symbol, quoteIsoCode, observations);
    }

    /// <summary>
    /// Parses the response bytes into a <see cref="JsonDocument" />, translating malformed JSON into a format
    /// exception.
    /// </summary>
    /// <param name="json">The UTF-8 JSON response bytes.</param>
    /// <param name="request">The originating request, used for the error message.</param>
    /// <returns>The parsed document.</returns>
    /// <exception cref="ExchangeRateFormatException">Thrown when the bytes are not valid JSON.</exception>
    private static JsonDocument ParseDocument(byte[] json, YahooChartRequest request)
    {
        try
        {
            return JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new ExchangeRateFormatException(
                string.Format(CultureInfo.CurrentCulture, YahooResourceStrings.Format_Invalid_YahooNoData, request.Symbol),
                ex);
        }
    }

    /// <summary>
    /// Throws when the chart object carries a non-null <c>error</c> member.
    /// </summary>
    /// <param name="chart">The <c>chart</c> element.</param>
    /// <param name="request">The originating request, used for the error message.</param>
    /// <exception cref="ExchangeRateFormatException">Thrown when an error is present.</exception>
    private static void ThrowOnChartError(JsonElement chart, YahooChartRequest request)
    {
        if (!chart.TryGetProperty("error", out JsonElement error) || error.ValueKind is JsonValueKind.Null)
            return;

        string description = error.TryGetProperty("description", out JsonElement element) && element.ValueKind == JsonValueKind.String
            ? element.GetString() ?? string.Empty
            : string.Empty;

        throw new ExchangeRateFormatException(
            string.Format(CultureInfo.CurrentCulture, YahooResourceStrings.Format_Invalid_YahooChartError, request.Symbol, description));
    }

    /// <summary>
    /// Attempts to locate the <c>indicators.quote[0].close</c> array within a chart result.
    /// </summary>
    /// <param name="result">The first chart result element.</param>
    /// <param name="closes">When this method returns <see langword="true" />, the close array.</param>
    /// <returns><see langword="true" /> when the close array was found; otherwise <see langword="false" />.</returns>
    private static bool TryGetCloseArray(JsonElement result, out JsonElement closes)
    {
        closes = default;

        if (!result.TryGetProperty("indicators", out JsonElement indicators)
            || !indicators.TryGetProperty("quote", out JsonElement quote)
            || quote.ValueKind != JsonValueKind.Array
            || quote.GetArrayLength() == 0
            || !quote[0].TryGetProperty("close", out JsonElement close)
            || close.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        closes = close;
        return true;
    }

    /// <summary>
    /// Converts a Unix timestamp in seconds to a UTC date, returning <see langword="false" /> when the value lies
    /// outside the range representable by <see cref="DateTimeOffset" />.
    /// </summary>
    /// <param name="unixSeconds">The Unix timestamp, in seconds.</param>
    /// <param name="date">When this method returns, the corresponding UTC date when conversion succeeded.</param>
    /// <returns><see langword="true" /> when the timestamp was converted; otherwise <see langword="false" />.</returns>
    private static bool TryUnixSecondsToDate(long unixSeconds, out DateOnly date)
    {
        // DateTimeOffset.FromUnixTimeSeconds accepts -62,135,596,800 .. 253,402,300,799; anything else is malformed.
        if (unixSeconds is < -62_135_596_800 or > 253_402_300_799)
        {
            date = default;
            return false;
        }

        date = DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime);
        return true;
    }

    /// <summary>
    /// Reads the quote-currency code from the chart meta, falling back to the requested code when absent.
    /// </summary>
    /// <param name="result">The first chart result element.</param>
    /// <param name="fallback">The requested quote ISO code to use when meta omits the currency.</param>
    /// <returns>The quote-currency ISO code.</returns>
    private static string ReadQuoteIsoCode(JsonElement result, string fallback) =>
        result.TryGetProperty("meta", out JsonElement meta)
        && meta.TryGetProperty("currency", out JsonElement currency)
        && currency.ValueKind == JsonValueKind.String
            ? currency.GetString() ?? fallback
            : fallback;

    /// <summary>
    /// Builds the no-data format exception for a request.
    /// </summary>
    /// <param name="request">The originating request.</param>
    /// <returns>The exception to throw.</returns>
    private static ExchangeRateFormatException NoData(YahooChartRequest request) =>
        new(string.Format(CultureInfo.CurrentCulture, YahooResourceStrings.Format_Invalid_YahooNoData, request.Symbol));
}
