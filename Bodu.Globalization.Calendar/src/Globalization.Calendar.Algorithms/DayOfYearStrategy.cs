// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DayOfYearStrategy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Calculates a date by its ordinal position within a Gregorian year, such as the 100th day or the final day of the
/// year.
/// </summary>
/// <remarks>
/// <para>
/// A positive ordinal counts from 1 January (<c>1</c> is 1 January); a negative ordinal counts from 31 December (<c>-1</c>
/// is 31 December). A positive ordinal beyond the number of days in the year produces no occurrence, so day 366 exists
/// only in a leap year.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // The 256th day of the year (Programmers' Day).
/// var strategy = new DayOfYearStrategy(256);
///]]>
/// </code>
/// </example>
/// </remarks>
/// <seealso cref="IDateCalculationStrategy" /> <seealso href="../guides/calendar/rule-authoring.html">Authoring notable
/// date rules (guide)</seealso>
public sealed class DayOfYearStrategy
    : IDateCalculationStrategy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DayOfYearStrategy" /> class.
    /// </summary>
    /// <param name="ordinal">The signed ordinal position of the day within the year.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="ordinal" /> is zero or has an absolute value greater than 366.
    /// </exception>
    public DayOfYearStrategy(int ordinal)
    {
        ThrowHelper.ThrowIfOutOfRange(ordinal, -366, 366);
        if (ordinal == 0)
            throw new ArgumentOutOfRangeException(nameof(ordinal), CalendarResourceStrings.Arg_OutOfRange_OrdinalZero);

        Ordinal = ordinal;
    }

    /// <summary>
    /// Gets the signed ordinal position of the day within the year.
    /// </summary>
    /// <value>The ordinal; positive counts from 1 January, negative from 31 December.</value>
    public int Ordinal { get; }

    /// <inheritdoc />
    public DateOnly? Calculate(int year, StrategyResolutionContext context)
    {
        if (year is < 1 or > 9999)
            return null;

        int daysInYear = DateTime.IsLeapYear(year) ? 366 : 365;

        if (Ordinal > 0)
            return Ordinal <= daysInYear ? new DateOnly(year, 1, 1).AddDays(Ordinal - 1) : null;

        int day = daysInYear + Ordinal + 1;
        return day >= 1 ? new DateOnly(year, 1, 1).AddDays(day - 1) : null;
    }
}
