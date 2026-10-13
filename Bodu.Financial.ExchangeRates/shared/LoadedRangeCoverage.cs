// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LoadedRangeCoverage.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Tracks the days a range-based feed has loaded: the days the source can no longer revise, held for good, and the open
/// days at the end of a download, held only until the provider's refresh interval has passed.
/// </summary>
/// <remarks>
/// <para>
/// A download of a range that reaches today records its final days (see <see cref="RatePublication" />) permanently and
/// the rest of the range provisionally, so that a day the source had not yet published when it was downloaded is
/// downloaded again once its provisional record expires, rather than never.
/// </para>
/// <para>
/// Instances are not thread-safe; callers synchronize access themselves.
/// </para>
/// </remarks>
internal sealed class LoadedRangeCoverage
{
    /// <summary>The days the source can no longer revise.</summary>
    private readonly DateRangeCoverage _final = new();

    /// <summary>The open days of each download, with the instant each expires; possibly overlapping.</summary>
    private readonly List<(DateOnly Start, DateOnly End, DateTimeOffset ExpiresAt)> _provisional = new();

    /// <summary>
    /// Reports whether every day of an inclusive range is held, either for good or provisionally and not yet expired.
    /// </summary>
    /// <param name="start">The inclusive first day of the range.</param>
    /// <param name="end">The inclusive last day of the range.</param>
    /// <param name="nowUtc">The current instant, against which provisional records expire.</param>
    /// <returns>
    /// <see langword="true" /> when every day of the range is held; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="start" /> is later than <paramref name="end" />.
    /// </exception>
    public bool Contains(DateOnly start, DateOnly end, DateTimeOffset nowUtc)
    {
        if (_final.Contains(start, end))
            return true;

        _provisional.RemoveAll(record => record.ExpiresAt <= nowUtc);
        if (_provisional.Count == 0)
            return false;

        // Walk the range from its start, jumping each time to the end of the furthest record that holds the current
        // day; a day no record holds is a gap.
        DateOnly cursor = start;
        while (true)
        {
            DateOnly? reach = Reach(cursor);
            if (reach is not { } held)
                return false;

            if (held >= end)
                return true;

            cursor = held.AddDays(1);
        }
    }

    /// <summary>
    /// Finds the span of an inclusive range not held: from its first missing day to its last, counting provisionally
    /// held days as held until they expire.
    /// </summary>
    /// <param name="start">The inclusive first day of the range.</param>
    /// <param name="end">The inclusive last day of the range.</param>
    /// <param name="nowUtc">The current instant, against which provisional records expire.</param>
    /// <param name="missingStart">
    /// When this method returns <see langword="true" />, the first day not held; otherwise <see langword="default" />.
    /// </param>
    /// <param name="missingEnd">
    /// When this method returns <see langword="true" />, the last day not held; otherwise <see langword="default" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when some day of the range is not held; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="start" /> is later than <paramref name="end" />.
    /// </exception>
    public bool TryGetUncoveredSpan(DateOnly start, DateOnly end, DateTimeOffset nowUtc, out DateOnly missingStart, out DateOnly missingEnd)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Records a download of an inclusive range: its final days for good, and the rest until the refresh interval has
    /// passed.
    /// </summary>
    /// <param name="start">The inclusive first day of the downloaded range.</param>
    /// <param name="end">The inclusive last day of the downloaded range.</param>
    /// <param name="lastReturnedDate">
    /// The latest date the download returned a rate for, or <see langword="null" /> when it returned none.
    /// </param>
    /// <param name="fetchedAtUtc">The instant the download was obtained from the source.</param>
    /// <param name="refreshInterval">How long the open days of the download may be served.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="start" /> is later than <paramref name="end" />.
    /// </exception>
    public void Add(DateOnly start, DateOnly end, DateOnly? lastReturnedDate, DateTimeOffset fetchedAtUtc, TimeSpan refreshInterval)
    {
        ThrowHelper.ThrowIfGreaterThan(start, end);

        DateOnly finalThrough = RatePublication.FinalThrough(lastReturnedDate, fetchedAtUtc);
        if (start <= finalThrough)
            _final.Add(start, end < finalThrough ? end : finalThrough);

        if (end > finalThrough)
        {
            DateOnly openStart = start > finalThrough ? start : finalThrough.AddDays(1);
            _provisional.Add((openStart, end, RatePublication.ExpiresAt(fetchedAtUtc, refreshInterval)));
        }
    }

    /// <summary>
    /// Returns the last day of the furthest-reaching record, final or provisional, that holds a day.
    /// </summary>
    /// <param name="day">The day to look up.</param>
    /// <returns>
    /// The last day held by a record that holds <paramref name="day" />, or <see langword="null" /> when none does.
    /// </returns>
    private DateOnly? Reach(DateOnly day)
    {
        DateOnly? reach = null;

        foreach ((DateOnly Start, DateOnly End) interval in _final.Intervals)
        {
            if (interval.Start <= day && interval.End >= day)
            {
                reach = interval.End;
                break;
            }
        }

        foreach ((DateOnly Start, DateOnly End, DateTimeOffset _) record in _provisional)
        {
            if (record.Start <= day && record.End >= day && (reach is null || record.End > reach.Value))
                reach = record.End;
        }

        return reach;
    }
}
