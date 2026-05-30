// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateSeries.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Bodu.Financial;

/// <summary>
/// Stores the ordered set of dated exchange-rate observations for a single provider and currency pair, optimised for
/// read-heavy lookup with allocation-free <c>O(log n)</c> resolution under any
/// <see cref="ExchangeRateDateResolution" /> policy.
/// </summary>
/// <remarks>
/// <para>
/// The collection is immutable after construction. Internally it stores observations as two parallel sorted arrays — a
/// <see cref="DateOnly" /> array of observation dates and a <see cref="decimal" /> array of rates — so that the binary
/// search at lookup time touches only the compact (4 bytes per element) date array. Compared with
/// <see cref="System.Collections.Generic.SortedDictionary{TKey, TValue}" /> this gives substantially better cache
/// locality, no per-node allocation, and predictable hot-path performance for the multi-year daily series typical of FX
/// data.
/// </para>
/// <para>
/// Instances are safe to share across threads after construction because all read paths only touch read-only arrays.
/// </para>
/// </remarks>
[DebuggerDisplay("{Pair.FromIsoCode,nq}/{Pair.ToIsoCode,nq} ({Provider,nq}) Count={Count}")]
public sealed class ExchangeRateSeries
{
    /// <summary>
    /// The observation dates in strictly ascending order. Index <c>i</c> corresponds to <see cref="_rates" /> at the
    /// same index.
    /// </summary>
    private readonly DateOnly[] _dates;

    /// <summary>
    /// The observed rates aligned positionally with <see cref="_dates" />. Every entry is strictly positive.
    /// </summary>
    private readonly decimal[] _rates;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeRateSeries" /> class.
    /// </summary>
    /// <param name="pair">The currency pair this series describes.</param>
    /// <param name="provider">The non-empty identifier of the publishing source.</param>
    /// <param name="rates">
    /// The observations to include. Must contain at least one entry and must not contain duplicate dates.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="provider" /> or <paramref name="rates" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="provider" /> is empty or white-space, if <paramref name="rates" /> is empty, or if it
    /// contains duplicate dates.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if any rate in <paramref name="rates" /> is zero or negative.
    /// </exception>
    public ExchangeRateSeries(
        ExchangeRatePair pair,
        string provider,
        IEnumerable<(DateOnly Date, decimal Rate)> rates)
    {
        FinancialThrowHelper.ThrowIfNullOrWhiteSpaceProvider(provider);
        ThrowHelper.ThrowIfNull(rates);

        Pair = pair;
        Provider = provider;

        List<(DateOnly Date, decimal Rate)> buffer = new(rates);

        if (buffer.Count == 0)
            throw new ArgumentException(FinancialResourceStrings.Arg_Invalid_RateSeriesEmpty, nameof(rates));

        for (var i = 0; i < buffer.Count; i++)
        {
            if (buffer[i].Rate <= 0m)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rates),
                    buffer[i].Rate,
                    FinancialResourceStrings.Arg_OutOfRange_RateNotPositive);
            }
        }

        buffer.Sort(static (a, b) => a.Date.CompareTo(b.Date));

        for (var i = 1; i < buffer.Count; i++)
        {
            if (buffer[i].Date == buffer[i - 1].Date)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        FinancialResourceStrings.Arg_Invalid_RateSeriesDuplicateDate,
                        buffer[i].Date),
                    nameof(rates));
            }
        }

        _dates = new DateOnly[buffer.Count];
        _rates = new decimal[buffer.Count];

        for (var i = 0; i < buffer.Count; i++)
        {
            _dates[i] = buffer[i].Date;
            _rates[i] = buffer[i].Rate;
        }
    }

    /// <summary>
    /// Gets the currency pair this series describes.
    /// </summary>
    /// <returns>The series pair.</returns>
    public ExchangeRatePair Pair { get; }

    /// <summary>
    /// Gets the identifier of the source that published the rates in this series.
    /// </summary>
    /// <returns>A non-empty provider identifier.</returns>
    public string Provider { get; }

    /// <summary>
    /// Gets the number of observations stored in this series.
    /// </summary>
    /// <returns>A non-negative count equal to the number of rate entries supplied at construction.</returns>
    public int Count => _dates.Length;

    /// <summary>
    /// Attempts to resolve a rate for <paramref name="requestedDate" /> under <paramref name="options" />.
    /// </summary>
    /// <param name="requestedDate">The calendar date the caller is asking about.</param>
    /// <param name="options">The lookup rules to apply.</param>
    /// <param name="resolvedDate">
    /// When this method returns <see langword="true" />, the observation date selected as the answer; otherwise
    /// <see langword="default" />.
    /// </param>
    /// <param name="rate">
    /// When this method returns <see langword="true" />, the rate observed on <paramref name="resolvedDate" />;
    /// otherwise <see langword="default" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if a rate was resolved within tolerance; otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The resolution algorithm performs a single <see cref="Array.BinarySearch{T}(T[], T)" /> over the date array. On
    /// an exact hit the corresponding rate is returned immediately. On a miss the previous and next candidate indices
    /// are derived from the bitwise complement of the returned index, then selected per <paramref name="options" />.
    /// For <see cref="ExchangeRateDateResolution.Nearest" /> with a tie (the requested date lies exactly midway between
    /// two observations), this method returns <see langword="false" /> so callers receive a deterministic failure
    /// rather than an arbitrary pick.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetRate(
        DateOnly requestedDate,
        ExchangeRateLookupOptions options,
        out DateOnly resolvedDate,
        out decimal rate)
    {
        options.Validate();

        DateOnly[] dates = _dates;
        var rates = _rates;

        var index = Array.BinarySearch(dates, requestedDate);

        if (index >= 0)
        {
            resolvedDate = dates[index];
            rate = rates[index];
            return true;
        }

        if (options.DateResolution == ExchangeRateDateResolution.Exact)
        {
            resolvedDate = default;
            rate = default;
            return false;
        }

        var next = ~index;
        var previous = next - 1;

        if (!TrySelectCandidate(dates, requestedDate, options.DateResolution, previous, next, out var candidate))
        {
            resolvedDate = default;
            rate = default;
            return false;
        }

        var offsetDays = Math.Abs(dates[candidate].DayNumber - requestedDate.DayNumber);

        if (offsetDays > options.ToleranceDays)
        {
            resolvedDate = default;
            rate = default;
            return false;
        }

        resolvedDate = dates[candidate];
        rate = rates[candidate];
        return true;
    }

    /// <summary>
    /// Selects the candidate index for the supplied <paramref name="resolution" /> given the previous and next bracket
    /// indices produced by the binary search.
    /// </summary>
    /// <param name="dates">The series date array.</param>
    /// <param name="requestedDate">The date the caller originally requested.</param>
    /// <param name="resolution">The date-resolution policy in effect.</param>
    /// <param name="previous">The index immediately before the insertion point, or <c>-1</c> if none.</param>
    /// <param name="next">The insertion point, or <c><see cref="Count" /></c> if past the end.</param>
    /// <param name="candidate">When this method returns <see langword="true" />, the chosen index.</param>
    /// <returns><see langword="true" /> if a candidate was selected; otherwise <see langword="false" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TrySelectCandidate(
        DateOnly[] dates,
        DateOnly requestedDate,
        ExchangeRateDateResolution resolution,
        int previous,
        int next,
        out int candidate)
    {
        var hasPrevious = previous >= 0;
        var hasNext = next < dates.Length;

        switch (resolution)
        {
            case ExchangeRateDateResolution.PreviousOnOrBefore:
                if (hasPrevious)
                {
                    candidate = previous;
                    return true;
                }
                break;

            case ExchangeRateDateResolution.NextOnOrAfter:
                if (hasNext)
                {
                    candidate = next;
                    return true;
                }
                break;

            case ExchangeRateDateResolution.Nearest:
            case ExchangeRateDateResolution.NearestPreferPrevious:
            case ExchangeRateDateResolution.NearestPreferNext:
                return TrySelectNearest(dates, requestedDate, resolution, hasPrevious, hasNext, previous, next, out candidate);
        }

        candidate = -1;
        return false;
    }

    /// <summary>
    /// Selects the nearest candidate index, honouring the tie-break preference encoded in
    /// <paramref name="resolution" />.
    /// </summary>
    /// <param name="dates">The series date array.</param>
    /// <param name="requestedDate">The date the caller originally requested.</param>
    /// <param name="resolution">A <c>Nearest*</c> resolution.</param>
    /// <param name="hasPrevious">Whether a previous candidate is available.</param>
    /// <param name="hasNext">Whether a next candidate is available.</param>
    /// <param name="previous">The previous candidate index.</param>
    /// <param name="next">The next candidate index.</param>
    /// <param name="candidate">When this method returns <see langword="true" />, the chosen index.</param>
    /// <returns><see langword="true" /> if a candidate was selected; otherwise <see langword="false" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TrySelectNearest(
        DateOnly[] dates,
        DateOnly requestedDate,
        ExchangeRateDateResolution resolution,
        bool hasPrevious,
        bool hasNext,
        int previous,
        int next,
        out int candidate)
    {
        if (!hasPrevious && !hasNext)
        {
            candidate = -1;
            return false;
        }

        if (!hasPrevious)
        {
            candidate = next;
            return true;
        }

        if (!hasNext)
        {
            candidate = previous;
            return true;
        }

        var requestedDay = requestedDate.DayNumber;
        var previousDistance = requestedDay - dates[previous].DayNumber;
        var nextDistance = dates[next].DayNumber - requestedDay;

        if (previousDistance < nextDistance)
        {
            candidate = previous;
            return true;
        }

        if (nextDistance < previousDistance)
        {
            candidate = next;
            return true;
        }

        switch (resolution)
        {
            case ExchangeRateDateResolution.NearestPreferPrevious:
                candidate = previous;
                return true;

            case ExchangeRateDateResolution.NearestPreferNext:
                candidate = next;
                return true;

            default:
                candidate = -1;
                return false;
        }
    }
}
