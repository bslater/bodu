// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateSeriesStorage.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Bodu.Financial;

/// <summary>
/// Stores the validated, sorted, deduplicated day-number / rate arrays that back an immutable
/// <see cref="ExchangeRateSeries" /> snapshot and provides the lookup, enumeration, and construction logic shared with
/// the mutable buffer.
/// </summary>
/// <remarks>
/// <para>
/// The storage owns its arrays and treats them as immutable. Day numbers are stored as <see cref="int" /> (
/// <see cref="DateOnly.DayNumber" />) to keep comparisons branch-light and to allow direct
/// <see cref="Array.BinarySearch{T}(T[], int, int, T)" /> dispatch without an <see cref="IComparable{T}" /> virtual
/// call. Public boundaries continue to use <see cref="DateOnly" />.
/// </para>
/// <para>
/// Instances are safe to share across threads after construction because all read paths only touch read-only arrays.
/// </para>
/// </remarks>
internal sealed class ExchangeRateSeriesStorage
{
    /// <summary>
    /// The observation day numbers in strictly ascending order. Index <c>i</c> corresponds to <see cref="_rates" /> at
    /// the same index.
    /// </summary>
    private readonly int[] _dayNumbers;

    /// <summary>
    /// The observed rates aligned positionally with <see cref="_dayNumbers" />. Every entry is strictly positive.
    /// </summary>
    private readonly decimal[] _rates;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeRateSeriesStorage" /> class with the supplied pre-validated
    /// arrays. The instance assumes ownership; callers must not mutate the arrays after the call.
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
    /// Reports whether the storage contains an observation on the supplied date.
    /// </summary>
    /// <param name="date">The observation date to query.</param>
    /// <returns><see langword="true" /> if an observation exists for <paramref name="date" />.</returns>
    public bool Contains(DateOnly date) => Array.BinarySearch(_dayNumbers, date.DayNumber) >= 0;

    /// <summary>
    /// Attempts to retrieve the rate stored exactly on <paramref name="date" />.
    /// </summary>
    /// <param name="date">The observation date to query.</param>
    /// <param name="rate">
    /// When this method returns <see langword="true" />, the rate observed on <paramref name="date" />; otherwise
    /// <see langword="default" />.
    /// </param>
    /// <returns><see langword="true" /> if an observation exists for <paramref name="date" />.</returns>
    public bool TryGetExactRate(DateOnly date, out decimal rate)
    {
        var index = Array.BinarySearch(_dayNumbers, date.DayNumber);
        if (index < 0)
        {
            rate = default;
            return false;
        }

        rate = _rates[index];
        return true;
    }

    /// <summary>
    /// Copies the storage's contents into the supplied caller-owned arrays. Used by the mutable buffer's seeding path
    /// so the round-trip through <see cref="DateOnly" /> can be avoided.
    /// </summary>
    /// <param name="dayNumbers">The caller-owned target array; must be at least <see cref="Count" /> long.</param>
    /// <param name="rates">The caller-owned target array; must be at least <see cref="Count" /> long.</param>
    internal void CopyTo(int[] dayNumbers, decimal[] rates)
    {
        Debug.Assert(dayNumbers is not null && rates is not null);
        Debug.Assert(dayNumbers!.Length >= _dayNumbers.Length);
        Debug.Assert(rates!.Length >= _dayNumbers.Length);

        Array.Copy(_dayNumbers, dayNumbers, _dayNumbers.Length);
        Array.Copy(_rates, rates, _rates.Length);
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
    /// <exception cref="ArgumentOutOfRangeException">Thrown if any rate is zero or negative.</exception>
    public static ExchangeRateSeriesStorage Create(
        IEnumerable<(DateOnly Date, decimal Rate)> rates,
        string ratesParamName)
    {
        List<(int DayNumber, decimal Rate)> normalised =
            ExchangeRateObservationNormalizer.ToSortedUniqueList(rates, ratesParamName, allowEmpty: false);

        return MaterialiseFromNormalised(normalised);
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
    /// <exception cref="ArgumentOutOfRangeException">Thrown if any rate is zero or negative.</exception>
    public static ExchangeRateSeriesStorage Create(
        IEnumerable<ExchangeRateObservation> observations,
        string observationsParamName)
    {
        List<(int DayNumber, decimal Rate)> normalised =
            ExchangeRateObservationNormalizer.ToSortedUniqueList(observations, observationsParamName, allowEmpty: false);

        return MaterialiseFromNormalised(normalised);
    }

    /// <summary>
    /// Adopts ownership of pre-validated strictly-ascending day-number and rate arrays. Asserts the invariants in debug
    /// builds and skips revalidation in release builds.
    /// </summary>
    /// <param name="dayNumbers">The strictly ascending unique day numbers. The instance takes ownership.</param>
    /// <param name="rates">The aligned strictly-positive rates. The instance takes ownership.</param>
    /// <returns>A new <see cref="ExchangeRateSeriesStorage" /> wrapping the supplied arrays.</returns>
    internal static ExchangeRateSeriesStorage AdoptSortedUniqueArrays(int[] dayNumbers, decimal[] rates)
    {
        Debug.Assert(dayNumbers is not null);
        Debug.Assert(rates is not null);
        Debug.Assert(dayNumbers!.Length == rates!.Length);
        Debug.Assert(dayNumbers.Length > 0);
        Debug.Assert(IsStrictlyAscending(dayNumbers));
        Debug.Assert(AllPositive(rates));

        return new ExchangeRateSeriesStorage(dayNumbers, rates);
    }

    /// <summary>
    /// Compatibility shim retained for any internal callers built before the rename.
    /// </summary>
    /// <param name="dayNumbers">The strictly ascending unique day numbers.</param>
    /// <param name="rates">The aligned strictly-positive rates.</param>
    /// <returns>A new <see cref="ExchangeRateSeriesStorage" /> wrapping the supplied arrays.</returns>
    internal static ExchangeRateSeriesStorage CreateFromSortedUnique(int[] dayNumbers, decimal[] rates) =>
        AdoptSortedUniqueArrays(dayNumbers, rates);

    /// <summary>
    /// Reports whether <paramref name="dayNumbers" /> is strictly ascending. Used only by debug assertions.
    /// </summary>
    /// <param name="dayNumbers">The day numbers to check.</param>
    /// <returns>
    /// <see langword="true" /> if every adjacent pair satisfies <c>a &lt; b</c>; otherwise <see langword="false" />.
    /// </returns>
    private static bool IsStrictlyAscending(int[] dayNumbers)
    {
        for (var i = 1; i < dayNumbers.Length; i++)
        {
            if (dayNumbers[i] <= dayNumbers[i - 1])
                return false;
        }

        return true;
    }

    /// <summary>
    /// Reports whether every rate in <paramref name="rates" /> is strictly positive. Used only by debug assertions.
    /// </summary>
    /// <param name="rates">The rates to check.</param>
    /// <returns><see langword="true" /> if every rate is &gt; 0; otherwise <see langword="false" />.</returns>
    private static bool AllPositive(decimal[] rates)
    {
        for (var i = 0; i < rates.Length; i++)
        {
            if (rates[i] <= 0m)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Materialises a normalised list of <c>(dayNumber, rate)</c> pairs into the exact-sized owned arrays.
    /// </summary>
    /// <param name="normalised">The pre-sorted, pre-deduplicated, validated buffer to copy from.</param>
    /// <returns>A new <see cref="ExchangeRateSeriesStorage" /> wrapping the materialised arrays.</returns>
    private static ExchangeRateSeriesStorage MaterialiseFromNormalised(List<(int DayNumber, decimal Rate)> normalised)
    {
        var dayNumbers = new int[normalised.Count];
        var rateArray = new decimal[normalised.Count];

        for (var i = 0; i < normalised.Count; i++)
        {
            dayNumbers[i] = normalised[i].DayNumber;
            rateArray[i] = normalised[i].Rate;
        }

        return AdoptSortedUniqueArrays(dayNumbers, rateArray);
    }
}
