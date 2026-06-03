// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekdayNearDateStrategy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Calculates a notable date that falls on a weekday on, before, after, or nearest to a fixed month and day, such as
/// the Monday on or after 24 May.
/// </summary>
public sealed class WeekdayNearDateStrategy : IDateCalculationStrategy
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

        this.Month = month;
        this.Day = day;
        this.DayOfWeek = dayOfWeek;
        this.Direction = direction;
    }

    /// <summary>
    /// Gets the one-based anchor month.
    /// </summary>
    /// <returns>The anchor month.</returns>
    public int Month { get; }

    /// <summary>
    /// Gets the one-based anchor day.
    /// </summary>
    /// <returns>The anchor day.</returns>
    public int Day { get; }

    /// <summary>
    /// Gets the weekday to seek.
    /// </summary>
    /// <returns>The target weekday.</returns>
    public DayOfWeek DayOfWeek { get; }

    /// <summary>
    /// Gets the direction and inclusivity applied when seeking the weekday.
    /// </summary>
    /// <returns>The configured <see cref="WeekdayProximity" />.</returns>
    public WeekdayProximity Direction { get; }

    /// <inheritdoc />
    public DateOnly? Calculate(int year, StrategyResolutionContext context)
    {
        if (year < 1 || year > 9999 || this.Day > DateTime.DaysInMonth(year, this.Month))
            return null;

        return WeekdayMath.Seek(new DateOnly(year, this.Month, this.Day), this.DayOfWeek, this.Direction);
    }
}
