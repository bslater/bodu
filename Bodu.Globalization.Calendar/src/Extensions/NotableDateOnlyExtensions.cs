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
/// <see cref="EnumerateNotableDates(DateOnly, DateOnly, INotableDateService, string, NotableDateFilter)" /> for large
/// windows.
/// </para>
/// <para>
/// <strong>Method groups.</strong> <c>IsWeekend</c> / <c>IsWorkingDay</c> / <c>IsNonWorkingDay</c> /
/// <c>IsNotableDate</c> test a single day; <c>NextWorkingDay</c> / <c>PreviousWorkingDay</c> / <c>SnapToWorkingDay</c>
/// (and the <c>NonWorkingDay</c> / <c>NotableDate</c> variants) traverse to a nearby day; <c>AddWorkingDays</c> /
/// <c>WorkingDaysBetween</c> count working days; and <c>EnumerateWorkingDays</c> / <c>EnumerateNonWorkingDays</c> /
/// <c>EnumerateNotableDates</c> stream a window.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// INotableDateService service = AmericasCalendarData.CreateService("US");
/// DateOnly date = new(2026, 7, 3); // Friday, observed Independence Day in the US
///
/// // Is this a working day?
/// bool working = date.IsWorkingDay(service, "US"); // false
///
/// // Settlement date three working days out, skipping weekends and holidays.
/// DateOnly settles = date.AddWorkingDays(3, service, "US");
///
/// // The next working day on or after a candidate date.
/// DateOnly resume = date.SnapToWorkingDay(service, "US");
///
/// // Every notable date in July 2026.
/// IReadOnlyList<NotableDate> july = date.GetNotableDatesInMonth(service, "US");
///]]>
/// </code>
/// </example>
/// <seealso cref="INotableDateService" /> <seealso cref="NotableDate" /> <seealso cref="NotableDateFilter" />
/// <seealso cref="WeekPattern" /> <seealso href="../guides/calendar/working-days.html">Working-day arithmetic (guide)
/// </seealso>
public static partial class NotableDateOnlyExtensions
{
    /// <summary>
    /// The maximum number of days a traversal will probe before giving up, guarding against a degenerate working week.
    /// </summary>
    private const int TraversalGuard = 4000;

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
