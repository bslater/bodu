// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DailyIntervalRecurrenceStrategy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Generates an occurrence every fixed number of calendar days from an anchor date, such as every 14 days from
/// 1 January 2026.
/// </summary>
/// <remarks>
/// <para>
/// The anchor is occurrence zero; no occurrence before the anchor is produced. Alignment into the requested window is
/// computed directly from the anchor via <see cref="DateOnlyExtensions.NextOccurrence" />, so a distant anchor never
/// forces per-day iteration. A window that starts exactly on an aligned occurrence includes that occurrence.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Every 14 days from 1 January 2026 (fortnightly).
/// var recurrence = new DailyIntervalRecurrenceStrategy(new DateOnly(2026, 1, 1), 14);
///]]>
/// </code>
/// </example>
/// </remarks>
/// <seealso cref="IDateRecurrenceStrategy" />
public sealed class DailyIntervalRecurrenceStrategy
    : IDateRecurrenceStrategy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DailyIntervalRecurrenceStrategy" /> class.
    /// </summary>
    /// <param name="anchorDate">The anchor date that defines occurrence zero and the phase of the series.</param>
    /// <param name="intervalDays">The number of calendar days between consecutive occurrences.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="intervalDays" /> is less than one.</exception>
    public DailyIntervalRecurrenceStrategy(DateOnly anchorDate, int intervalDays = 1)
    {
        ThrowHelper.ThrowIfLessThan(intervalDays, 1);

        AnchorDate = anchorDate;
        IntervalDays = intervalDays;
    }

    /// <summary>
    /// Gets the anchor date that defines occurrence zero and the phase of the series.
    /// </summary>
    /// <value>The anchor date.</value>
    public DateOnly AnchorDate { get; }

    /// <summary>
    /// Gets the number of calendar days between consecutive occurrences.
    /// </summary>
    /// <value>The interval in days.</value>
    public int IntervalDays { get; }

    /// <inheritdoc />
    public IEnumerable<DateOnly> GetOccurrences(DateRange range, StrategyResolutionContext context) =>
        RecurrenceEnumeration.Stride(AnchorDate, IntervalDays, range);
}
