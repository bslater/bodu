// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonthlyWeekdayRecurrenceStrategy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Generates an ordinal weekday every fixed number of months, such as the first Monday of every month or the last
/// Friday of every third month.
/// </summary>
/// <remarks>
/// <para>
/// The ordinal weekday in each participating month is calculated with <see cref="WeekdayMath.NthWeekdayInMonth" />, so
/// <see cref="WeekOrdinal.Last" /> selects the final matching weekday and a month without a fifth matching
/// weekday simply produces no occurrence for that month rather than invalidating the recurrence. For a monthly interval
/// an anchor is optional; a multi-month interval requires an anchor whose year and month define month zero.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // The last Friday of every month.
/// var recurrence = new MonthlyWeekdayRecurrenceStrategy(DayOfWeek.Friday, WeekOrdinal.Last);
///]]>
/// </code>
/// </example>
/// </remarks>
/// <seealso cref="IDateRecurrenceStrategy" />
public sealed class MonthlyWeekdayRecurrenceStrategy
    : IDateRecurrenceStrategy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MonthlyWeekdayRecurrenceStrategy" /> class.
    /// </summary>
    /// <param name="dayOfWeek">The weekday to select in each participating month.</param>
    /// <param name="weekOrdinal">The ordinal position of the weekday within the month.</param>
    /// <param name="intervalMonths">The number of months between consecutive occurrences.</param>
    /// <param name="anchorDate">
    /// The anchor whose year and month define month zero, or <see langword="null" /> when the interval is one month.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="dayOfWeek" /> or <paramref name="weekOrdinal" /> is not a defined enumeration value, or
    /// <paramref name="intervalMonths" /> is less than one.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="intervalMonths" /> is greater than one while <paramref name="anchorDate" /> is <see langword="null" />.
    /// </exception>
    public MonthlyWeekdayRecurrenceStrategy(
        DayOfWeek dayOfWeek,
        WeekOrdinal weekOrdinal,
        int intervalMonths = 1,
        DateOnly? anchorDate = null)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);
        ThrowHelper.ThrowIfEnumValueIsUndefined(weekOrdinal);
        ThrowHelper.ThrowIfLessThan(intervalMonths, 1);

        if (intervalMonths > 1 && anchorDate is null)
            throw new ArgumentException(CalendarResourceStrings.Arg_Invalid_RecurrenceAnchorRequired, nameof(anchorDate));

        DayOfWeek = dayOfWeek;
        WeekOrdinal = weekOrdinal;
        IntervalMonths = intervalMonths;
        AnchorDate = anchorDate;
    }

    /// <summary>
    /// Gets the weekday selected in each participating month.
    /// </summary>
    /// <value>The target weekday.</value>
    public DayOfWeek DayOfWeek { get; }

    /// <summary>
    /// Gets the ordinal position of the weekday within the month.
    /// </summary>
    /// <value>The configured <see cref="Bodu.Extensions.WeekOrdinal" /> position.</value>
    public WeekOrdinal WeekOrdinal { get; }

    /// <summary>
    /// Gets the number of months between consecutive occurrences.
    /// </summary>
    /// <value>The interval in months.</value>
    public int IntervalMonths { get; }

    /// <summary>
    /// Gets the anchor whose year and month define month zero.
    /// </summary>
    /// <value>The anchor date, or <see langword="null" /> when the interval is one month.</value>
    public DateOnly? AnchorDate { get; }

    /// <inheritdoc />
    public IEnumerable<DateOnly> GetOccurrences(DateRange range, StrategyResolutionContext context)
    {
        foreach ((int year, int month) in RecurrenceEnumeration.Months(AnchorDate, IntervalMonths, range))
        {
            if (WeekdayMath.NthWeekdayInMonth(year, month, DayOfWeek, WeekOrdinal) is not DateOnly candidate)
                continue;

            if (candidate >= range.StartDate && candidate <= range.EndDate)
                yield return candidate;
        }
    }
}
