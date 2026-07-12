// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixerResponseParser.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Parses a Fixer foreign-exchange JSON response into a <see cref="PairRateData{TSeries}" />.
/// </summary>
/// <remarks>
/// A Fixer response carries a <c>success</c> flag and, on success, a <c>base</c> currency and a <c>rates</c> object.
/// The time-series endpoint returns <c>rates</c> as a date-keyed map of per-symbol objects; the single-date endpoint
/// returns <c>rates</c> as a flat per-symbol object accompanied by a top-level <c>date</c>. Both shapes are supported.
/// A response with <c>success</c> set to <see langword="false" /> is translated into an
/// <see cref="ExchangeRateFormatException" /> carrying the reported error type.
/// </remarks>
internal static class FixerResponseParser
{
    /// <summary>
    /// Parses a Fixer response, restricting observations to the request's inclusive date range.
    /// </summary>
    /// <param name="json">The UTF-8 JSON response bytes.</param>
    /// <param name="request">The originating request, supplying the pair and date range.</param>
    /// <param name="quoteSymbol">
    /// The mapped quote-currency symbol whose rate is read from each <c>rates</c> entry.
    /// </param>
    /// <param name="options">The provider options used to build the series metadata.</param>
    /// <returns>The parsed, range-restricted observations and series metadata.</returns>
    /// <exception cref="ExchangeRateFormatException">
    /// Thrown when the response is not valid JSON, reports failure, or omits the expected rate data.
    /// </exception>
    public static PairRateData<FixerSeriesInfo> Parse(byte[] json, CurrencyPairRequest request, string quoteSymbol, FixerRateProviderOptions options)
    {
        ThrowHelper.ThrowIfNull(json);
        ThrowHelper.ThrowIfNull(quoteSymbol);
        ThrowHelper.ThrowIfNull(options);

        using JsonDocument document = ParseDocument(json, request);
        JsonElement root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
            throw NoData(request);

        ThrowOnError(root, request);

        if (!root.TryGetProperty("rates", out JsonElement rates) || rates.ValueKind != JsonValueKind.Object)
            throw NoData(request);

        var observations = new List<RateObservation>();

        foreach (JsonProperty entry in rates.EnumerateObject())
        {
            if (entry.Value.ValueKind == JsonValueKind.Object)
            {
                // Time-series shape: the property name is an ISO date and the value is a per-symbol object.
                if (DateOnly.TryParseExact(entry.Name, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date)
                    && date >= request.StartDate && date <= request.EndDate
                    && TryReadRate(entry.Value, quoteSymbol, out decimal rate))
                {
                    observations.Add(new RateObservation(date, rate));
                }
            }
            else if (string.Equals(entry.Name, quoteSymbol, StringComparison.Ordinal)
                && TryReadDecimal(entry.Value, out decimal singleRate))
            {
                // Single-date shape: the property name is the quote symbol; the date is the top-level "date".
                DateOnly date = ReadSingleDate(root, request);
                if (date >= request.StartDate && date <= request.EndDate)
                    observations.Add(new RateObservation(date, singleRate));
            }
        }

        string baseIsoCode = ReadBaseIsoCode(root, request.Pair.From.ToString());
        FixerSeriesInfo series = new(request.Pair, baseIsoCode, request.Pair.To.ToString());

        return new PairRateData<FixerSeriesInfo>(request.Pair, observations, series);
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
                string.Format(CultureInfo.CurrentCulture, FixerResourceStrings.Format_Invalid_FixerNoData, request.Pair.From.ToString(), request.Pair.To.ToString()),
                ex);
        }
    }

    /// <summary>
    /// Throws when the response reports failure through a <c>success</c> flag set to <see langword="false" />.
    /// </summary>
    /// <param name="root">The response root object.</param>
    /// <param name="request">The originating request, used for the error message.</param>
    /// <exception cref="ExchangeRateFormatException">Thrown when the response reports failure.</exception>
    private static void ThrowOnError(JsonElement root, CurrencyPairRequest request)
    {
        if (!root.TryGetProperty("success", out JsonElement success) || success.ValueKind != JsonValueKind.False)
            return;

        string type = root.TryGetProperty("error", out JsonElement error)
            && error.ValueKind == JsonValueKind.Object
            && error.TryGetProperty("type", out JsonElement typeElement)
            && typeElement.ValueKind == JsonValueKind.String
            ? typeElement.GetString() ?? string.Empty
            : string.Empty;

        throw new ExchangeRateFormatException(
            string.Format(CultureInfo.CurrentCulture, FixerResourceStrings.Format_Invalid_FixerError, request.Pair.From.ToString(), request.Pair.To.ToString(), type));
    }

    /// <summary>
    /// Reads a strictly positive rate for a symbol from a per-symbol object.
    /// </summary>
    /// <param name="rates">The per-symbol object.</param>
    /// <param name="symbol">The quote symbol to read.</param>
    /// <param name="rate">When this method returns <see langword="true" />, the positive rate.</param>
    /// <returns><see langword="true" /> when a positive rate was read; otherwise <see langword="false" />.</returns>
    private static bool TryReadRate(JsonElement rates, string symbol, out decimal rate)
    {
        rate = 0m;

        return rates.TryGetProperty(symbol, out JsonElement element) && TryReadDecimal(element, out rate);
    }

    /// <summary>
    /// Reads a strictly positive decimal from a JSON element, tolerating a number or numeric string.
    /// </summary>
    /// <param name="element">The value element.</param>
    /// <param name="rate">When this method returns <see langword="true" />, the positive value.</param>
    /// <returns><see langword="true" /> when a positive value was read; otherwise <see langword="false" />.</returns>
    private static bool TryReadDecimal(JsonElement element, out decimal rate)
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
    /// Reads the single-date response's top-level <c>date</c>, falling back to the request start date when absent.
    /// </summary>
    /// <param name="root">The response root object.</param>
    /// <param name="request">The originating request, supplying the fallback date.</param>
    /// <returns>The observation date.</returns>
    private static DateOnly ReadSingleDate(JsonElement root, CurrencyPairRequest request) =>
        root.TryGetProperty("date", out JsonElement date)
        && date.ValueKind == JsonValueKind.String
        && DateOnly.TryParseExact(date.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsed)
            ? parsed
            : request.StartDate;

    /// <summary>
    /// Reads the response's base-currency code, falling back to the requested source code when absent.
    /// </summary>
    /// <param name="root">The response root object.</param>
    /// <param name="fallback">The requested source ISO code to use when the response omits the base.</param>
    /// <returns>The base-currency ISO code.</returns>
    private static string ReadBaseIsoCode(JsonElement root, string fallback) =>
        root.TryGetProperty("base", out JsonElement element)
        && element.ValueKind == JsonValueKind.String
            ? element.GetString() ?? fallback
            : fallback;

    /// <summary>
    /// Builds the no-data format exception for a request.
    /// </summary>
    /// <param name="request">The originating request.</param>
    /// <returns>The exception to throw.</returns>
    private static ExchangeRateFormatException NoData(CurrencyPairRequest request) =>
        new(string.Format(CultureInfo.CurrentCulture, FixerResourceStrings.Format_Invalid_FixerNoData, request.Pair.From.ToString(), request.Pair.To.ToString()));
}
