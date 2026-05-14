// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LosarNotableDateAlgorithm.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using SysGlobal = System.Globalization;

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Provides an algorithm for determining the approximate Gregorian date of Losar (Tibetan New Year) for a given year.
/// </summary>
/// <remarks>
/// <para>
/// Losar (lo = year, sar = new) is the Tibetan New Year, celebrated on the first day of the first month of the Tibetan lunar
/// calendar. It is the most important festival in the Tibetan Buddhist calendar, observed by Tibetan, Mongolian, and Himalayan
/// Buddhist communities.
/// </para>
/// <para>
/// <strong>Accuracy note:</strong> This algorithm uses the first new moon on or after 20 January of the given year as an
/// approximation of the Tibetan New Year. This matches the actual Losar date in the majority of years. However, the Tibetan lunisolar
/// calendar (Phugpa school, maintained by the Tibetan Medical and Astro Institute) uses a distinct intercalation scheme that
/// diverges from the Chinese lunisolar calendar by approximately one month in certain years. In those years — which occur
/// irregularly, roughly every three to five years — this algorithm may return a date that is approximately one month earlier than
/// the actual Losar. Consumers who require exact Tibetan calendar dates should register a custom implementation that incorporates
/// the official TMAI Tibetan calendar tables.
/// </para>
/// <para>
/// The new moon date is computed using the Meeus Chapter 49 algorithm via <see cref="LunarPhaseAlgorithm" />, accurate to within
/// one calendar day for years in the range 1900–2100.
/// </para>
/// </remarks>
public sealed class LosarNotableDateAlgorithm
    : INotableDateAlgorithm
{
    /// <summary>
    /// Computes the approximate date of Losar for the specified year.
    /// </summary>
    /// <param name="year">The year for which to calculate Losar. Must be greater than or equal to 1.</param>
    /// <param name="calendar">
    /// An optional <see cref="SysGlobal.Calendar" /> instance representing the calendar system to use. When <see langword="null" />,
    /// <see cref="SysGlobal.GregorianCalendar" /> is assumed.
    /// </param>
    /// <returns>
    /// A <see cref="DateTime" /> representing the approximate date of Losar, or <see langword="null" /> if the date cannot be
    /// determined. The returned <see cref="DateTime.Kind" /> is always <see cref="DateTimeKind.Unspecified" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="year" /> is less than 1.</exception>
    /// <exception cref="NotSupportedException">Thrown when the specified <paramref name="calendar" /> type is unsupported.</exception>
    public DateTime? GetDate(int year, SysGlobal.Calendar? calendar = null)
    {
        if (year < 1)
            throw new ArgumentOutOfRangeException(nameof(year), CalendarResourceStrings.ArgumentOutOfRangeException_YearOutOfRange);

        // The Tibetan New Year falls on or shortly after the second new moon after the winter
        // solstice. The winter solstice is approximately 21 December; the second new moon after
        // it is typically in late January or February. Using 20 January as the search start
        // reliably catches the correct lunation for the vast majority of years.
        DateTime? newMoon = LunarPhaseAlgorithm.GetNewMoonOnOrAfter(new DateTime(year, 1, 20));
        if (newMoon is null)
            return null;

        SysGlobal.Calendar targetCalendar = calendar ?? new SysGlobal.GregorianCalendar();

        if (targetCalendar.GetType() == typeof(SysGlobal.GregorianCalendar))
            return DateTime.SpecifyKind(newMoon.Value, DateTimeKind.Unspecified);

        return new DateTime(
            targetCalendar.GetYear(newMoon.Value),
            targetCalendar.GetMonth(newMoon.Value),
            targetCalendar.GetDayOfMonth(newMoon.Value),
            0, 0, 0, 0,
            targetCalendar,
            DateTimeKind.Unspecified);
    }
}
