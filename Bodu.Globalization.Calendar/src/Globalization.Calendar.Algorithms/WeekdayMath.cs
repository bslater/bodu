// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekdayMath.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Provides the calendar-specific weekday-seeking and nth-weekday-in-month helpers used by the weekday-based
/// strategies.
/// </summary>
/// <remarks>
/// The underlying weekday navigation is supplied by the <see cref="DateOnlyExtensions" /> primitives in
/// <c>Bodu.Core</c> (<see cref="DateOnlyExtensions.NextDateOfWeek(DateOnly, DayOfWeek)" /> and its <c>OrSame</c>/
/// <c>Previous</c>/<c>Nearest</c> companions). This type layers on only the behavior the resolution pipeline requires
/// that those primitives do not provide: dispatch over <see cref="WeekdayProximity" />, and a null-returning (rather
/// than throwing) result at the year extremes and for ordinals that do not occur in a month.
/// </remarks>
internal static class WeekdayMath
{
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
            WeekdayProximity.Before => anchor.PreviousDateOfWeek(dayOfWeek),
            WeekdayProximity.OnOrBefore => anchor.PreviousOrSameDateOfWeek(dayOfWeek),
            WeekdayProximity.Nearest => anchor.NearestDateOfWeek(dayOfWeek),
            WeekdayProximity.OnOrAfter => anchor.NextOrSameDateOfWeek(dayOfWeek),
            WeekdayProximity.After => anchor.NextDateOfWeek(dayOfWeek),
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
    /// The matching date, or <see langword="null" /> when the seek rolls past <see cref="DateOnly.MinValue" />/
    /// <see cref="DateOnly.MaxValue" /> at the year extremes.
    /// </returns>
    /// <remarks>
    /// A proximity seek moves the anchor by up to seven days, so at the first and last representable weeks of the
    /// supported year range it can overflow; resolution treats that as "no occurrence" instead of failing the query.
    /// </remarks>
    public static DateOnly? SeekOrNull(DateOnly anchor, DayOfWeek dayOfWeek, WeekdayProximity proximity)
    {
        // A proximity seek moves the anchor by at most seven days. When the anchor sits at least a week inside the
        // representable range the seek cannot overflow, so take the fast path with no exception handling. Only in the
        // first or last representable week — where a roll past DateOnly.Min/MaxValue is possible — do we fall back to
        // catching the boundary overflow and reporting "no occurrence".
        if (anchor.DayNumber >= DateOnly.MinValue.DayNumber + 7 && anchor.DayNumber <= DateOnly.MaxValue.DayNumber - 7)
            return Seek(anchor, dayOfWeek, proximity);

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
            return lastDay.PreviousOrSameDateOfWeek(dayOfWeek);
        }

        DateOnly firstMatch = new DateOnly(year, month, 1).NextOrSameDateOfWeek(dayOfWeek);
        DateOnly result = firstMatch.AddDays(7 * ((int)ordinal - 1));
        return result.Month == month ? result : null;
    }
}
