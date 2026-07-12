// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WorkingDayInMonthStrategy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Calculates the Nth working day from the beginning or end of a Gregorian month, such as the first working day of
/// January or the last working day of a reporting month.
/// </summary>
/// <remarks>
/// <para>
/// A positive ordinal counts working days from the start of the month (<c>1</c> is the first working day); a negative
/// ordinal counts from the end (<c>-1</c> is the last working day). Working days exclude rest days and applicable
/// non-working notable-date occurrences, computed deterministically and independently of resource-declaration order by
/// the <see cref="StrategyResolutionContext" />. When the requested ordinal does not exist within the month, no
/// occurrence is produced rather than spilling into an adjacent month.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // The first working day of January.
/// var strategy = new WorkingDayInMonthStrategy(1, 1);
///]]>
/// </code>
/// </example>
/// </remarks>
/// <seealso cref="IDateCalculationStrategy" /> <seealso href="../guides/calendar/rule-authoring.html">Authoring notable
/// date rules (guide)</seealso>
public sealed class WorkingDayInMonthStrategy
    : IDateCalculationStrategy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkingDayInMonthStrategy" /> class.
    /// </summary>
    /// <param name="month">The one-based month.</param>
    /// <param name="ordinal">The signed ordinal position of the working day within the month.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="month" /> is not between 1 and 12, or <paramref name="ordinal" /> is zero.
    /// </exception>
    public WorkingDayInMonthStrategy(int month, int ordinal)
    {
        ThrowHelper.ThrowIfOutOfRange(month, 1, 12);
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
    /// Gets the signed ordinal position of the working day within the month.
    /// </summary>
    /// <value>The ordinal; positive counts from the start, negative from the end.</value>
    public int Ordinal { get; }

    /// <inheritdoc />
    public DateOnly? Calculate(int year, StrategyResolutionContext context)
    {
        ThrowHelper.ThrowIfNull(context);

        if (year is < 1 or > 9999)
            return null;

        DateOnly first = new(year, Month, 1);
        DateOnly last = DateOnlyExtensions.GetLastDateOfMonth(year, Month);

        int needed = Math.Abs(Ordinal);
        int count = 0;

        if (Ordinal > 0)
        {
            for (DateOnly cursor = first; cursor <= last; cursor = cursor.AddDays(1))
            {
                if (context.IsWorkingDay(cursor, context.Territory) && ++count == needed)
                    return cursor;
            }

            return null;
        }

        for (DateOnly cursor = last; cursor >= first; cursor = cursor.AddDays(-1))
        {
            if (context.IsWorkingDay(cursor, context.Territory) && ++count == needed)
                return cursor;
        }

        return null;
    }
}
