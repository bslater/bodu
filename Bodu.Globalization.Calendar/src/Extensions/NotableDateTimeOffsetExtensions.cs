// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeOffsetExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

/// <summary>
/// Provides working-day, traversal, and notable-date query extension methods over <see cref="DateTimeOffset" />,
/// delegating to the <see cref="DateOnly" /> surface on the offset-local date component.
/// </summary>
/// <remarks>
/// <para>
/// The date component is the offset-local wall-clock date (<see cref="DateTimeOffset.DateTime" />). Traversal methods
/// that return a moved <see cref="DateTimeOffset" /> preserve the original time-of-day and
/// <see cref="DateTimeOffset.Offset" />.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// INotableDateService service = AmericasCalendarData.CreateService("US");
/// DateTimeOffset stamp = new(2026, 7, 3, 14, 0, 0, TimeSpan.FromHours(-5)); // observed Independence Day
///
/// bool working = stamp.IsWorkingDay(service, "US"); // false
///
/// // The next working day keeps the original time-of-day and -05:00 offset.
/// DateTimeOffset resume = stamp.NextWorkingDay(service, "US");
///]]>
/// </code>
/// </example>
/// <seealso cref="NotableDateOnlyExtensions" />
/// <seealso cref="INotableDateService" />
/// <seealso href="../guides/calendar/working-days.html">Working-day arithmetic (guide)</seealso>
public static class NotableDateTimeOffsetExtensions
{
    /// <summary>
    /// Determines whether the date falls outside the working week.
    /// </summary>
    /// <param name="date">The value whose offset-local date is tested.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns><see langword="true" /> if the date is not a working-week day; otherwise <see langword="false" />.</returns>
    public static bool IsWeekend(this DateTimeOffset date, WeekPattern? workingWeek = null) =>
        DateOnly.FromDateTime(date.DateTime).IsWeekend(workingWeek);

    /// <summary>
    /// Determines whether the date is a non-working day for the territory.
    /// </summary>
    /// <param name="date">The value whose offset-local date is tested.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns><see langword="true" /> if the date is non-working; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static bool IsNonWorkingDay(this DateTimeOffset date, INotableDateService service, string territory, WeekPattern? workingWeek = null) =>
        DateOnly.FromDateTime(date.DateTime).IsNonWorkingDay(service, territory, workingWeek);

    /// <summary>
    /// Determines whether the date is a working day for the territory.
    /// </summary>
    /// <param name="date">The value whose offset-local date is tested.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns><see langword="true" /> if the date is a working day; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static bool IsWorkingDay(this DateTimeOffset date, INotableDateService service, string territory, WeekPattern? workingWeek = null) =>
        DateOnly.FromDateTime(date.DateTime).IsWorkingDay(service, territory, workingWeek);

    /// <summary>
    /// Determines whether any notable date is emitted on the date for the territory.
    /// </summary>
    /// <param name="date">The value whose offset-local date is tested.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="filter">An optional filter the occurrence must satisfy.</param>
    /// <returns><see langword="true" /> if at least one occurrence is emitted; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static bool IsNotableDate(this DateTimeOffset date, INotableDateService service, string territory, NotableDateFilter? filter = null) =>
        DateOnly.FromDateTime(date.DateTime).IsNotableDate(service, territory, filter);

    /// <summary>
    /// Gets the notable dates emitted on the date for the territory.
    /// </summary>
    /// <param name="date">The value whose offset-local date is resolved.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="filter">An optional filter the occurrences must satisfy.</param>
    /// <returns>The emitted occurrences; empty when there are none.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static IReadOnlyList<NotableDate> GetNotableDates(this DateTimeOffset date, INotableDateService service, string territory, NotableDateFilter? filter = null) =>
        DateOnly.FromDateTime(date.DateTime).GetNotableDates(service, territory, filter);

    /// <summary>
    /// Returns the first working day strictly after the date, preserving the time-of-day and offset.
    /// </summary>
    /// <param name="date">The starting date.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The next working day at the original time-of-day and offset.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static DateTimeOffset NextWorkingDay(this DateTimeOffset date, INotableDateService service, string territory, WeekPattern? workingWeek = null) =>
        WithTimeOf(date, DateOnly.FromDateTime(date.DateTime).NextWorkingDay(service, territory, workingWeek));

    /// <summary>
    /// Returns the first working day strictly before the date, preserving the time-of-day and offset.
    /// </summary>
    /// <param name="date">The starting date.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The previous working day at the original time-of-day and offset.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static DateTimeOffset PreviousWorkingDay(this DateTimeOffset date, INotableDateService service, string territory, WeekPattern? workingWeek = null) =>
        WithTimeOf(date, DateOnly.FromDateTime(date.DateTime).PreviousWorkingDay(service, territory, workingWeek));

    /// <summary>
    /// Returns the date if it is a working day; otherwise the next working day, preserving the time-of-day and offset.
    /// </summary>
    /// <param name="date">The date to snap.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The date or the next working day, at the original time-of-day and offset.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static DateTimeOffset SnapToWorkingDay(this DateTimeOffset date, INotableDateService service, string territory, WeekPattern? workingWeek = null) =>
        WithTimeOf(date, DateOnly.FromDateTime(date.DateTime).SnapToWorkingDay(service, territory, workingWeek));

    /// <summary>
    /// Returns the date if it is a working day; otherwise the previous working day, preserving the time-of-day and
    /// offset.
    /// </summary>
    /// <param name="date">The date to snap.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The date or the previous working day, at the original time-of-day and offset.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static DateTimeOffset SnapToWorkingDayBackward(this DateTimeOffset date, INotableDateService service, string territory, WeekPattern? workingWeek = null) =>
        WithTimeOf(date, DateOnly.FromDateTime(date.DateTime).SnapToWorkingDayBackward(service, territory, workingWeek));

    /// <summary>
    /// Returns the date if it is a working day; otherwise the nearest working day, preserving the time-of-day and
    /// offset.
    /// </summary>
    /// <param name="date">The date to snap.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The nearest working day at the original time-of-day and offset.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static DateTimeOffset SnapToNearestWorkingDay(this DateTimeOffset date, INotableDateService service, string territory, WeekPattern? workingWeek = null) =>
        WithTimeOf(date, DateOnly.FromDateTime(date.DateTime).SnapToNearestWorkingDay(service, territory, workingWeek));

    /// <summary>
    /// Advances the date by a signed number of working days, preserving the time-of-day and offset.
    /// </summary>
    /// <param name="date">The starting date.</param>
    /// <param name="count">The number of working days to add; negative retreats, zero returns the date unchanged.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The resulting date at the original time-of-day and offset.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static DateTimeOffset AddWorkingDays(this DateTimeOffset date, int count, INotableDateService service, string territory, WeekPattern? workingWeek = null) =>
        WithTimeOf(date, DateOnly.FromDateTime(date.DateTime).AddWorkingDays(count, service, territory, workingWeek));

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
    public static int WorkingDaysBetween(this DateTimeOffset start, DateTimeOffset end, INotableDateService service, string territory, WeekPattern? workingWeek = null) =>
        DateOnly.FromDateTime(start.DateTime).WorkingDaysBetween(DateOnly.FromDateTime(end.DateTime), service, territory, workingWeek);

    /// <summary>
    /// Lazily enumerates the working days in the inclusive range, in ascending order, each at the start's time-of-day
    /// and offset.
    /// </summary>
    /// <param name="start">The inclusive start date.</param>
    /// <param name="end">The inclusive end date.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The working days in the range at the start's time-of-day and offset.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static IEnumerable<DateTimeOffset> EnumerateWorkingDays(this DateTimeOffset start, DateTimeOffset end, INotableDateService service, string territory, WeekPattern? workingWeek = null)
    {
        TimeOnly time = TimeOnly.FromTimeSpan(start.TimeOfDay);
        TimeSpan offset = start.Offset;

        return DateOnly.FromDateTime(start.DateTime)
            .EnumerateWorkingDays(DateOnly.FromDateTime(end.DateTime), service, territory, workingWeek)
            .Select(day => new DateTimeOffset(day.ToDateTime(time), offset));
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
    public static IReadOnlyList<NotableDate> EnumerateNotableDates(this DateTimeOffset start, DateTimeOffset end, INotableDateService service, string territory, NotableDateFilter? filter = null) =>
        DateOnly.FromDateTime(start.DateTime).EnumerateNotableDates(DateOnly.FromDateTime(end.DateTime), service, territory, filter);

    /// <summary>
    /// Reattaches the time-of-day and offset of an original <see cref="DateTimeOffset" /> to a computed date.
    /// </summary>
    /// <param name="original">The original value supplying the time-of-day and offset.</param>
    /// <param name="date">The computed date.</param>
    /// <returns>The computed date combined with the original time-of-day and offset.</returns>
    private static DateTimeOffset WithTimeOf(DateTimeOffset original, DateOnly date) =>
        new(date.ToDateTime(TimeOnly.FromTimeSpan(original.TimeOfDay)), original.Offset);
}
