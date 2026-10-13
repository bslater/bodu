// ---------------------------------------------------------------------------------------------------------------
// <copyright file="JsonRateValueReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Pure invariant JSON scalar readers shared by rate-feed parsers that accept numbers and numeric strings.
/// </summary>
internal static class JsonRateValueReader
{
    /// <summary>
    /// Reads a decimal of any sign from a JSON number or invariant numeric string.
    /// </summary>
    /// <param name="element">The JSON value to read.</param>
    /// <param name="value">Receives the parsed decimal value when the input is numeric; otherwise zero.</param>
    /// <returns>
    /// <see langword="true" /> when the JSON value is a number that fits a <see cref="decimal" />, or a string that
    /// parses as one under the invariant culture; otherwise <see langword="false" />. A value of any other kind (<c>null</c>,
    /// a boolean, an object or an array) is not numeric, and reading it never throws.
    /// </returns>
    internal static bool TryReadDecimal(JsonElement element, out decimal value)
    {
        value = 0m;
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetDecimal(out value),
            JsonValueKind.String => decimal.TryParse(element.GetString(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value),
            _ => false,
        };
    }

    /// <summary>
    /// Reads a strictly positive decimal from a JSON number or invariant numeric string.
    /// </summary>
    /// <param name="element">The JSON value to read.</param>
    /// <param name="rate">Receives the parsed decimal value when the input is numeric.</param>
    /// <returns>
    /// <see langword="true" /> when the JSON value represents a strictly positive decimal; otherwise,
    /// <see langword="false" />.
    /// </returns>
    internal static bool TryReadPositiveDecimal(JsonElement element, out decimal rate) =>
        TryReadDecimal(element, out rate) && rate > 0m;

    /// <summary>
    /// Reads a 64-bit integer from a JSON number or invariant integer string.
    /// </summary>
    /// <param name="element">The JSON value to read.</param>
    /// <param name="value">Receives the parsed integer when parsing succeeds; otherwise zero.</param>
    /// <returns>
    /// <see langword="true" /> when the JSON value is an integral number that fits a <see cref="long" />, or a string
    /// that parses as one under the invariant culture; otherwise <see langword="false" />. A value of any other kind
    /// never throws.
    /// </returns>
    internal static bool TryReadInt64(JsonElement element, out long value)
    {
        value = 0;
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetInt64(out value),
            JsonValueKind.String => long.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value),
            _ => false,
        };
    }

    /// <summary>
    /// Reads an integer Unix-millisecond timestamp from a JSON number or invariant numeric string.
    /// </summary>
    /// <param name="element">The JSON value to read.</param>
    /// <param name="timestamp">Receives the parsed Unix-millisecond timestamp when parsing succeeds.</param>
    /// <returns>
    /// <see langword="true" /> when the JSON value can be parsed as a 64-bit integer; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Conversion to a valid <see cref="DateTimeOffset" /> remains the caller's responsibility.
    /// </remarks>
    internal static bool TryReadUnixMilliseconds(JsonElement element, out long timestamp) =>
        TryReadInt64(element, out timestamp);

    /// <summary>
    /// Reads an integer Unix-millisecond timestamp from a JSON number or invariant numeric string as the UTC date it
    /// falls on.
    /// </summary>
    /// <param name="element">The JSON value to read.</param>
    /// <param name="date">
    /// Receives the UTC calendar date of the timestamp when reading succeeds; otherwise <see langword="default" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the JSON value is an integer timestamp within the range a
    /// <see cref="DateTimeOffset" /> represents (the years 1 to 9999); otherwise <see langword="false" />. A timestamp
    /// outside that range, which <see cref="DateTimeOffset.FromUnixTimeMilliseconds(long)" /> rejects, is not a date,
    /// and reading it never throws.
    /// </returns>
    internal static bool TryReadUnixMillisecondsDate(JsonElement element, out DateOnly date)
    {
        throw new NotImplementedException();
    }
}
