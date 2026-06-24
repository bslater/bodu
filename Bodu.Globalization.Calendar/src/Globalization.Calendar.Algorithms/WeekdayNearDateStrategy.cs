// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekdayNearDateStrategy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Calculates a notable date that falls on a weekday on, before, after, or nearest to a fixed month and day, such as
/// the Monday on or after 24 May.
/// </summary>
/// <seealso cref="IDateCalculationStrategy" /> <seealso href="../guides/calendar/rule-authoring.html">Authoring notable
/// date rules (guide)</seealso>
public sealed class WeekdayNearDateStrategy
    : IDateCalculationStrategy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WeekdayNearDateStrategy" /> class.
    /// </summary>
    /// <param name="month">The one-based anchor month.</param>
    /// <param name="day">The one-based anchor day.</param>
    /// <param name="dayOfWeek">The weekday to seek.</param>
    /// <param name="direction">The direction and inclusivity to apply when seeking the weekday.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="month" /> is not between 1 and 12, or <paramref name="day" /> is not between 1 and 31.
    /// </exception>
    public WeekdayNearDateStrategy(int month, int day, DayOfWeek dayOfWeek, WeekdayProximity direction)
    {
        ThrowHelper.ThrowIfOutOfRange(month, 1, 12);
        ThrowHelper.ThrowIfOutOfRange(day, 1, 31);

        Month = month;
        Day = day;
        DayOfWeek = dayOfWeek;
        Direction = direction;
    }

    /// <summary>
    /// Gets the one-based anchor month.
    /// </summary>
    /// <value>The anchor month.</value>
    public int Month { get; }

    /// <summary>
    /// Gets the one-based anchor day.
    /// </summary>
    /// <value>The anchor day.</value>
    public int Day { get; }

    /// <summary>
    /// Gets the weekday to seek.
    /// </summary>
    /// <value>The target weekday.</value>
    public DayOfWeek DayOfWeek { get; }

    /// <summary>
    /// Gets the direction and inclusivity applied when seeking the weekday.
    /// </summary>
    /// <value>The configured <see cref="WeekdayProximity" />.</value>
    public WeekdayProximity Direction { get; }

    /// <inheritdoc />
    public DateOnly? Calculate(int year, StrategyResolutionContext context)
    {
        if (year < 1 || year > 9999 || Day > DateTime.DaysInMonth(year, Month))
            return null;

        return WeekdayMath.SeekOrNull(new DateOnly(year, Month, Day), DayOfWeek, Direction);
    }
}
