// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RelativeWeekdayInMonthStrategy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Calculates a notable date that falls on a weekday relative to an anchor weekday occurrence within a month, such as
/// the Tuesday after the first Monday of November (United States general election day).
/// </summary>
/// <seealso cref="IDateCalculationStrategy" /> <seealso href="../guides/calendar/rule-authoring.html">Authoring notable
/// date rules (guide)</seealso>
public sealed class RelativeWeekdayInMonthStrategy : IDateCalculationStrategy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RelativeWeekdayInMonthStrategy" /> class.
    /// </summary>
    /// <param name="month">The one-based month of the anchor.</param>
    /// <param name="dayOfWeek">The anchor weekday.</param>
    /// <param name="weekOrdinal">The occurrence of the anchor weekday in the month.</param>
    /// <param name="relativeDayOfWeek">The weekday to seek relative to the anchor.</param>
    /// <param name="direction">The direction and inclusivity to apply when seeking the relative weekday.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="month" /> is not between 1 and 12.</exception>
    public RelativeWeekdayInMonthStrategy(
        int month,
        DayOfWeek dayOfWeek,
        WeekOrdinal weekOrdinal,
        DayOfWeek relativeDayOfWeek,
        WeekdayProximity direction)
    {
        ThrowHelper.ThrowIfOutOfRange(month, 1, 12);

        Month = month;
        DayOfWeek = dayOfWeek;
        WeekOrdinal = weekOrdinal;
        RelativeDayOfWeek = relativeDayOfWeek;
        Direction = direction;
    }

    /// <summary>
    /// Gets the one-based month of the anchor.
    /// </summary>
    /// <returns>The anchor month.</returns>
    public int Month { get; }

    /// <summary>
    /// Gets the anchor weekday.
    /// </summary>
    /// <returns>The anchor weekday.</returns>
    public DayOfWeek DayOfWeek { get; }

    /// <summary>
    /// Gets the occurrence of the anchor weekday in the month.
    /// </summary>
    /// <returns>The anchor <see cref="WeekOrdinal" />.</returns>
    public WeekOrdinal WeekOrdinal { get; }

    /// <summary>
    /// Gets the weekday to seek relative to the anchor.
    /// </summary>
    /// <returns>The relative weekday.</returns>
    public DayOfWeek RelativeDayOfWeek { get; }

    /// <summary>
    /// Gets the direction and inclusivity applied when seeking the relative weekday.
    /// </summary>
    /// <returns>The configured <see cref="WeekdayProximity" />.</returns>
    public WeekdayProximity Direction { get; }

    /// <inheritdoc />
    public DateOnly? Calculate(int year, StrategyResolutionContext context)
    {
        if (WeekdayMath.NthWeekdayInMonth(year, Month, DayOfWeek, WeekOrdinal) is not DateOnly anchor)
            return null;

        return WeekdayMath.SeekOrNull(anchor, RelativeDayOfWeek, Direction);
    }
}
