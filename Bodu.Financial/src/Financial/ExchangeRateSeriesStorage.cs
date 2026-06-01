// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateSeriesStorage.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Runtime.CompilerServices;

namespace Bodu.Financial;

/// <summary>
/// Stores the validated, sorted, deduplicated day-number / rate arrays that back an immutable
/// <see cref="ExchangeRateSeries" /> snapshot and provides the lookup, enumeration, and construction logic
/// shared with the mutable buffer.
/// </summary>
/// <remarks>
/// <para>
/// The storage owns its arrays and treats them as immutable. Day numbers are stored as <see cref="int" />
/// (<see cref="DateOnly.DayNumber" />) to keep comparisons branch-light and to allow direct
/// <see cref="Array.BinarySearch{T}(T[], int, int, T)" /> dispatch without an <see cref="IComparable{T}" />
/// virtual call. Public boundaries continue to use <see cref="DateOnly" />.
/// </para>
/// <para>
/// Instances are safe to share across threads after construction because all read paths only touch read-only
/// arrays.
/// </para>
/// </remarks>
internal sealed class ExchangeRateSeriesStorage
{
    /// <summary>
    /// The observation day numbers in strictly ascending order. Index <c>i</c> corresponds to <see cref="_rates" />
    /// at the same index.
    /// </summary>
    private readonly int[] _dayNumbers;

    /// <summary>
    /// The observed rates aligned positionally with <see cref="_dayNumbers" />. Every entry is strictly positive.
    /// </summary>
    private readonly decimal[] _rates;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeRateSeriesStorage" /> class with the supplied
    /// pre-validated arrays. The instance assumes ownership; callers must not mutate the arrays after the call.
    /// </summary>
    /// <param name="dayNumbers">The strictly ascending day-number array.</param>
    /// <param name="rates">The aligned strictly-positive rate array.</param>
    private ExchangeRateSeriesStorage(int[] dayNumbers, decimal[] rates)
    {
        _dayNumbers = dayNumbers;
        _rates = rates;
    }

    /// <summary>
    /// Gets the number of observations stored.
    /// </summary>
    /// <returns>A positive count.</returns>
    public int Count => _dayNumbers.Length;

    /// <summary>
    /// Gets the calendar date of the first (earliest) observation.
    /// </summary>
    /// <returns>The earliest observation date.</returns>
    public DateOnly FirstDate => DateOnly.FromDayNumber(_dayNumbers[0]);

    /// <summary>
    /// Gets the calendar date of the last (latest) observation.
    /// </summary>
    /// <returns>The latest observation date.</returns>
    public DateOnly LastDate => DateOnly.FromDayNumber(_dayNumbers[_dayNumbers.Length - 1]);

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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetRate(
        DateOnly requestedDate,
        ExchangeRateLookupOptions options,
        out DateOnly resolvedDate,
        out decimal rate)
    {
        var dayNumbers = _dayNumbers;
        var rates = _rates;
        var requestedDayNumber = requestedDate.DayNumber;

        var index = Array.BinarySearch(dayNumbers, requestedDayNumber);

        if (index >= 0)
        {
            resolvedDate = DateOnly.FromDayNumber(dayNumbers[index]);
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

        if (!ExchangeRateDateSearch.TrySelectCandidate(dayNumbers, requestedDayNumber, options.DateResolution, previous, next, out var candidate))
        {
            resolvedDate = default;
            rate = default;
            return false;
        }

        var offsetDays = Math.Abs(dayNumbers[candidate] - requestedDayNumber);

        if (offsetDays > options.ToleranceDays)
        {
            resolvedDate = default;
            rate = default;
            return false;
        }

        resolvedDate = DateOnly.FromDayNumber(dayNumbers[candidate]);
        rate = rates[candidate];
        return true;
    }

    /// <summary>
    /// Enumerates the observations in strictly ascending date order.
    /// </summary>
    /// <returns>A lazy sequence of <see cref="ExchangeRateObservation" /> values.</returns>
    public IEnumerable<ExchangeRateObservation> Enumerate()
    {
        for (var i = 0; i < _dayNumbers.Length; i++)
        {
            yield return new ExchangeRateObservation(DateOnly.FromDayNumber(_dayNumbers[i]), _rates[i]);
        }
    }

    /// <summary>
    /// Validates, sorts, and deduplicates the supplied tuple sequence and returns a new storage instance.
    /// </summary>
    /// <param name="rates">The candidate observations.</param>
    /// <param name="ratesParamName">
    /// The parameter name used in raised exceptions so the caller's signature is reflected in
    /// <see cref="ArgumentException.ParamName" />.
    /// </param>
    /// <returns>A new <see cref="ExchangeRateSeriesStorage" /> wrapping the validated arrays.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="rates" /> is empty or contains duplicate dates.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if any rate is zero or negative.
    /// </exception>
    public static ExchangeRateSeriesStorage Create(
        IEnumerable<(DateOnly Date, decimal Rate)> rates,
        string ratesParamName)
    {
        List<(DateOnly Date, decimal Rate)> buffer = new(rates);

        if (buffer.Count == 0)
            throw new ArgumentException(FinancialResourceStrings.Arg_Invalid_RateSeriesEmpty, ratesParamName);

        for (var i = 0; i < buffer.Count; i++)
        {
            if (buffer[i].Rate <= 0m)
            {
                throw new ArgumentOutOfRangeException(
                    ratesParamName,
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
                    ratesParamName);
            }
        }

        var dayNumbers = new int[buffer.Count];
        var rateArray = new decimal[buffer.Count];

        for (var i = 0; i < buffer.Count; i++)
        {
            dayNumbers[i] = buffer[i].Date.DayNumber;
            rateArray[i] = buffer[i].Rate;
        }

        return new ExchangeRateSeriesStorage(dayNumbers, rateArray);
    }

    /// <summary>
    /// Validates, sorts, and deduplicates the supplied observation sequence and returns a new storage instance.
    /// </summary>
    /// <param name="observations">The candidate observations.</param>
    /// <param name="observationsParamName">
    /// The parameter name used in raised exceptions so the caller's signature is reflected in
    /// <see cref="ArgumentException.ParamName" />.
    /// </param>
    /// <returns>A new <see cref="ExchangeRateSeriesStorage" /> wrapping the validated arrays.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="observations" /> is empty or contains duplicate dates.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if any rate is zero or negative.
    /// </exception>
    public static ExchangeRateSeriesStorage Create(
        IEnumerable<ExchangeRateObservation> observations,
        string observationsParamName)
    {
        List<ExchangeRateObservation> buffer = new(observations);

        if (buffer.Count == 0)
            throw new ArgumentException(FinancialResourceStrings.Arg_Invalid_RateSeriesEmpty, observationsParamName);

        for (var i = 0; i < buffer.Count; i++)
        {
            if (buffer[i].Rate <= 0m)
            {
                throw new ArgumentOutOfRangeException(
                    observationsParamName,
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
                    observationsParamName);
            }
        }

        var dayNumbers = new int[buffer.Count];
        var rateArray = new decimal[buffer.Count];

        for (var i = 0; i < buffer.Count; i++)
        {
            dayNumbers[i] = buffer[i].Date.DayNumber;
            rateArray[i] = buffer[i].Rate;
        }

        return new ExchangeRateSeriesStorage(dayNumbers, rateArray);
    }

    /// <summary>
    /// Creates a storage instance directly from arrays that the caller guarantees are strictly ascending, unique,
    /// and strictly positive. Used by the mutable buffer's snapshot path to avoid revalidation.
    /// </summary>
    /// <param name="dayNumbers">
    /// The strictly ascending unique day numbers. The instance takes ownership; the caller must not mutate the
    /// array after the call.
    /// </param>
    /// <param name="rates">
    /// The aligned strictly-positive rates. The instance takes ownership; the caller must not mutate the array
    /// after the call.
    /// </param>
    /// <returns>A new <see cref="ExchangeRateSeriesStorage" /> wrapping the supplied arrays.</returns>
    internal static ExchangeRateSeriesStorage CreateFromSortedUnique(int[] dayNumbers, decimal[] rates) =>
        new(dayNumbers, rates);
}
