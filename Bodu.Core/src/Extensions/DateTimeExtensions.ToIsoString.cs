// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.ToIsoString.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

using System.Globalization;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Converts the specified <see cref="DateTime"/> to an ISO 8601 formatted string, using its <see cref="DateTime.Kind"/> to determine
    /// the appropriate format suffix.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime"/> value to convert.</param>
    /// <returns>
    /// A string representation of <paramref name="dateTime"/> in ISO 8601 format:
    /// <list type="bullet">
    /// <item>
    /// <description>Ends with <c>'Z'</c> if <see cref="DateTime.Kind"/> is <see cref="DateTimeKind.Utc"/>.</description>
    /// </item>
    /// <item>
    /// <description>Includes local time zone offset if <see cref="DateTimeKind.Local"/>.</description>
    /// </item>
    /// <item>
    /// <description>Omits offset for <see cref="DateTimeKind.Unspecified"/>.</description>
    /// </item>
    /// </list>
    /// </returns>
    /// <remarks>Uses the "o" (round-trip) format for <c>Local</c> and <c>Unspecified</c>, and a custom UTC format for <c>Utc</c>.</remarks>
    public static string ToIsoString(this DateTime dateTime) => dateTime.Kind switch
    {
        DateTimeKind.Utc => dateTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture),
        DateTimeKind.Local => dateTime.ToString("o", CultureInfo.InvariantCulture),
        _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified).ToString("o", CultureInfo.InvariantCulture),
    };

    /// <summary>
    /// Converts the specified <see cref="DateTime"/> to an ISO 8601 formatted string, optionally omitting fractional seconds.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime"/> value to convert.</param>
    /// <param name="includeFractionalSeconds">Indicates whether to include fractional seconds (7 digits) in the output.</param>
    /// <returns>A string representation of <paramref name="dateTime"/> in ISO 8601 format, with or without fractional seconds.</returns>
    /// <remarks>Uses the "o" (round-trip) format when <paramref name="includeFractionalSeconds"/> is <see langword="true"/>; otherwise, uses "yyyy-MM-ddTHH:mm:ss".</remarks>
    public static string ToIsoString(this DateTime dateTime, bool includeFractionalSeconds)
    {
        string format = includeFractionalSeconds ? "o" : "yyyy-MM-ddTHH:mm:ss";
        return dateTime.ToString(format, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Converts the specified <see cref="DateTime"/> to an ISO 8601 formatted string, using an explicit <see cref="DateTimeKind"/> override.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime"/> to convert.</param>
    /// <param name="kind">The <see cref="DateTimeKind"/> to apply before formatting.</param>
    /// <returns>
    /// A string representation of the input <see cref="DateTime"/> in ISO 8601 format:
    /// <list type="bullet">
    /// <item>
    /// <description><see cref="DateTimeKind.Utc"/> → Ends with <c>'Z'</c>.</description>
    /// </item>
    /// <item>
    /// <description><see cref="DateTimeKind.Local"/> → Includes local time zone offset.</description>
    /// </item>
    /// <item>
    /// <description><see cref="DateTimeKind.Unspecified"/> → No offset information.</description>
    /// </item>
    /// </list>
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="kind"/> is not a valid value of <see cref="DateTimeKind"/>.</exception>
    public static string ToIsoString(this DateTime dateTime, DateTimeKind kind) => kind switch
    {
        DateTimeKind.Utc => dateTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture),
        DateTimeKind.Local => dateTime.ToLocalTime().ToString("o", CultureInfo.InvariantCulture),
        DateTimeKind.Unspecified => DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified).ToString("o", CultureInfo.InvariantCulture),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), "Invalid DateTimeKind value.")
    };

    /// <summary>
    /// Converts the specified <see cref="DateTime"/> to a string using a custom format and optional culture.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime"/> value to format.</param>
    /// <param name="format">A valid date-time format string (e.g., "yyyy-MM-ddTHH:mm:ss").</param>
    /// <param name="culture">An optional <see cref="CultureInfo"/> used for culture-specific formatting. If <see langword="null"/>, <see cref="CultureInfo.InvariantCulture"/> is used.</param>
    /// <returns>A formatted string representation of <paramref name="dateTime"/> using the specified format and culture.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="format"/> is <see langword="null"/> or empty.</exception>
    public static string ToIsoString(this DateTime dateTime, string format, CultureInfo? culture = null)
    {
        if (string.IsNullOrWhiteSpace(format))
            throw new ArgumentNullException(nameof(format), "Format string must be specified.");

        return dateTime.ToString(format, culture ?? CultureInfo.InvariantCulture);
    }
}
