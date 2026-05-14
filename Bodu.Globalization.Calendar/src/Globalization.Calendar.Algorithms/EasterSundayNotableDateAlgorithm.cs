// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EasterSundayNotableDateAlgorithm.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Provides Easter Sunday date calculations based on the given year and calendar system.
/// </summary>
/// <remarks>
/// This implementation uses the Gregorian calendar for years &gt;= 1583 and the Julian calendar otherwise. Results are cached for
/// performance. Use this algorithm to retrieve the date of Easter Sunday.
/// </remarks>
public sealed class EasterSundayNotableDateAlgorithm
    : INotableDateAlgorithm
{
    /// <summary>Shared per-process cache of computed Easter dates, keyed by year and calendar identity string.</summary>
    private static readonly ConcurrentDictionary<(int year, string calendarId), DateTime> s_easterCache = new();

    /// <inheritdoc />
    public DateTime? GetDate(int year, System.Globalization.Calendar? calendar)
    {
        ThrowHelper.ThrowIfLessThan(year, 1);

        return GetOrAddEasterSunday(year, calendar);
    }

    /// <summary>
    /// Computes Easter Sunday for the specified year and optional calendar.
    /// </summary>
    /// <param name="year">The year for which to compute Easter.</param>
    /// <param name="calendar">The calendar system (defaults to Gregorian).</param>
    /// <returns>A <see cref="DateTime" /> representing Easter Sunday.</returns>
    private static DateTime GetOrAddEasterSunday(int year, System.Globalization.Calendar? calendar)
    {
        var calendarId = calendar?.GetType().FullName ?? "Gregorian";
        (int year, string calendarId) key = (year, calendarId);

        return s_easterCache.GetOrAdd(key, _ =>
        {
            int month, day;
            if (year >= 1583 && calendar is not System.Globalization.JulianCalendar)
            {
                // Gregorian calendar algorithm (Computus)
                var a = year % 19;
                var b = year / 100;
                var c = year % 100;
                var d = b / 4;
                var e = b % 4;
                var f = (b + 8) / 25;
                var g = (b - f + 1) / 3;
                var h = (19 * a + b - d - g + 15) % 30;
                var i = c / 4;
                var k = c % 4;
                var l = (32 + 2 * e + 2 * i - h - k) % 7;
                var m = (a + 11 * h + 22 * l) / 451;
                month = (h + l - 7 * m + 114) / 31;
                day = (h + l - 7 * m + 114) % 31 + 1;
            }
            else
            {
                // Julian calendar algorithm
                var a = year % 4;
                var b = year % 7;
                var c = year % 19;
                var d = (19 * c + 15) % 30;
                var e = (2 * a + 4 * b - d + 34) % 7;
                var f = d + e + 114;
                month = f / 31;
                day = f % 31 + 1;
            }

            return calendar != null
                ? calendar.ToDateTime(year, month, day, 0, 0, 0, 0)
                : new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Unspecified);
        });
    }
}
