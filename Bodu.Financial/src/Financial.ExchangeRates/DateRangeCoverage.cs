// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateRangeCoverage.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Tracks the set of <see cref="DateOnly" /> ranges that have actually been observed, as a sorted list of disjoint
/// inclusive intervals that merge on insertion but never bridge a gap.
/// </summary>
/// <remarks>
/// <para>
/// The type answers one question precisely: has every day in a requested window already been seen? Unlike a single
/// <c>(min, max)</c> envelope, it preserves interior gaps, so a window that straddles an unobserved stretch is
/// correctly reported as not covered. Two intervals are merged only when they overlap or touch - the later interval
/// begins on or before the day after the earlier one ends - so a genuine gap of one or more days keeps them separate.
/// </para>
/// <para>
/// When a window is not covered, <see cref="TryGetUncoveredSpan(DateOnly, DateOnly, out DateOnly, out DateOnly)" />
/// reports the span from its first unrecorded day to its last, so a caller can fetch only that span rather than the
/// whole window again.
/// </para>
/// <para>
/// Instances are not thread-safe; callers that mutate a shared instance must synchronize access themselves.
/// </para>
/// </remarks>
public sealed class DateRangeCoverage
{
    /// <summary>The covered intervals, kept sorted by start date and mutually disjoint, with no two intervals overlapping or touching.</summary>
    private readonly List<(DateOnly Start, DateOnly End)> _intervals = new();

    /// <summary>
    /// Gets a value indicating whether no range has been recorded.
    /// </summary>
    /// <value><see langword="true" /> when no interval has been added; otherwise <see langword="false" />.</value>
    public bool IsEmpty => _intervals.Count == 0;

    /// <summary>
    /// Gets the recorded intervals in ascending order.
    /// </summary>
    /// <value>The disjoint inclusive intervals that make up the coverage, ordered by start date.</value>
    public IReadOnlyList<(DateOnly Start, DateOnly End)> Intervals => _intervals.AsReadOnly();

    /// <summary>
    /// Records the inclusive range <paramref name="start" />..<paramref name="end" /> as observed, merging it with any
    /// overlapping or adjacent intervals already present.
    /// </summary>
    /// <param name="start">The inclusive first date of the observed range.</param>
    /// <param name="end">The inclusive last date of the observed range.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="start" /> is later than <paramref name="end" />.
    /// </exception>
    public void Add(DateOnly start, DateOnly end)
    {
        ThrowHelper.ThrowIfGreaterThan(start, end);

        // Skip every interval that ends before the day preceding start: those are too early to overlap or touch.
        int index = 0;
        while (index < _intervals.Count && _intervals[index].End < SubtractOneDay(start))
            index++;

        // Absorb each interval that overlaps or touches [start, end], widening the bounds to their union and removing it.
        while (index < _intervals.Count && _intervals[index].Start <= AddOneDay(end))
        {
            start = Min(start, _intervals[index].Start);
            end = Max(end, _intervals[index].End);
            _intervals.RemoveAt(index);
        }

        _intervals.Insert(index, (start, end));
    }

    /// <summary>
    /// Reports whether every day in the inclusive range <paramref name="start" />..<paramref name="end" /> has been
    /// recorded.
    /// </summary>
    /// <param name="start">The inclusive first date of the window to test.</param>
    /// <param name="end">The inclusive last date of the window to test.</param>
    /// <returns>
    /// <see langword="true" /> when a single recorded interval fully contains the window; otherwise
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="start" /> is later than <paramref name="end" />.
    /// </exception>
    public bool Contains(DateOnly start, DateOnly end)
    {
        ThrowHelper.ThrowIfGreaterThan(start, end);

        foreach ((DateOnly Start, DateOnly End) interval in _intervals)
        {
            if (interval.Start <= start && interval.End >= end)
                return true;

            // Intervals are ordered by start date; once one begins after the window start, none can contain it.
            if (interval.Start > start)
                break;
        }

        return false;
    }

    /// <summary>
    /// Finds the span of a window that is not yet recorded: from its first unrecorded day to its last.
    /// </summary>
    /// <param name="start">The inclusive first date of the window to test.</param>
    /// <param name="end">The inclusive last date of the window to test.</param>
    /// <param name="uncoveredStart">
    /// When this method returns <see langword="true" />, the first day of the window that has not been recorded;
    /// otherwise <see langword="default" />.
    /// </param>
    /// <param name="uncoveredEnd">
    /// When this method returns <see langword="true" />, the last day of the window that has not been recorded;
    /// otherwise <see langword="default" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when some day of the window has not been recorded; <see langword="false" /> when every
    /// day has (the window is <see cref="Contains(DateOnly, DateOnly)" />).
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="start" /> is later than <paramref name="end" />.
    /// </exception>
    /// <remarks>
    /// A caller that fetches only the reported span, rather than the whole window, fetches every missing day at the
    /// cost of at most the recorded days between the first and last missing ones, in a single request.
    /// </remarks>
    public bool TryGetUncoveredSpan(DateOnly start, DateOnly end, out DateOnly uncoveredStart, out DateOnly uncoveredEnd)
    {
        ThrowHelper.ThrowIfGreaterThan(start, end);

        uncoveredStart = default;
        uncoveredEnd = default;

        // Intervals are disjoint and never adjacent, so at most one holds the window's first day and at most one its
        // last; stepping past each gives the first and last unrecorded days.
        DateOnly first = start;
        foreach ((DateOnly Start, DateOnly End) interval in _intervals)
        {
            if (interval.Start <= first && interval.End >= first)
            {
                if (interval.End >= end)
                    return false;

                first = interval.End.AddDays(1);
                break;
            }
        }

        DateOnly last = end;
        for (int i = _intervals.Count - 1; i >= 0; i--)
        {
            (DateOnly Start, DateOnly End) interval = _intervals[i];
            if (interval.Start <= last && interval.End >= last)
            {
                last = interval.Start.AddDays(-1);
                break;
            }
        }

        uncoveredStart = first;
        uncoveredEnd = last;
        return true;
    }

    /// <summary>
    /// Reports whether the supplied date has been recorded.
    /// </summary>
    /// <param name="date">The date to test.</param>
    /// <returns>
    /// <see langword="true" /> when a recorded interval contains <paramref name="date" />; otherwise
    /// <see langword="false" />.
    /// </returns>
    public bool Contains(DateOnly date) => Contains(date, date);

    /// <summary>
    /// Returns the earlier of two dates.
    /// </summary>
    /// <param name="left">The first date.</param>
    /// <param name="right">The second date.</param>
    /// <returns>The earlier of <paramref name="left" /> and <paramref name="right" />.</returns>
    private static DateOnly Min(DateOnly left, DateOnly right) => left < right ? left : right;

    /// <summary>
    /// Returns the later of two dates.
    /// </summary>
    /// <param name="left">The first date.</param>
    /// <param name="right">The second date.</param>
    /// <returns>The later of <paramref name="left" /> and <paramref name="right" />.</returns>
    private static DateOnly Max(DateOnly left, DateOnly right) => left > right ? left : right;

    /// <summary>
    /// Returns the day after <paramref name="date" />, saturating at <see cref="DateOnly.MaxValue" /> so adjacency
    /// tests never overflow.
    /// </summary>
    /// <param name="date">The date to advance by one day.</param>
    /// <returns>
    /// The following day, or <see cref="DateOnly.MaxValue" /> when <paramref name="date" /> is already the maximum.
    /// </returns>
    private static DateOnly AddOneDay(DateOnly date) => date == DateOnly.MaxValue ? date : date.AddDays(1);

    /// <summary>
    /// Returns the day before <paramref name="date" />, saturating at <see cref="DateOnly.MinValue" /> so adjacency
    /// tests never overflow.
    /// </summary>
    /// <param name="date">The date to step back by one day.</param>
    /// <returns>
    /// The preceding day, or <see cref="DateOnly.MinValue" /> when <paramref name="date" /> is already the minimum.
    /// </returns>
    private static DateOnly SubtractOneDay(DateOnly date) => date == DateOnly.MinValue ? date : date.AddDays(-1);
}
