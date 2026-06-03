// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixedDateStrategy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Calculates a notable date that falls on a fixed Gregorian month and day every year, such as 1 January or 25 April.
/// </summary>
public sealed class FixedDateStrategy : IDateCalculationStrategy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FixedDateStrategy" /> class.
    /// </summary>
    /// <param name="month">The one-based month of the occurrence.</param>
    /// <param name="day">The one-based day of the occurrence.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="month" /> is not between 1 and 12, or <paramref name="day" /> is not between 1 and 31.
    /// </exception>
    public FixedDateStrategy(int month, int day)
    {
        ThrowHelper.ThrowIfOutOfRange(month, 1, 12);
        ThrowHelper.ThrowIfOutOfRange(day, 1, 31);

        this.Month = month;
        this.Day = day;
    }

    /// <summary>
    /// Gets the one-based month of the occurrence.
    /// </summary>
    /// <returns>The month, where 1 is January and 12 is December.</returns>
    public int Month { get; }

    /// <summary>
    /// Gets the one-based day of the occurrence.
    /// </summary>
    /// <returns>The day of the month.</returns>
    public int Day { get; }

    /// <inheritdoc />
    public DateOnly? Calculate(int year)
    {
        if (year < 1 || year > 9999 || this.Day > DateTime.DaysInMonth(year, this.Month))
            return null;

        return new DateOnly(year, this.Month, this.Day);
    }
}
