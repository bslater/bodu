// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceEnumeration.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Provides shared, allocation-frugal enumeration primitives for recurrence strategies, aligning directly into a
/// bounded window rather than iterating from an anchor in the distant past.
/// </summary>
/// <remarks>
/// The stride helper delegates its fixed-interval phase arithmetic to <see cref="DateOnlyExtensions.NextOccurrence" />,
/// and the month helper yields participating months without ever iterating individual days, so callers stay within the
/// performance envelope required of recurrence generation.
/// </remarks>
internal static class RecurrenceEnumeration
{
    /// <summary>
    /// Enumerates the occurrences of a fixed day-interval series that fall within a bounded window, in ascending order.
    /// </summary>
    /// <param name="seriesAnchor">The phase origin of the series; no occurrence before this date is produced.</param>
    /// <param name="strideDays">The stride between consecutive occurrences, in days.</param>
    /// <param name="range">The inclusive window to generate occurrences within.</param>
    /// <returns>The aligned occurrences within <paramref name="range" /> in ascending order.</returns>
    public static IEnumerable<DateOnly> Stride(DateOnly seriesAnchor, int strideDays, DateRange range)
    {
        if (range.EndDate < seriesAnchor || range.StartDate > range.EndDate)
            yield break;

        // Align to the first occurrence on or after the window start without iterating from the anchor. NextOccurrence
        // is strictly-after its bound, so pass StartDate - 1 to obtain on-or-after semantics; the branch guards the
        // DateOnly.MinValue edge (which can only arise when the anchor is at or after the window start anyway).
        DateOnly current = range.StartDate <= seriesAnchor
            ? seriesAnchor
            : seriesAnchor.NextOccurrence(strideDays, range.StartDate.AddDays(-1));

        while (current <= range.EndDate)
        {
            yield return current;

            if (current.DayNumber > DateOnly.MaxValue.DayNumber - strideDays)
                yield break;

            current = current.AddDays(strideDays);
        }
    }

    /// <summary>
    /// Enumerates the participating months of a month-interval series that overlap a bounded window, in ascending
    /// order.
    /// </summary>
    /// <param name="anchor">
    /// The anchor whose year and month define month zero when <paramref name="intervalMonths" /> is greater than one.
    /// </param>
    /// <param name="intervalMonths">The stride between consecutive participating months.</param>
    /// <param name="range">The inclusive window whose overlapping months are considered.</param>
    /// <returns>
    /// The participating <c>(year, month)</c> pairs in ascending order, restricted to years 1 through 9999.
    /// </returns>
    public static IEnumerable<(int Year, int Month)> Months(DateOnly? anchor, int intervalMonths, DateRange range)
    {
        if (range.StartDate > range.EndDate)
            yield break;

        int startIndex = MonthIndex(range.StartDate);
        int endIndex = MonthIndex(range.EndDate);
        int anchorIndex = anchor.HasValue ? MonthIndex(anchor.Value) : startIndex;

        // For a multi-month interval the anchor's month is month zero: no occurrence before it, and only months on the
        // interval grid participate. For a monthly interval every month participates from the window start.
        int firstIndex = intervalMonths > 1 ? Math.Max(startIndex, anchorIndex) : startIndex;

        for (int index = firstIndex; index <= endIndex; index++)
        {
            if (intervalMonths > 1 && (index - anchorIndex) % intervalMonths != 0)
                continue;

            int year = index / 12;
            int month = (index % 12) + 1;
            if (year is < 1 or > 9999)
                continue;

            yield return (year, month);
        }
    }

    /// <summary>
    /// Computes the zero-based absolute month index (<c>year * 12 + month - 1</c>) for a date.
    /// </summary>
    /// <param name="date">The date whose month index to compute.</param>
    /// <returns>The absolute month index.</returns>
    private static int MonthIndex(DateOnly date) =>
        (date.Year * 12) + (date.Month - 1);
}
