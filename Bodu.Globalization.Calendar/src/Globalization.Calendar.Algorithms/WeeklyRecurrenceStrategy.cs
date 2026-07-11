// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeeklyRecurrenceStrategy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Generates occurrences on one or more selected weekdays every fixed number of weeks, such as every Monday and Friday
/// or every second Tuesday.
/// </summary>
/// <remarks>
/// <para>
/// The recurrence is anchor-based and day-of-week based; it never consults
/// <see cref="System.Globalization.CultureInfo.CurrentCulture" /> for a week boundary. For a weekly interval an anchor
/// is optional because every week participates; for a multi-week interval an anchor is required and defines the phase
/// of each selected weekday series. Occurrences are returned in ascending chronological order regardless of the order
/// the weekdays were declared, with no duplicates.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Every second Tuesday, phased from 6 January 2026.
/// var recurrence = new WeeklyRecurrenceStrategy(new[] { DayOfWeek.Tuesday }, 2, new DateOnly(2026, 1, 6));
///]]>
/// </code>
/// </example>
/// </remarks>
/// <seealso cref="IDateRecurrenceStrategy" />
public sealed class WeeklyRecurrenceStrategy
    : IDateRecurrenceStrategy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WeeklyRecurrenceStrategy" /> class.
    /// </summary>
    /// <param name="daysOfWeek">The weekdays to generate occurrences on.</param>
    /// <param name="intervalWeeks">The number of weeks between consecutive occurrences of each weekday.</param>
    /// <param name="anchorDate">
    /// The anchor date that phases each weekday series, or <see langword="null" /> when the interval is one week.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="daysOfWeek" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="intervalWeeks" /> is less than one, or a member of <paramref name="daysOfWeek" /> is not a defined
    /// <see cref="DayOfWeek" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="daysOfWeek" /> is empty or contains duplicates, or <paramref name="intervalWeeks" /> is greater
    /// than one while <paramref name="anchorDate" /> is <see langword="null" />.
    /// </exception>
    public WeeklyRecurrenceStrategy(IEnumerable<DayOfWeek> daysOfWeek, int intervalWeeks = 1, DateOnly? anchorDate = null)
    {
        ThrowHelper.ThrowIfNull(daysOfWeek);
        ThrowHelper.ThrowIfLessThan(intervalWeeks, 1);

        var days = daysOfWeek.ToList();
        foreach (DayOfWeek day in days)
            ThrowHelper.ThrowIfEnumValueIsUndefined(day);

        if (days.Count == 0)
            throw new ArgumentException(CalendarResourceStrings.Arg_Invalid_RecurrenceWeekdaysEmpty, nameof(daysOfWeek));
        if (days.Distinct().Count() != days.Count)
            throw new ArgumentException(CalendarResourceStrings.Arg_Invalid_RecurrenceWeekdaysDuplicate, nameof(daysOfWeek));
        if (intervalWeeks > 1 && anchorDate is null)
            throw new ArgumentException(CalendarResourceStrings.Arg_Invalid_RecurrenceAnchorRequired, nameof(anchorDate));

        DaysOfWeek = [.. days.OrderBy(d => (int)d)];
        IntervalWeeks = intervalWeeks;
        AnchorDate = anchorDate;
    }

    /// <summary>
    /// Gets the weekdays occurrences are generated on, in ascending order.
    /// </summary>
    /// <value>The distinct, ordered weekdays.</value>
    public IReadOnlyList<DayOfWeek> DaysOfWeek { get; }

    /// <summary>
    /// Gets the number of weeks between consecutive occurrences of each weekday.
    /// </summary>
    /// <value>The interval in weeks.</value>
    public int IntervalWeeks { get; }

    /// <summary>
    /// Gets the anchor date that phases each weekday series.
    /// </summary>
    /// <value>The anchor date, or <see langword="null" /> when the interval is one week.</value>
    public DateOnly? AnchorDate { get; }

    /// <inheritdoc />
    public IEnumerable<DateOnly> GetOccurrences(DateRange range, StrategyResolutionContext context)
    {
        if (range.StartDate > range.EndDate)
            return [];

        int strideDays = IntervalWeeks * 7;
        SortedSet<DateOnly> occurrences = new();

        foreach (DayOfWeek day in DaysOfWeek)
        {
            // A multi-week interval phases from the anchor's first matching weekday; a weekly interval selects every
            // week, so any matching weekday in the window is a valid, query-window-invariant phase origin.
            DateOnly seriesAnchor = AnchorDate is DateOnly anchor
                ? anchor.NextOrSameDateOfWeek(day)
                : range.StartDate.NextOrSameDateOfWeek(day);

            foreach (DateOnly occurrence in RecurrenceEnumeration.Stride(seriesAnchor, strideDays, range))
                occurrences.Add(occurrence);
        }

        return occurrences;
    }
}
