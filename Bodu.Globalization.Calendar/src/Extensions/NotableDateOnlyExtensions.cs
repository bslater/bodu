// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

/// <summary>
/// Provides working-day, traversal, and notable-date query extension methods over <see cref="DateOnly" />, resolved
/// through an explicit <see cref="INotableDateService" /> for a requested territory.
/// </summary>
/// <remarks>
/// <para>
/// A working day is a day that is neither outside the working week nor a non-working notable date. The working week
/// defaults to Monday through Friday and can be overridden with a <see cref="WeekPattern" />. The traversal and
/// counting methods resolve each candidate day through the service, so prefer the range-based
/// <see cref="EnumerateNotableDates" /> for large windows.
/// </para>
/// </remarks>
public static class NotableDateOnlyExtensions
{
    /// <summary>
    /// The maximum number of days a traversal will probe before giving up, guarding against a degenerate working week.
    /// </summary>
    private const int TraversalGuard = 4000;

    /// <summary>
    /// Determines whether the date falls outside the working week.
    /// </summary>
    /// <param name="date">The date to test.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>
    /// <see langword="true" /> if the date is not a working-week day; otherwise <see langword="false" />.
    /// </returns>
    public static bool IsWeekend(this DateOnly date, WeekPattern? workingWeek = null) =>
        !(workingWeek ?? WeekPattern.MondayToFriday).Contains(date.DayOfWeek);

    /// <summary>
    /// Determines whether the date is a non-working day: outside the working week, or a non-working notable date.
    /// </summary>
    /// <param name="date">The date to test.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns><see langword="true" /> if the date is non-working; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static bool IsNonWorkingDay(this DateOnly date, INotableDateService service, string territory, WeekPattern? workingWeek = null)
    {
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfNull(territory);

        if (date.IsWeekend(workingWeek))
            return true;

        return service.Resolve(date, territory).Any(n => n.IsNonWorkingDay);
    }

    /// <summary>
    /// Determines whether the date is a working day.
    /// </summary>
    /// <param name="date">The date to test.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns><see langword="true" /> if the date is a working day; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static bool IsWorkingDay(this DateOnly date, INotableDateService service, string territory, WeekPattern? workingWeek = null) =>
        !date.IsNonWorkingDay(service, territory, workingWeek);

    /// <summary>
    /// Determines whether any notable date is emitted on the date for the territory.
    /// </summary>
    /// <param name="date">The date to test.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="filter">An optional filter the occurrence must satisfy.</param>
    /// <returns>
    /// <see langword="true" /> if at least one occurrence is emitted; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static bool IsNotableDate(this DateOnly date, INotableDateService service, string territory, NotableDateFilter? filter = null) =>
        date.GetNotableDates(service, territory, filter).Count > 0;

    /// <summary>
    /// Gets the notable dates emitted on the date for the territory.
    /// </summary>
    /// <param name="date">The date to resolve.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="filter">An optional filter the occurrences must satisfy.</param>
    /// <returns>The emitted occurrences; empty when there are none.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static IReadOnlyList<NotableDate> GetNotableDates(this DateOnly date, INotableDateService service, string territory, NotableDateFilter? filter = null)
    {
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfNull(territory);

        return filter is null ? service.Resolve(date, territory) : service.Resolve(date, territory, filter);
    }

    /// <summary>
    /// Returns the first working day strictly after the date.
    /// </summary>
    /// <param name="date">The starting date.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The next working day.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static DateOnly NextWorkingDay(this DateOnly date, INotableDateService service, string territory, WeekPattern? workingWeek = null) =>
        Step(date, 1, service, territory, workingWeek);

    /// <summary>
    /// Returns the first working day strictly before the date.
    /// </summary>
    /// <param name="date">The starting date.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The previous working day.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static DateOnly PreviousWorkingDay(this DateOnly date, INotableDateService service, string territory, WeekPattern? workingWeek = null) =>
        Step(date, -1, service, territory, workingWeek);

    /// <summary>
    /// Returns the date if it is a working day; otherwise the next working day.
    /// </summary>
    /// <param name="date">The date to snap.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The date or the next working day.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static DateOnly SnapToWorkingDay(this DateOnly date, INotableDateService service, string territory, WeekPattern? workingWeek = null) =>
        date.IsWorkingDay(service, territory, workingWeek) ? date : Step(date, 1, service, territory, workingWeek);

    /// <summary>
    /// Returns the date if it is a working day; otherwise the previous working day.
    /// </summary>
    /// <param name="date">The date to snap.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The date or the previous working day.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static DateOnly SnapToWorkingDayBackward(this DateOnly date, INotableDateService service, string territory, WeekPattern? workingWeek = null) =>
        date.IsWorkingDay(service, territory, workingWeek) ? date : Step(date, -1, service, territory, workingWeek);

    /// <summary>
    /// Returns the date if it is a working day; otherwise the nearest working day, preferring the forward direction on
    /// a tie.
    /// </summary>
    /// <param name="date">The date to snap.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The nearest working day.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static DateOnly SnapToNearestWorkingDay(this DateOnly date, INotableDateService service, string territory, WeekPattern? workingWeek = null)
    {
        if (date.IsWorkingDay(service, territory, workingWeek))
            return date;

        DateOnly forward = Step(date, 1, service, territory, workingWeek);
        DateOnly backward = Step(date, -1, service, territory, workingWeek);

        return (forward.DayNumber - date.DayNumber) <= (date.DayNumber - backward.DayNumber) ? forward : backward;
    }

    /// <summary>
    /// Advances the date by a signed number of working days.
    /// </summary>
    /// <param name="date">The starting date.</param>
    /// <param name="count">
    /// The number of working days to add; negative retreats, zero returns the date unchanged.
    /// </param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The resulting date.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static DateOnly AddWorkingDays(this DateOnly date, int count, INotableDateService service, string territory, WeekPattern? workingWeek = null)
    {
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfNull(territory);

        if (count == 0)
            return date;

        var direction = count > 0 ? 1 : -1;
        var remaining = Math.Abs(count);
        DateOnly current = date;

        while (remaining > 0)
        {
            current = Step(current, direction, service, territory, workingWeek);
            remaining--;
        }

        return current;
    }

    /// <summary>
    /// Counts the working days in the inclusive range bounded by the two dates, regardless of their order.
    /// </summary>
    /// <param name="start">One end of the range.</param>
    /// <param name="end">The other end of the range.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The number of working days in the inclusive range.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static int WorkingDaysBetween(this DateOnly start, DateOnly end, INotableDateService service, string territory, WeekPattern? workingWeek = null)
    {
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfNull(territory);

        DateOnly lower = start <= end ? start : end;
        DateOnly upper = start <= end ? end : start;

        var count = 0;
        for (DateOnly cursor = lower; cursor <= upper; cursor = cursor.AddDays(1))
        {
            if (cursor.IsWorkingDay(service, territory, workingWeek))
                count++;
        }

        return count;
    }

    /// <summary>
    /// Lazily enumerates the working days in the inclusive range, in ascending order.
    /// </summary>
    /// <param name="start">The inclusive start date.</param>
    /// <param name="end">The inclusive end date.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The working days in the range.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static IEnumerable<DateOnly> EnumerateWorkingDays(this DateOnly start, DateOnly end, INotableDateService service, string territory, WeekPattern? workingWeek = null)
    {
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfNull(territory);

        return Iterator();

        IEnumerable<DateOnly> Iterator()
        {
            for (DateOnly cursor = start; cursor <= end; cursor = cursor.AddDays(1))
            {
                if (cursor.IsWorkingDay(service, territory, workingWeek))
                    yield return cursor;
            }
        }
    }

    /// <summary>
    /// Resolves the notable dates emitted within the inclusive range for the territory.
    /// </summary>
    /// <param name="start">The inclusive start date.</param>
    /// <param name="end">The inclusive end date.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="filter">An optional filter the occurrences must satisfy.</param>
    /// <returns>The emitted occurrences, ordered by date then identity.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static IReadOnlyList<NotableDate> EnumerateNotableDates(this DateOnly start, DateOnly end, INotableDateService service, string territory, NotableDateFilter? filter = null)
    {
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfNull(territory);

        DateRange range = new(start, end);
        return filter is null ? service.Resolve(range, territory) : service.Resolve(range, territory, filter);
    }

    /// <summary>
    /// Resolves the notable dates emitted in the calendar month that contains the date for the territory.
    /// </summary>
    /// <param name="date">A date within the month to resolve.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="filter">An optional filter the occurrences must satisfy.</param>
    /// <returns>The emitted occurrences in the month, ordered by date then identity.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static IReadOnlyList<NotableDate> GetNotableDatesInMonth(this DateOnly date, INotableDateService service, string territory, NotableDateFilter? filter = null) =>
        new DateOnly(date.Year, date.Month, 1)
            .EnumerateNotableDates(new DateOnly(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month)), service, territory, filter);

    /// <summary>
    /// Resolves the notable dates emitted in the calendar year that contains the date for the territory.
    /// </summary>
    /// <param name="date">A date within the year to resolve.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="filter">An optional filter the occurrences must satisfy.</param>
    /// <returns>The emitted occurrences in the year, ordered by date then identity.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static IReadOnlyList<NotableDate> GetNotableDatesInYear(this DateOnly date, INotableDateService service, string territory, NotableDateFilter? filter = null) =>
        new DateOnly(date.Year, 1, 1)
            .EnumerateNotableDates(new DateOnly(date.Year, 12, 31), service, territory, filter);

    /// <summary>
    /// Returns the first non-working day strictly after the date.
    /// </summary>
    /// <param name="date">The starting date.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The next non-working day.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">No non-working day is found within the traversal guard.</exception>
    public static DateOnly NextNonWorkingDay(this DateOnly date, INotableDateService service, string territory, WeekPattern? workingWeek = null) =>
        StepNonWorking(date, 1, service, territory, workingWeek);

    /// <summary>
    /// Returns the first non-working day strictly before the date.
    /// </summary>
    /// <param name="date">The starting date.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The previous non-working day.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">No non-working day is found within the traversal guard.</exception>
    public static DateOnly PreviousNonWorkingDay(this DateOnly date, INotableDateService service, string territory, WeekPattern? workingWeek = null) =>
        StepNonWorking(date, -1, service, territory, workingWeek);

    /// <summary>
    /// Lazily enumerates the non-working days in the inclusive range, in ascending order.
    /// </summary>
    /// <param name="start">The inclusive start date.</param>
    /// <param name="end">The inclusive end date.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The non-working days in the range.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static IEnumerable<DateOnly> EnumerateNonWorkingDays(this DateOnly start, DateOnly end, INotableDateService service, string territory, WeekPattern? workingWeek = null)
    {
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfNull(territory);

        return Iterator();

        IEnumerable<DateOnly> Iterator()
        {
            for (DateOnly cursor = start; cursor <= end; cursor = cursor.AddDays(1))
            {
                if (cursor.IsNonWorkingDay(service, territory, workingWeek))
                    yield return cursor;
            }
        }
    }

    /// <summary>
    /// Returns the earliest notable date emitted strictly after the date for the territory.
    /// </summary>
    /// <param name="date">The reference date.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="filter">An optional filter the occurrence must satisfy.</param>
    /// <returns>
    /// The next matching occurrence, or <see langword="null" /> when none exists up to the maximum year.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static NotableDate? NextNotableDate(this DateOnly date, INotableDateService service, string territory, NotableDateFilter? filter = null)
    {
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfNull(territory);

        for (var year = date.Year; year <= DateOnly.MaxValue.Year; year++)
        {
            foreach (NotableDate notable in ResolveYear(year, service, territory, filter))
            {
                if (notable.Date > date)
                    return notable;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns the most recent notable date emitted strictly before the date for the territory.
    /// </summary>
    /// <param name="date">The reference date.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="filter">An optional filter the occurrence must satisfy.</param>
    /// <returns>
    /// The previous matching occurrence, or <see langword="null" /> when none exists down to the minimum year.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static NotableDate? PreviousNotableDate(this DateOnly date, INotableDateService service, string territory, NotableDateFilter? filter = null)
    {
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfNull(territory);

        for (var year = date.Year; year >= DateOnly.MinValue.Year; year--)
        {
            IReadOnlyList<NotableDate> resolved = ResolveYear(year, service, territory, filter);
            for (var i = resolved.Count - 1; i >= 0; i--)
            {
                if (resolved[i].Date < date)
                    return resolved[i];
            }
        }

        return null;
    }

    /// <summary>
    /// Resolves the notable dates emitted in a calendar year for the territory, ordered by date then identity.
    /// </summary>
    /// <param name="year">The calendar year to resolve.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="filter">An optional filter the occurrences must satisfy.</param>
    /// <returns>The emitted occurrences in the year.</returns>
    private static IReadOnlyList<NotableDate> ResolveYear(int year, INotableDateService service, string territory, NotableDateFilter? filter)
    {
        DateRange range = new(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31));
        return filter is null ? service.Resolve(range, territory) : service.Resolve(range, territory, filter);
    }

    /// <summary>
    /// Steps from a date in the given direction until the first working day is reached.
    /// </summary>
    /// <param name="date">The starting date.</param>
    /// <param name="direction">The step direction, <c>1</c> forward or <c>-1</c> backward.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The first working day strictly past the starting date in the direction.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">No working day is found within the traversal guard.</exception>
    private static DateOnly Step(DateOnly date, int direction, INotableDateService service, string territory, WeekPattern? workingWeek)
    {
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfNull(territory);

        for (var probe = 0; probe < TraversalGuard; probe++)
        {
            date = date.AddDays(direction);
            if (date.IsWorkingDay(service, territory, workingWeek))
                return date;
        }

        throw new InvalidOperationException(CalendarResourceStrings.Op_Invalid_NoWorkingDayFound);
    }

    /// <summary>
    /// Steps from a date in the given direction until the first non-working day is reached.
    /// </summary>
    /// <param name="date">The starting date.</param>
    /// <param name="direction">The step direction, <c>1</c> forward or <c>-1</c> backward.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The first non-working day strictly past the starting date in the direction.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">No non-working day is found within the traversal guard.</exception>
    private static DateOnly StepNonWorking(DateOnly date, int direction, INotableDateService service, string territory, WeekPattern? workingWeek)
    {
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfNull(territory);

        for (var probe = 0; probe < TraversalGuard; probe++)
        {
            date = date.AddDays(direction);
            if (date.IsNonWorkingDay(service, territory, workingWeek))
                return date;
        }

        throw new InvalidOperationException(CalendarResourceStrings.Op_Invalid_NoNonWorkingDayFound);
    }
}
