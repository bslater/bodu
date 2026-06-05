// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

/// <summary>
/// Provides working-day, traversal, and notable-date query extension methods over <see cref="DateTime" />, delegating
/// to the <see cref="DateOnly" /> surface on the date component.
/// </summary>
/// <remarks>
/// <para>
/// Predicate and query methods evaluate the date component only. Traversal methods that return a moved
/// <see cref="DateTime" /> preserve the original time-of-day and <see cref="DateTime.Kind" />.
/// </para>
/// </remarks>
public static class NotableDateTimeExtensions
{
    /// <summary>
    /// Determines whether the date falls outside the working week.
    /// </summary>
    /// <param name="date">The date whose date component is tested.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>
    /// <see langword="true" /> if the date is not a working-week day; otherwise <see langword="false" />.
    /// </returns>
    public static bool IsWeekend(this DateTime date, WeekPattern? workingWeek = null) =>
        DateOnly.FromDateTime(date).IsWeekend(workingWeek);

    /// <summary>
    /// Determines whether the date is a non-working day for the territory.
    /// </summary>
    /// <param name="date">The date whose date component is tested.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns><see langword="true" /> if the date is non-working; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static bool IsNonWorkingDay(this DateTime date, INotableDateService service, string territory, WeekPattern? workingWeek = null) =>
        DateOnly.FromDateTime(date).IsNonWorkingDay(service, territory, workingWeek);

    /// <summary>
    /// Determines whether the date is a working day for the territory.
    /// </summary>
    /// <param name="date">The date whose date component is tested.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns><see langword="true" /> if the date is a working day; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static bool IsWorkingDay(this DateTime date, INotableDateService service, string territory, WeekPattern? workingWeek = null) =>
        DateOnly.FromDateTime(date).IsWorkingDay(service, territory, workingWeek);

    /// <summary>
    /// Determines whether any notable date is emitted on the date for the territory.
    /// </summary>
    /// <param name="date">The date whose date component is tested.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="filter">An optional filter the occurrence must satisfy.</param>
    /// <returns>
    /// <see langword="true" /> if at least one occurrence is emitted; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static bool IsNotableDate(this DateTime date, INotableDateService service, string territory, NotableDateFilter? filter = null) =>
        DateOnly.FromDateTime(date).IsNotableDate(service, territory, filter);

    /// <summary>
    /// Gets the notable dates emitted on the date for the territory.
    /// </summary>
    /// <param name="date">The date whose date component is resolved.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="filter">An optional filter the occurrences must satisfy.</param>
    /// <returns>The emitted occurrences; empty when there are none.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static IReadOnlyList<NotableDate> GetNotableDates(this DateTime date, INotableDateService service, string territory, NotableDateFilter? filter = null) =>
        DateOnly.FromDateTime(date).GetNotableDates(service, territory, filter);

    /// <summary>
    /// Returns the first working day strictly after the date, preserving the time-of-day and kind.
    /// </summary>
    /// <param name="date">The starting date.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The next working day at the original time-of-day.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static DateTime NextWorkingDay(this DateTime date, INotableDateService service, string territory, WeekPattern? workingWeek = null) =>
        WithTimeOf(date, DateOnly.FromDateTime(date).NextWorkingDay(service, territory, workingWeek));

    /// <summary>
    /// Returns the first working day strictly before the date, preserving the time-of-day and kind.
    /// </summary>
    /// <param name="date">The starting date.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The previous working day at the original time-of-day.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static DateTime PreviousWorkingDay(this DateTime date, INotableDateService service, string territory, WeekPattern? workingWeek = null) =>
        WithTimeOf(date, DateOnly.FromDateTime(date).PreviousWorkingDay(service, territory, workingWeek));

    /// <summary>
    /// Returns the date if it is a working day; otherwise the next working day, preserving the time-of-day and kind.
    /// </summary>
    /// <param name="date">The date to snap.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The date or the next working day, at the original time-of-day.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static DateTime SnapToWorkingDay(this DateTime date, INotableDateService service, string territory, WeekPattern? workingWeek = null) =>
        WithTimeOf(date, DateOnly.FromDateTime(date).SnapToWorkingDay(service, territory, workingWeek));

    /// <summary>
    /// Returns the date if it is a working day; otherwise the previous working day, preserving the time-of-day and
    /// kind.
    /// </summary>
    /// <param name="date">The date to snap.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The date or the previous working day, at the original time-of-day.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static DateTime SnapToWorkingDayBackward(this DateTime date, INotableDateService service, string territory, WeekPattern? workingWeek = null) =>
        WithTimeOf(date, DateOnly.FromDateTime(date).SnapToWorkingDayBackward(service, territory, workingWeek));

    /// <summary>
    /// Returns the date if it is a working day; otherwise the nearest working day, preserving the time-of-day and kind.
    /// </summary>
    /// <param name="date">The date to snap.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The nearest working day at the original time-of-day.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static DateTime SnapToNearestWorkingDay(this DateTime date, INotableDateService service, string territory, WeekPattern? workingWeek = null) =>
        WithTimeOf(date, DateOnly.FromDateTime(date).SnapToNearestWorkingDay(service, territory, workingWeek));

    /// <summary>
    /// Advances the date by a signed number of working days, preserving the time-of-day and kind.
    /// </summary>
    /// <param name="date">The starting date.</param>
    /// <param name="count">
    /// The number of working days to add; negative retreats, zero returns the date unchanged.
    /// </param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The resulting date at the original time-of-day.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static DateTime AddWorkingDays(this DateTime date, int count, INotableDateService service, string territory, WeekPattern? workingWeek = null) =>
        WithTimeOf(date, DateOnly.FromDateTime(date).AddWorkingDays(count, service, territory, workingWeek));

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
    public static int WorkingDaysBetween(this DateTime start, DateTime end, INotableDateService service, string territory, WeekPattern? workingWeek = null) =>
        DateOnly.FromDateTime(start).WorkingDaysBetween(DateOnly.FromDateTime(end), service, territory, workingWeek);

    /// <summary>
    /// Lazily enumerates the working days in the inclusive range, in ascending order, each at the start's time-of-day
    /// and kind.
    /// </summary>
    /// <param name="start">The inclusive start date.</param>
    /// <param name="end">The inclusive end date.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The working days in the range at the start's time-of-day.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static IEnumerable<DateTime> EnumerateWorkingDays(this DateTime start, DateTime end, INotableDateService service, string territory, WeekPattern? workingWeek = null)
    {
        var time = TimeOnly.FromDateTime(start);
        DateTimeKind kind = start.Kind;

        return DateOnly.FromDateTime(start)
            .EnumerateWorkingDays(DateOnly.FromDateTime(end), service, territory, workingWeek)
            .Select(day => day.ToDateTime(time, kind));
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
    public static IReadOnlyList<NotableDate> EnumerateNotableDates(this DateTime start, DateTime end, INotableDateService service, string territory, NotableDateFilter? filter = null) =>
        DateOnly.FromDateTime(start).EnumerateNotableDates(DateOnly.FromDateTime(end), service, territory, filter);

    /// <summary>
    /// Returns the first non-working day strictly after the date, preserving the time-of-day and kind.
    /// </summary>
    /// <param name="date">The starting date.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The next non-working day at the original time-of-day.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">No non-working day is found within the traversal guard.</exception>
    public static DateTime NextNonWorkingDay(this DateTime date, INotableDateService service, string territory, WeekPattern? workingWeek = null) =>
        WithTimeOf(date, DateOnly.FromDateTime(date).NextNonWorkingDay(service, territory, workingWeek));

    /// <summary>
    /// Returns the first non-working day strictly before the date, preserving the time-of-day and kind.
    /// </summary>
    /// <param name="date">The starting date.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The previous non-working day at the original time-of-day.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">No non-working day is found within the traversal guard.</exception>
    public static DateTime PreviousNonWorkingDay(this DateTime date, INotableDateService service, string territory, WeekPattern? workingWeek = null) =>
        WithTimeOf(date, DateOnly.FromDateTime(date).PreviousNonWorkingDay(service, territory, workingWeek));

    /// <summary>
    /// Lazily enumerates the non-working days in the inclusive range, in ascending order, each at the start's
    /// time-of-day and kind.
    /// </summary>
    /// <param name="start">The inclusive start date.</param>
    /// <param name="end">The inclusive end date.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The non-working days in the range at the start's time-of-day.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static IEnumerable<DateTime> EnumerateNonWorkingDays(this DateTime start, DateTime end, INotableDateService service, string territory, WeekPattern? workingWeek = null)
    {
        var time = TimeOnly.FromDateTime(start);
        DateTimeKind kind = start.Kind;

        return DateOnly.FromDateTime(start)
            .EnumerateNonWorkingDays(DateOnly.FromDateTime(end), service, territory, workingWeek)
            .Select(day => day.ToDateTime(time, kind));
    }

    /// <summary>
    /// Returns the earliest notable date emitted strictly after the date for the territory.
    /// </summary>
    /// <param name="date">The reference date whose date component is used.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="filter">An optional filter the occurrence must satisfy.</param>
    /// <returns>
    /// The next matching occurrence, or <see langword="null" /> when none exists up to the maximum year.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static NotableDate? NextNotableDate(this DateTime date, INotableDateService service, string territory, NotableDateFilter? filter = null) =>
        DateOnly.FromDateTime(date).NextNotableDate(service, territory, filter);

    /// <summary>
    /// Returns the most recent notable date emitted strictly before the date for the territory.
    /// </summary>
    /// <param name="date">The reference date whose date component is used.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="filter">An optional filter the occurrence must satisfy.</param>
    /// <returns>
    /// The previous matching occurrence, or <see langword="null" /> when none exists down to the minimum year.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static NotableDate? PreviousNotableDate(this DateTime date, INotableDateService service, string territory, NotableDateFilter? filter = null) =>
        DateOnly.FromDateTime(date).PreviousNotableDate(service, territory, filter);

    /// <summary>
    /// Reattaches the time-of-day and kind of an original <see cref="DateTime" /> to a computed date.
    /// </summary>
    /// <param name="original">The original value supplying the time-of-day and kind.</param>
    /// <param name="date">The computed date.</param>
    /// <returns>The computed date combined with the original time-of-day and kind.</returns>
    private static DateTime WithTimeOf(DateTime original, DateOnly date) =>
        date.ToDateTime(TimeOnly.FromDateTime(original), original.Kind);
}
