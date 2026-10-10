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
    /// Reads a strictly positive decimal from a JSON number or invariant numeric string.
    /// </summary>
    /// <param name="element">The JSON value to read.</param>
    /// <param name="rate">Receives the parsed decimal value when the input is numeric.</param>
    /// <returns>
    /// <see langword="true" /> when the JSON value represents a strictly positive decimal; otherwise,
    /// <see langword="false" />.
    /// </returns>
    internal static bool TryReadPositiveDecimal(JsonElement element, out decimal rate)
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
    internal static bool TryReadUnixMilliseconds(JsonElement element, out long timestamp)
    {
        timestamp = 0;
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetInt64(out timestamp),
            JsonValueKind.String => long.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out timestamp),
            _ => false,
        };
    }
}
