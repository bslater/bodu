// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekdayMath.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Provides shared weekday-seeking and nth-weekday-in-month arithmetic used by the weekday-based strategies.
/// </summary>
internal static class WeekdayMath
{
    /// <summary>
    /// Returns the supplied date if it already falls on the target weekday, otherwise the first such weekday after it.
    /// </summary>
    /// <param name="date">The anchor date.</param>
    /// <param name="dayOfWeek">The target weekday.</param>
    /// <returns>The matching date on or after <paramref name="date" />.</returns>
    public static DateOnly OnOrAfter(DateOnly date, DayOfWeek dayOfWeek)
    {
        int delta = ((int)dayOfWeek - (int)date.DayOfWeek + 7) % 7;
        return date.AddDays(delta);
    }

    /// <summary>
    /// Returns the first occurrence of the target weekday strictly after the supplied date.
    /// </summary>
    /// <param name="date">The anchor date.</param>
    /// <param name="dayOfWeek">The target weekday.</param>
    /// <returns>The matching date after <paramref name="date" />.</returns>
    public static DateOnly After(DateOnly date, DayOfWeek dayOfWeek) =>
        OnOrAfter(date.AddDays(1), dayOfWeek);

    /// <summary>
    /// Returns the supplied date if it already falls on the target weekday, otherwise the first such weekday before it.
    /// </summary>
    /// <param name="date">The anchor date.</param>
    /// <param name="dayOfWeek">The target weekday.</param>
    /// <returns>The matching date on or before <paramref name="date" />.</returns>
    public static DateOnly OnOrBefore(DateOnly date, DayOfWeek dayOfWeek)
    {
        int delta = ((int)date.DayOfWeek - (int)dayOfWeek + 7) % 7;
        return date.AddDays(-delta);
    }

    /// <summary>
    /// Returns the first occurrence of the target weekday strictly before the supplied date.
    /// </summary>
    /// <param name="date">The anchor date.</param>
    /// <param name="dayOfWeek">The target weekday.</param>
    /// <returns>The matching date before <paramref name="date" />.</returns>
    public static DateOnly Before(DateOnly date, DayOfWeek dayOfWeek) =>
        OnOrBefore(date.AddDays(-1), dayOfWeek);

    /// <summary>
    /// Returns the occurrence of the target weekday closest to the supplied date in either direction.
    /// </summary>
    /// <param name="date">The anchor date.</param>
    /// <param name="dayOfWeek">The target weekday.</param>
    /// <returns>The closest matching date.</returns>
    public static DateOnly Nearest(DateOnly date, DayOfWeek dayOfWeek)
    {
        DateOnly after = OnOrAfter(date, dayOfWeek);
        DateOnly before = OnOrBefore(date, dayOfWeek);
        return (after.DayNumber - date.DayNumber) <= (date.DayNumber - before.DayNumber) ? after : before;
    }

    /// <summary>
    /// Seeks the target weekday relative to an anchor using the supplied proximity rule.
    /// </summary>
    /// <param name="anchor">The anchor date.</param>
    /// <param name="dayOfWeek">The target weekday.</param>
    /// <param name="proximity">The direction and inclusivity to apply.</param>
    /// <returns>The matching date.</returns>
    public static DateOnly Seek(DateOnly anchor, DayOfWeek dayOfWeek, WeekdayProximity proximity) =>
        proximity switch
        {
            WeekdayProximity.Before => Before(anchor, dayOfWeek),
            WeekdayProximity.OnOrBefore => OnOrBefore(anchor, dayOfWeek),
            WeekdayProximity.Nearest => Nearest(anchor, dayOfWeek),
            WeekdayProximity.OnOrAfter => OnOrAfter(anchor, dayOfWeek),
            WeekdayProximity.After => After(anchor, dayOfWeek),
            _ => anchor,
        };

    /// <summary>
    /// Seeks the target weekday relative to an anchor, returning <see langword="null" /> when the result would fall
    /// outside the representable date range rather than throwing.
    /// </summary>
    /// <param name="anchor">The anchor date.</param>
    /// <param name="dayOfWeek">The target weekday.</param>
    /// <param name="proximity">The direction and inclusivity to apply.</param>
    /// <returns>
    /// The matching date, or <see langword="null" /> when the seek rolls past
    /// <see cref="DateOnly.MinValue" />/<see cref="DateOnly.MaxValue" /> at the year extremes.
    /// </returns>
    /// <remarks>
    /// A proximity seek moves the anchor by up to seven days, so at the first and last representable weeks of the
    /// supported year range it can overflow; resolution treats that as "no occurrence" instead of failing the query.
    /// </remarks>
    public static DateOnly? SeekOrNull(DateOnly anchor, DayOfWeek dayOfWeek, WeekdayProximity proximity)
    {
        try
        {
            return Seek(anchor, dayOfWeek, proximity);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    /// <summary>
    /// Returns the date of the nth (or last) occurrence of a weekday within a month.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="month">The one-based month.</param>
    /// <param name="dayOfWeek">The target weekday.</param>
    /// <param name="ordinal">The occurrence to select.</param>
    /// <returns>
    /// The matching date, or <see langword="null" /> when the requested ordinal does not exist in the month (for
    /// example a fifth occurrence in a month that has only four).
    /// </returns>
    public static DateOnly? NthWeekdayInMonth(int year, int month, DayOfWeek dayOfWeek, WeekOrdinal ordinal)
    {
        if (year < 1 || year > 9999 || month < 1 || month > 12)
            return null;

        if (ordinal == WeekOrdinal.Last)
        {
            DateOnly lastDay = new(year, month, DateTime.DaysInMonth(year, month));
            return OnOrBefore(lastDay, dayOfWeek);
        }

        DateOnly firstMatch = OnOrAfter(new DateOnly(year, month, 1), dayOfWeek);
        DateOnly result = firstMatch.AddDays(7 * ((int)ordinal - 1));
        return result.Month == month ? result : null;
    }
}
