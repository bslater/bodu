// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonthlyDayRecurrenceStrategy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Generates an occurrence on a specified calendar day every fixed number of months, such as the 15th of every month or
/// the 20th of every third month.
/// </summary>
/// <remarks>
/// <para>
/// Month iteration uses a stable month anchor and re-applies the day each month, so no month-to-month clamp drift
/// occurs: a day-31 recurrence still evaluates March as the 31st even though February has no 31st. A month that does
/// not contain the requested day is handled by <see cref="InvalidDayOfMonthBehavior" /> — either skipped or emitted as
/// its last calendar day. For a monthly interval an anchor is optional; a multi-month interval requires an anchor whose
/// year and month define month zero.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // The 31st of every month, using the last day of shorter months.
/// var recurrence = new MonthlyDayRecurrenceStrategy(31, 1, null, InvalidDayOfMonthBehavior.UseLastDayOfMonth);
///]]>
/// </code>
/// </example>
/// </remarks>
/// <seealso cref="IDateRecurrenceStrategy" />
public sealed class MonthlyDayRecurrenceStrategy
    : IDateRecurrenceStrategy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MonthlyDayRecurrenceStrategy" /> class.
    /// </summary>
    /// <param name="dayOfMonth">The one-based day of the month to generate occurrences on.</param>
    /// <param name="intervalMonths">The number of months between consecutive occurrences.</param>
    /// <param name="anchorDate">
    /// The anchor whose year and month define month zero, or <see langword="null" /> when the interval is one month.
    /// </param>
    /// <param name="invalidDayBehavior">How a month that does not contain the requested day is handled.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="dayOfMonth" /> is not between 1 and 31, <paramref name="intervalMonths" /> is less than one, or
    /// <paramref name="invalidDayBehavior" /> is not a defined <see cref="InvalidDayOfMonthBehavior" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="intervalMonths" /> is greater than one while <paramref name="anchorDate" /> is
    /// <see langword="null" />.
    /// </exception>
    public MonthlyDayRecurrenceStrategy(
        int dayOfMonth,
        int intervalMonths = 1,
        DateOnly? anchorDate = null,
        InvalidDayOfMonthBehavior invalidDayBehavior = InvalidDayOfMonthBehavior.Skip)
    {
        ThrowHelper.ThrowIfOutOfRange(dayOfMonth, 1, 31);
        ThrowHelper.ThrowIfLessThan(intervalMonths, 1);
        ThrowHelper.ThrowIfEnumValueIsUndefined(invalidDayBehavior);

        if (intervalMonths > 1 && anchorDate is null)
            throw new ArgumentException(CalendarResourceStrings.Arg_Invalid_RecurrenceAnchorRequired, nameof(anchorDate));

        DayOfMonth = dayOfMonth;
        IntervalMonths = intervalMonths;
        AnchorDate = anchorDate;
        InvalidDayBehavior = invalidDayBehavior;
    }

    /// <summary>
    /// Gets the one-based day of the month occurrences are generated on.
    /// </summary>
    /// <value>The day of the month.</value>
    public int DayOfMonth { get; }

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

    /// <summary>
    /// Gets the behavior applied to a month that does not contain the requested day.
    /// </summary>
    /// <value>The configured <see cref="InvalidDayOfMonthBehavior" />.</value>
    public InvalidDayOfMonthBehavior InvalidDayBehavior { get; }

    /// <inheritdoc />
    public IEnumerable<DateOnly> GetOccurrences(DateRange range, StrategyResolutionContext context)
    {
        foreach ((int year, int month) in RecurrenceEnumeration.Months(AnchorDate, IntervalMonths, range))
        {
            int daysInMonth = DateTime.DaysInMonth(year, month);

            DateOnly candidate;
            if (DayOfMonth <= daysInMonth)
            {
                candidate = new DateOnly(year, month, DayOfMonth);
            }
            else if (InvalidDayBehavior == InvalidDayOfMonthBehavior.UseLastDayOfMonth)
            {
                candidate = new DateOnly(year, month, daysInMonth);
            }
            else
            {
                continue;
            }

            if (candidate >= range.StartDate && candidate <= range.EndDate)
                yield return candidate;
        }
    }
}
