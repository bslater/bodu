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
    /// Returns a string representation of the specified <paramref name="dateTime"/> in ISO 8601 format, using its <see cref="DateTime.Kind"/> to determine the appropriate format suffix.
    /// </summary>
    /// <param name="dateTime">The date and time value to convert.</param>
    /// <returns>
    /// A <see cref="string"/> representation of <paramref name="dateTime"/> in ISO 8601 format:
    /// <list type="bullet">
    /// <item><description>ends with <c>'Z'</c> if <see cref="DateTime.Kind"/> is <see cref="DateTimeKind.Utc"/>;</description></item>
    /// <item><description>includes the local time zone offset if <see cref="DateTime.Kind"/> is <see cref="DateTimeKind.Local"/>;</description></item>
    /// <item><description>omits any offset if <see cref="DateTime.Kind"/> is <see cref="DateTimeKind.Unspecified"/>.</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// <para>Uses the <c>"o"</c> (round-trip) format for <see cref="DateTimeKind.Local"/> and <see cref="DateTimeKind.Unspecified"/>, and a custom UTC format (<c>"yyyy-MM-ddTHH:mm:ss.fffffffZ"</c>) for <see cref="DateTimeKind.Utc"/>.</para>
    /// </remarks>
    public static string ToIsoString(this DateTime dateTime) => dateTime.Kind switch
    {
        DateTimeKind.Utc => dateTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture),
        DateTimeKind.Local => dateTime.ToString("o", CultureInfo.InvariantCulture),
        _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified).ToString("o", CultureInfo.InvariantCulture),
    };

    /// <summary>
    /// Returns a string representation of the specified <paramref name="dateTime"/> in ISO 8601 format, optionally omitting fractional seconds.
    /// </summary>
    /// <param name="dateTime">The date and time value to convert.</param>
    /// <param name="includeFractionalSeconds">Indicates whether to include fractional seconds (7 digits) in the output.</param>
    /// <returns>A <see cref="string"/> representation of <paramref name="dateTime"/> in ISO 8601 format, with or without fractional seconds.</returns>
    /// <remarks>
    /// <para>Uses the <c>"o"</c> (round-trip) format when <paramref name="includeFractionalSeconds"/> is <see langword="true"/>; otherwise uses <c>"yyyy-MM-ddTHH:mm:ss"</c>.</para>
    /// </remarks>
    public static string ToIsoString(this DateTime dateTime, bool includeFractionalSeconds)
    {
        var format = includeFractionalSeconds ? "o" : "yyyy-MM-ddTHH:mm:ss";
        return dateTime.ToString(format, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Returns a string representation of the specified <paramref name="dateTime"/> in ISO 8601 format, using an explicit <see cref="DateTimeKind"/> override.
    /// </summary>
    /// <param name="dateTime">The date and time value to convert.</param>
    /// <param name="kind">The <see cref="DateTimeKind"/> to apply before formatting.</param>
    /// <returns>
    /// A <see cref="string"/> representation of <paramref name="dateTime"/> in ISO 8601 format:
    /// <list type="bullet">
    /// <item><description><see cref="DateTimeKind.Utc"/> — ends with <c>'Z'</c>;</description></item>
    /// <item><description><see cref="DateTimeKind.Local"/> — includes the local time zone offset;</description></item>
    /// <item><description><see cref="DateTimeKind.Unspecified"/> — omits any offset.</description></item>
    /// </list>
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="kind"/> is not a defined value of the <see cref="DateTimeKind"/> enumeration.</exception>
    public static string ToIsoString(this DateTime dateTime, DateTimeKind kind)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(kind);

        return kind switch
        {
            DateTimeKind.Utc => dateTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture),
            DateTimeKind.Local => dateTime.ToLocalTime().ToString("o", CultureInfo.InvariantCulture),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified).ToString("o", CultureInfo.InvariantCulture),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), "Invalid DateTimeKind value.")
        };
    }

    /// <summary>
    /// Returns a string representation of the specified <paramref name="dateTime"/> using a custom format and an optional culture.
    /// </summary>
    /// <param name="dateTime">The date and time value to format.</param>
    /// <param name="format">A valid date-time format string (e.g. <c>"yyyy-MM-ddTHH:mm:ss"</c>). Must not be <see langword="null"/> or whitespace.</param>
    /// <param name="culture">An optional <see cref="CultureInfo"/> used for culture-specific formatting. If <see langword="null"/>, <see cref="CultureInfo.InvariantCulture"/> is used.</param>
    /// <returns>A formatted <see cref="string"/> representation of <paramref name="dateTime"/> using the supplied format and culture.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="format"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="format"/> is empty or whitespace.</exception>
    public static string ToIsoString(this DateTime dateTime, string format, CultureInfo? culture = null)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(format);

        return dateTime.ToString(format, culture ?? CultureInfo.InvariantCulture);
    }
}
