// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EasterSundayNotableDateAlgorithm.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Computes the Gregorian or Julian date of Easter Sunday for a supplied year, implementing the
/// <see cref="INotableDateAlgorithm" /> contract so that the result can be folded into a <see cref="NotableDateRule" />
/// via <see cref="DateResolutionStrategy.Algorithm" />.
/// </summary>
/// <remarks>
/// <para>
/// Easter Sunday is the first Sunday after the Paschal full moon. Two computus algorithms are used: the Gregorian
/// (Anonymous) algorithm for years <c>&gt;= 1583</c>, and Meeus's adaptation of the Julian algorithm for earlier years
/// or when the caller explicitly supplies a <see cref="System.Globalization.JulianCalendar" />.
/// </para>
/// <para>
/// Results are deterministic and cached per <c>(year, calendar)</c> pair in a process-wide
/// <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey, TValue}" /> so that the relatively expensive
/// modular arithmetic is paid once per distinct year, regardless of how many rules anchor against Easter.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// // Direct use — most callers reach Easter through NotableDateRule + DateResolutionStrategy.Algorithm
/// // rather than constructing the algorithm by hand.
/// INotableDateAlgorithm easter = new EasterSundayNotableDateAlgorithm();
/// DateTime? sunday = easter.GetDate(2026, calendar: null);   // 2026-04-05 (Gregorian)
///
/// // Rule-based use — register the algorithm and anchor every dependent observance against it.
/// var goodFriday = new NotableDateRule
/// {
///     Name         = "Good Friday",
///     Strategy     = DateResolutionStrategy.OffsetFromAnchor,
///     AnchorName   = "Easter Sunday",
///     OffsetDays   = -2,
/// };
///]]>
/// </example>
public sealed class EasterSundayNotableDateAlgorithm
    : INotableDateAlgorithm
{
    /// <summary>
    /// Shared per-process cache of computed Easter dates, keyed by year and calendar identity string.
    /// </summary>
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
