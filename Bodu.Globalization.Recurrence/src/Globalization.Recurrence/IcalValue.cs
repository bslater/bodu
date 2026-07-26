// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IcalValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Recurrence;

/// <summary>
/// Provides parsing and formatting of the RFC 5545 <c>DATE</c> / <c>DATE-TIME</c> value forms and the two-letter
/// weekday tokens shared by <see cref="RecurrenceRule" /> and <c>RecurrenceSet</c>.
/// </summary>
internal static class IcalValue
{
    /// <summary>
    /// Attempts to parse an RFC 5545 <c>DATE</c> (<c>YYYYMMDD</c>) or <c>DATE-TIME</c> (<c>YYYYMMDDTHHMMSS</c>,
    /// optionally suffixed with <c>Z</c> for UTC) value.
    /// </summary>
    /// <param name="value">The candidate value text.</param>
    /// <param name="result">
    /// The parsed instant on success; a UTC value yields a <see cref="DateTimeKind.Utc" /> result.
    /// </param>
    /// <returns><see langword="true" /> if the value was parsed; otherwise <see langword="false" />.</returns>
    internal static bool TryParseDateTime(ReadOnlySpan<char> value, out DateTime result)
    {
        result = default;

        bool utc = value.Length > 0 && (value[^1] is 'Z' or 'z');
        if (utc)
        {
            value = value[..^1];
        }

        // DATE form: YYYYMMDD.
        if (value.Length == 8)
        {
            return !utc && TryBuild(value, 0, 0, 0, DateTimeKind.Unspecified, out result);
        }

        // DATE-TIME form: YYYYMMDD 'T' HHMMSS.
        if (value.Length == 15 && (value[8] is 'T' or 't')
            && TryParseTwo(value, 9, 0, 23, out int hour)
            && TryParseTwo(value, 11, 0, 59, out int minute)
            && TryParseTwo(value, 13, 0, 60, out int second))
        {
            // A leap second (60) is clamped to 59 because the BCL calendar has no representation for it.
            if (second == 60)
            {
                second = 59;
            }

            return TryBuild(value[..8], hour, minute, second, utc ? DateTimeKind.Utc : DateTimeKind.Unspecified, out result);
        }

        return false;
    }

    /// <summary>
    /// Formats an instant as an RFC 5545 <c>DATE-TIME</c> value, appending the <c>Z</c> UTC designator when the
    /// instant's kind is <see cref="DateTimeKind.Utc" />.
    /// </summary>
    /// <param name="value">The instant to format.</param>
    /// <returns>The canonical iCalendar date-time text.</returns>
    internal static string FormatDateTime(DateTime value)
    {
        string core = value.ToString("yyyyMMdd'T'HHmmss", CultureInfo.InvariantCulture);
        return value.Kind == DateTimeKind.Utc ? core + "Z" : core;
    }

    /// <summary>
    /// Attempts to map a two-letter RFC 5545 weekday token (<c>SU</c>, <c>MO</c>, …, <c>SA</c>) to a
    /// <see cref="DayOfWeek" />.
    /// </summary>
    /// <param name="token">The candidate two-letter token.</param>
    /// <param name="day">The mapped day on success.</param>
    /// <returns><see langword="true" /> if the token was recognized; otherwise <see langword="false" />.</returns>
    internal static bool TryParseWeekDay(ReadOnlySpan<char> token, out DayOfWeek day)
    {
        if (token.Length == 2)
        {
            char a = char.ToUpperInvariant(token[0]);
            char b = char.ToUpperInvariant(token[1]);
            switch ((a, b))
            {
                case ('S', 'U'):
                    day = DayOfWeek.Sunday;
                    return true;
                case ('M', 'O'):
                    day = DayOfWeek.Monday;
                    return true;
                case ('T', 'U'):
                    day = DayOfWeek.Tuesday;
                    return true;
                case ('W', 'E'):
                    day = DayOfWeek.Wednesday;
                    return true;
                case ('T', 'H'):
                    day = DayOfWeek.Thursday;
                    return true;
                case ('F', 'R'):
                    day = DayOfWeek.Friday;
                    return true;
                case ('S', 'A'):
                    day = DayOfWeek.Saturday;
                    return true;
            }
        }

        day = default;
        return false;
    }

    /// <summary>
    /// Returns the two-letter RFC 5545 token for the specified <see cref="DayOfWeek" />.
    /// </summary>
    /// <param name="day">The day to format.</param>
    /// <returns>The uppercase two-letter weekday token.</returns>
    internal static string FormatWeekDay(DayOfWeek day) =>
        day switch
        {
            DayOfWeek.Sunday => "SU",
            DayOfWeek.Monday => "MO",
            DayOfWeek.Tuesday => "TU",
            DayOfWeek.Wednesday => "WE",
            DayOfWeek.Thursday => "TH",
            DayOfWeek.Friday => "FR",
            _ => "SA",
        };

    /// <summary>
    /// Builds a <see cref="DateTime" /> from an <c>YYYYMMDD</c> date span and the supplied time-of-day components,
    /// validating every field against the Gregorian calendar.
    /// </summary>
    /// <param name="date">The eight-character date span.</param>
    /// <param name="hour">The hour component.</param>
    /// <param name="minute">The minute component.</param>
    /// <param name="second">The second component.</param>
    /// <param name="kind">The kind assigned to the result.</param>
    /// <param name="result">The constructed instant on success.</param>
    /// <returns><see langword="true" /> if every field was valid; otherwise <see langword="false" />.</returns>
    private static bool TryBuild(ReadOnlySpan<char> date, int hour, int minute, int second, DateTimeKind kind, out DateTime result)
    {
        result = default;

        if (!int.TryParse(date[..4], NumberStyles.None, CultureInfo.InvariantCulture, out int year) || year < 1 || year > 9999)
        {
            return false;
        }

        if (!TryParseTwo(date, 4, 1, 12, out int month))
        {
            return false;
        }

        if (!TryParseTwo(date, 6, 1, DateTime.DaysInMonth(year, month), out int day))
        {
            return false;
        }

        result = new DateTime(year, month, day, hour, minute, second, kind);
        return true;
    }

    /// <summary>
    /// Parses two decimal digits at the given offset and validates the result against an inclusive range.
    /// </summary>
    /// <param name="source">The source span.</param>
    /// <param name="start">The offset of the first digit.</param>
    /// <param name="min">The inclusive lower bound.</param>
    /// <param name="max">The inclusive upper bound.</param>
    /// <param name="value">The parsed value on success.</param>
    /// <returns>
    /// <see langword="true" /> if the two digits parsed within range; otherwise <see langword="false" />.
    /// </returns>
    private static bool TryParseTwo(ReadOnlySpan<char> source, int start, int min, int max, out int value) =>
        int.TryParse(source.Slice(start, 2), NumberStyles.None, CultureInfo.InvariantCulture, out value)
        && value >= min
        && value <= max;
}
