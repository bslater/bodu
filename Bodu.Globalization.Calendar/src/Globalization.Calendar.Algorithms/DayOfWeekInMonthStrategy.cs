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
/// <remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // The rule shape this strategy realizes - an nth-weekday-of-month floater
/// // (United States Labor Day: the first Monday of September):
/// NotableDateResource resource = NotableDateDocumentBuilder.Create("demo")
///     .AddNotableDate("labor-day", "Labor Day", NotableDateCategory.PublicHoliday, c => c
///         .AddRule("default", r => r.DayOfWeekInMonth(9, DayOfWeek.Monday, WeekOrdinal.First)))
///     .Build();
///]]>
/// </code>
/// </example>
/// </remarks>
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
    /// <value>The month, where 1 is January and 12 is December.</value>
    public int Month { get; }

    /// <summary>
    /// Gets the weekday to select.
    /// </summary>
    /// <value>The target weekday.</value>
    public DayOfWeek DayOfWeek { get; }

    /// <summary>
    /// Gets the occurrence of the weekday to select.
    /// </summary>
    /// <value>The target <see cref="WeekOrdinal" />.</value>
    public WeekOrdinal WeekOrdinal { get; }

    /// <inheritdoc />
    public DateOnly? Calculate(int year, StrategyResolutionContext context) =>
        WeekdayMath.NthWeekdayInMonth(year, Month, DayOfWeek, WeekOrdinal);
}
