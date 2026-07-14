// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OrdinalDayOfMonthStrategy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Calculates a day by its ordinal position from the beginning or end of a Gregorian month, such as the last day of
/// February or the second-last day of a month.
/// </summary>
/// <remarks>
/// <para>
/// A positive ordinal counts from the start of the month (<c>1</c> is the first day); a negative ordinal counts from
/// the end (<c>-1</c> is the last day). The result always stays within the specified month: a positive ordinal that
/// does not exist in a particular year and month produces no occurrence.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // The last day of February (28 or 29 depending on the year).
/// var strategy = new OrdinalDayOfMonthStrategy(2, -1);
///]]>
/// </code>
/// </example>
/// </remarks>
/// <seealso cref="IDateCalculationStrategy" /> <seealso href="../guides/calendar/rule-authoring.html">Authoring notable
/// date rules (guide)</seealso>
public sealed class OrdinalDayOfMonthStrategy
    : IDateCalculationStrategy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrdinalDayOfMonthStrategy" /> class.
    /// </summary>
    /// <param name="month">The one-based month.</param>
    /// <param name="ordinal">The signed ordinal position of the day within the month.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="month" /> is not between 1 and 12, or <paramref name="ordinal" /> is zero or has an absolute
    /// value greater than 31.
    /// </exception>
    public OrdinalDayOfMonthStrategy(int month, int ordinal)
    {
        ThrowHelper.ThrowIfOutOfRange(month, 1, 12);
        ThrowHelper.ThrowIfOutOfRange(ordinal, -31, 31);
        if (ordinal == 0)
            throw new ArgumentOutOfRangeException(nameof(ordinal), CalendarResourceStrings.Arg_OutOfRange_OrdinalZero);

        Month = month;
        Ordinal = ordinal;
    }

    /// <summary>
    /// Gets the one-based month.
    /// </summary>
    /// <value>The month.</value>
    public int Month { get; }

    /// <summary>
    /// Gets the signed ordinal position of the day within the month.
    /// </summary>
    /// <value>The ordinal; positive counts from the start, negative from the end.</value>
    public int Ordinal { get; }

    /// <inheritdoc />
    public DateOnly? Calculate(int year, StrategyResolutionContext context)
    {
        if (year is < 1 or > 9999)
            return null;

        if (Ordinal > 0)
        {
            int daysInMonth = DateTime.DaysInMonth(year, Month);
            return Ordinal <= daysInMonth ? new DateOnly(year, Month, Ordinal) : null;
        }

        DateOnly candidate = DateOnlyExtensions.GetLastDateOfMonth(year, Month).AddDays(Ordinal + 1);
        return candidate.Year == year && candidate.Month == Month ? candidate : null;
    }
}
