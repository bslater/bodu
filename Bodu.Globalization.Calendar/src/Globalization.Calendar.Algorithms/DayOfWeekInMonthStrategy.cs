// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DayOfWeekInMonthStrategy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Calculates a notable date that falls on the nth (or last) occurrence of a weekday within a month, such as the fourth
/// Thursday of November or the last Monday of May.
/// </summary>
/// <seealso cref="IDateCalculationStrategy" /> <seealso href="../guides/calendar/rule-authoring.html">Authoring notable
/// date rules (guide)</seealso>
public sealed class DayOfWeekInMonthStrategy
    : IDateCalculationStrategy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DayOfWeekInMonthStrategy" /> class.
    /// </summary>
    /// <param name="month">The one-based month of the occurrence.</param>
    /// <param name="dayOfWeek">The weekday to select.</param>
    /// <param name="weekOrdinal">The occurrence of the weekday to select.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="month" /> is not between 1 and 12.</exception>
    public DayOfWeekInMonthStrategy(int month, DayOfWeek dayOfWeek, WeekOrdinal weekOrdinal)
    {
        ThrowHelper.ThrowIfOutOfRange(month, 1, 12);

        Month = month;
        DayOfWeek = dayOfWeek;
        WeekOrdinal = weekOrdinal;
    }

    /// <summary>
    /// Gets the one-based month of the occurrence.
    /// </summary>
    /// <returns>The month, where 1 is January and 12 is December.</returns>
    public int Month { get; }

    /// <summary>
    /// Gets the weekday to select.
    /// </summary>
    /// <returns>The target weekday.</returns>
    public DayOfWeek DayOfWeek { get; }

    /// <summary>
    /// Gets the occurrence of the weekday to select.
    /// </summary>
    /// <returns>The target <see cref="WeekOrdinal" />.</returns>
    public WeekOrdinal WeekOrdinal { get; }

    /// <inheritdoc />
    public DateOnly? Calculate(int year, StrategyResolutionContext context) =>
        WeekdayMath.NthWeekdayInMonth(year, Month, DayOfWeek, WeekOrdinal);
}
