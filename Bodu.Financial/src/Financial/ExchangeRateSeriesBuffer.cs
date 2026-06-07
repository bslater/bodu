// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateSeriesBuffer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial;

/// <summary>
/// Holds the mutable sorted day-number / rate arrays backing an <see cref="ExchangeRateSeriesBuilder" /> and implements
/// the validation, search, insert, remove, and bulk-merge primitives shared with the immutable storage.
/// </summary>
/// <remarks>
/// <para>
/// The buffer maintains the same invariants as <see cref="ExchangeRateSeriesStorage" /> — strictly ascending unique day
/// numbers and strictly positive rates — but grows its arrays with spare capacity using the <see cref="List{T}" />
/// doubling rule. Single-element mutations perform a <see cref="Array.BinarySearch{T}(T[], int, int, T)" /> followed by
/// an <see cref="Array.Copy(Array, int, Array, int, int)" /> shift. Range mutations sort and deduplicate the incoming
/// batch, then run a two-pointer merge against the existing buffer into fresh arrays so a validation failure leaves the
/// buffer unchanged.
/// </para>
/// <para>
/// Instances are not thread-safe.
/// </para>
/// </remarks>
internal sealed class ExchangeRateSeriesBuffer
{
    /// <summary>
    /// The default growable capacity used when the buffer first needs to grow.
    /// </summary>
    private const int DefaultCapacity = 16;

    /// <summary>
    /// The backing day-number array. Slots <c>[0, _count)</c> are live and strictly ascending; trailing slots are
    /// uninitialised capacity.
    /// </summary>
    private int[] _dayNumbers;

    /// <summary>
    /// The backing rate array. Slots <c>[0, _count)</c> are live and aligned with <see cref="_dayNumbers" />.
    /// </summary>
    private decimal[] _rates;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeRateSeriesBuffer" /> class with zero capacity.
    /// </summary>
    public ExchangeRateSeriesBuffer()
    {
        _dayNumbers = [];
        _rates = [];
        Count = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeRateSeriesBuffer" /> class with at least
    /// <paramref name="capacity" /> slots pre-allocated.
    /// </summary>
    /// <param name="capacity">The initial capacity. Negative values are clamped to zero.</param>
    public ExchangeRateSeriesBuffer(int capacity)
    {
        if (capacity <= 0)
        {
            _dayNumbers = [];
            _rates = [];
        }
        else
        {
            _dayNumbers = new int[capacity];
            _rates = new decimal[capacity];
        }

        Count = 0;
    }

    /// <summary>
    /// Gets the number of live observations.
    /// </summary>
    /// <returns>A non-negative count.</returns>
    public int Count { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the buffer holds no observations.
    /// </summary>
    /// <returns><see langword="true" /> if the buffer is empty; otherwise <see langword="false" />.</returns>
    public bool IsEmpty => Count == 0;

    /// <summary>
    /// Inserts a new observation, throwing if an observation already exists for <paramref name="dayNumber" />.
    /// </summary>
    /// <param name="dayNumber">The observation day number.</param>
    /// <param name="rate">The observed rate.</param>
    /// <param name="rateParamName">
    /// The caller's parameter name to report in any raised <see cref="ArgumentOutOfRangeException" />.
    /// </param>
    /// <param name="dateParamName">
    /// The caller's parameter name to report in any raised <see cref="ArgumentException" /> for a duplicate date.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="rate" /> is zero or negative.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if an observation already exists for <paramref name="dayNumber" />.
    /// </exception>
    public void Add(int dayNumber, decimal rate, string rateParamName, string dateParamName)
    {
        FinancialThrowHelper.ThrowIfExchangeRateNotPositive(rate, rateParamName);

        var index = IndexOf(dayNumber);
        if (index >= 0)
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    FinancialResourceStrings.Arg_Invalid_RateSeriesDuplicateDate,
                    DateOnly.FromDayNumber(dayNumber)),
                dateParamName);
        }

        Insert(~index, dayNumber, rate);
    }

    /// <summary>
    /// Attempts to insert a new observation, returning <see langword="false" /> if one already exists for
    /// <paramref name="dayNumber" />.
    /// </summary>
    /// <param name="dayNumber">The observation day number.</param>
    /// <param name="rate">The observed rate.</param>
    /// <param name="rateParamName">
    /// The caller's parameter name to report in any raised <see cref="ArgumentOutOfRangeException" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the observation was inserted; <see langword="false" /> if one already existed for
    /// <paramref name="dayNumber" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="rate" /> is zero or negative.
    /// </exception>
    public bool TryAdd(int dayNumber, decimal rate, string rateParamName)
    {
        FinancialThrowHelper.ThrowIfExchangeRateNotPositive(rate, rateParamName);

        var index = IndexOf(dayNumber);
        if (index >= 0)
            return false;

        Insert(~index, dayNumber, rate);
        return true;
    }

    /// <summary>
    /// Updates the rate for an existing observation, throwing if none exists for <paramref name="dayNumber" />.
    /// </summary>
    /// <param name="dayNumber">The observation day number.</param>
    /// <param name="rate">The new rate.</param>
    /// <param name="rateParamName">
    /// The caller's parameter name to report in any raised <see cref="ArgumentOutOfRangeException" />.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="rate" /> is zero or negative.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown if no observation exists for <paramref name="dayNumber" />.
    /// </exception>
    public void Set(int dayNumber, decimal rate, string rateParamName)
    {
        FinancialThrowHelper.ThrowIfExchangeRateNotPositive(rate, rateParamName);

        var index = IndexOf(dayNumber);
        if (index < 0)
        {
            throw new KeyNotFoundException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    FinancialResourceStrings.IO_KeyNotFound_RateObservationDate,
                    DateOnly.FromDayNumber(dayNumber)));
        }

        _rates[index] = rate;
    }

    /// <summary>
    /// Attempts to update the rate for an existing observation; returns <see langword="false" /> if none exists.
    /// </summary>
    /// <param name="dayNumber">The observation day number.</param>
    /// <param name="rate">The new rate.</param>
    /// <param name="rateParamName">
    /// The caller's parameter name to report in any raised <see cref="ArgumentOutOfRangeException" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the rate was updated; <see langword="false" /> if no observation existed.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="rate" /> is zero or negative.
    /// </exception>
    public bool TrySet(int dayNumber, decimal rate, string rateParamName)
    {
        FinancialThrowHelper.ThrowIfExchangeRateNotPositive(rate, rateParamName);

        var index = IndexOf(dayNumber);
        if (index < 0)
            return false;

        _rates[index] = rate;
        return true;
    }

    /// <summary>
    /// Inserts a new observation or replaces the rate of an existing one.
    /// </summary>
    /// <param name="dayNumber">The observation day number.</param>
    /// <param name="rate">The rate to record.</param>
    /// <param name="rateParamName">
    /// The caller's parameter name to report in any raised <see cref="ArgumentOutOfRangeException" />.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="rate" /> is zero or negative.
    /// </exception>
    public void Upsert(int dayNumber, decimal rate, string rateParamName)
    {
        FinancialThrowHelper.ThrowIfExchangeRateNotPositive(rate, rateParamName);

        var index = IndexOf(dayNumber);
        if (index >= 0)
            _rates[index] = rate;
        else
            Insert(~index, dayNumber, rate);
    }

    /// <summary>
    /// Removes the observation for <paramref name="dayNumber" /> if present.
    /// </summary>
    /// <param name="dayNumber">The observation day number.</param>
    /// <returns>
    /// <see langword="true" /> if an observation was removed; <see langword="false" /> if none existed.
    /// </returns>
    public bool Remove(int dayNumber)
    {
        var index = IndexOf(dayNumber);
        if (index < 0)
            return false;

        RemoveAt(index);
        return true;
    }

    /// <summary>
    /// Reports whether an observation exists for <paramref name="dayNumber" />.
    /// </summary>
    /// <param name="dayNumber">The day number to query.</param>
    /// <returns><see langword="true" /> if an observation exists; otherwise <see langword="false" />.</returns>
    public bool Contains(int dayNumber) => IndexOf(dayNumber) >= 0;

    /// <summary>
    /// Attempts to retrieve the rate observed exactly on <paramref name="dayNumber" />.
    /// </summary>
    /// <param name="dayNumber">The day number to query.</param>
    /// <param name="rate">
    /// When this method returns <see langword="true" />, the observed rate; otherwise <see langword="default" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if an observation exists for <paramref name="dayNumber" />; otherwise
    /// <see langword="false" />.
    /// </returns>
    public bool TryGetRate(int dayNumber, out decimal rate)
    {
        var index = IndexOf(dayNumber);
        if (index < 0)
        {
            rate = default;
            return false;
        }

        rate = _rates[index];
        return true;
    }

    /// <summary>
    /// Inserts a batch of observations, rejecting duplicate dates inside the batch and dates that already exist in the
    /// buffer.
    /// </summary>
    /// <param name="observations">The observations to insert.</param>
    /// <param name="observationsParamName">The caller's parameter name to report in raised argument exceptions.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if any rate is zero or negative.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown if the batch contains a duplicate date or a date already present in the buffer.
    /// </exception>
    public void AddRange(IEnumerable<ExchangeRateObservation> observations, string observationsParamName)
    {
        List<(int DayNumber, decimal Rate)> incoming = PrepareIncoming(observations, observationsParamName);
        if (incoming.Count == 0)
            return;

        Merge(incoming, observationsParamName, throwOnConflict: true);
    }

    /// <summary>
    /// Merges a batch of observations: inserts new dates and replaces rates for dates already present. Rejects
    /// duplicate dates within the incoming batch.
    /// </summary>
    /// <param name="observations">The observations to merge.</param>
    /// <param name="observationsParamName">The caller's parameter name to report in raised argument exceptions.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if any rate is zero or negative.</exception>
    /// <exception cref="ArgumentException">Thrown if the batch contains a duplicate date.</exception>
    public void UpsertRange(IEnumerable<ExchangeRateObservation> observations, string observationsParamName)
    {
        List<(int DayNumber, decimal Rate)> incoming = PrepareIncoming(observations, observationsParamName);
        if (incoming.Count == 0)
            return;

        Merge(incoming, observationsParamName, throwOnConflict: false);
    }

    /// <summary>
    /// Enumerates the live observations in strictly ascending date order.
    /// </summary>
    /// <returns>A lazy sequence of <see cref="ExchangeRateObservation" /> values.</returns>
    public IEnumerable<ExchangeRateObservation> Enumerate()
    {
        for (var i = 0; i < Count; i++)
        {
            yield return new ExchangeRateObservation(DateOnly.FromDayNumber(_dayNumbers[i]), _rates[i]);
        }
    }

    /// <summary>
    /// Produces a fresh immutable storage snapshot of the live observations.
    /// </summary>
    /// <returns>A new <see cref="ExchangeRateSeriesStorage" /> instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the buffer holds no observations.</exception>
    public ExchangeRateSeriesStorage ToStorage()
    {
        if (Count == 0)
            throw new InvalidOperationException(FinancialResourceStrings.Op_Invalid_RateSeriesBuilderEmpty);

        var dayNumbers = new int[Count];
        var rates = new decimal[Count];

        Array.Copy(_dayNumbers, dayNumbers, Count);
        Array.Copy(_rates, rates, Count);

        return ExchangeRateSeriesStorage.AdoptSortedUniqueArrays(dayNumbers, rates);
    }

    /// <summary>
    /// Creates a buffer seeded from the supplied immutable storage. Mutations on the resulting buffer do not affect the
    /// source storage.
    /// </summary>
    /// <param name="storage">The storage to copy.</param>
    /// <returns>A new <see cref="ExchangeRateSeriesBuffer" /> with the same observations.</returns>
    public static ExchangeRateSeriesBuffer FromStorage(ExchangeRateSeriesStorage storage)
    {
        var count = storage.Count;
        var buffer = new ExchangeRateSeriesBuffer(count);

        storage.CopyTo(buffer._dayNumbers, buffer._rates);
        buffer.Count = count;
        return buffer;
    }

    /// <summary>
    /// Validates and pre-sorts an incoming batch into a duplicate-free list of (day, rate) pairs.
    /// </summary>
    /// <param name="observations">The candidate observations.</param>
    /// <param name="observationsParamName">The caller's parameter name to report in raised exceptions.</param>
    /// <returns>A sorted list of validated incoming observations.</returns>
    private static List<(int DayNumber, decimal Rate)> PrepareIncoming(
        IEnumerable<ExchangeRateObservation> observations,
        string observationsParamName) =>
        ExchangeRateObservationNormalizer.ToSortedUniqueList(observations, observationsParamName, allowEmpty: true);

    /// <summary>
    /// Merges a pre-validated sorted incoming batch with the existing buffer. The merge writes into fresh arrays and
    /// only commits to the buffer after the entire pass succeeds, so a thrown exception leaves the buffer unchanged.
    /// </summary>
    /// <param name="incoming">The sorted unique incoming batch.</param>
    /// <param name="observationsParamName">The caller's parameter name to report in raised exceptions.</param>
    /// <param name="throwOnConflict">
    /// When <see langword="true" /> (AddRange semantics) a collision with an existing date throws; when
    /// <see langword="false" /> (UpsertRange semantics) the incoming value replaces the existing one.
    /// </param>
    private void Merge(
        List<(int DayNumber, decimal Rate)> incoming,
        string observationsParamName,
        bool throwOnConflict)
    {
        var maxLength = Count + incoming.Count;
        var mergedDays = new int[maxLength];
        var mergedRates = new decimal[maxLength];

        var i = 0;
        var j = 0;
        var k = 0;

        while (i < Count && j < incoming.Count)
        {
            var existingDay = _dayNumbers[i];
            var incomingDay = incoming[j].DayNumber;

            if (existingDay < incomingDay)
            {
                mergedDays[k] = existingDay;
                mergedRates[k] = _rates[i];
                i++;
                k++;
            }
            else if (existingDay > incomingDay)
            {
                mergedDays[k] = incomingDay;
                mergedRates[k] = incoming[j].Rate;
                j++;
                k++;
            }
            else
            {
                if (throwOnConflict)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            FinancialResourceStrings.Arg_Invalid_RateSeriesAddRangeConflict,
                            DateOnly.FromDayNumber(incomingDay)),
                        observationsParamName);
                }

                mergedDays[k] = incomingDay;
                mergedRates[k] = incoming[j].Rate;
                i++;
                j++;
                k++;
            }
        }

        while (i < Count)
        {
            mergedDays[k] = _dayNumbers[i];
            mergedRates[k] = _rates[i];
            i++;
            k++;
        }

        while (j < incoming.Count)
        {
            mergedDays[k] = incoming[j].DayNumber;
            mergedRates[k] = incoming[j].Rate;
            j++;
            k++;
        }

        if (k != mergedDays.Length)
        {
            Array.Resize(ref mergedDays, k);
            Array.Resize(ref mergedRates, k);
        }

        _dayNumbers = mergedDays;
        _rates = mergedRates;
        Count = k;
    }

    /// <summary>
    /// Performs a binary search across the live segment of the day-number array.
    /// </summary>
    /// <param name="dayNumber">The day number to locate.</param>
    /// <returns>
    /// A non-negative index if the value is present; otherwise the bitwise complement of the insertion index.
    /// </returns>
    private int IndexOf(int dayNumber) => Array.BinarySearch(_dayNumbers, 0, Count, dayNumber);

    /// <summary>
    /// Inserts a new entry at <paramref name="index" />, shifting subsequent entries to the right.
    /// </summary>
    /// <param name="index">The insertion index in <c>[0, _count]</c>.</param>
    /// <param name="dayNumber">The day number to insert.</param>
    /// <param name="rate">The rate to insert.</param>
    private void Insert(int index, int dayNumber, decimal rate)
    {
        EnsureCapacity(Count + 1);

        var moveCount = Count - index;
        if (moveCount > 0)
        {
            Array.Copy(_dayNumbers, index, _dayNumbers, index + 1, moveCount);
            Array.Copy(_rates, index, _rates, index + 1, moveCount);
        }

        _dayNumbers[index] = dayNumber;
        _rates[index] = rate;
        Count++;
    }

    /// <summary>
    /// Removes the entry at <paramref name="index" />, shifting subsequent entries to the left.
    /// </summary>
    /// <param name="index">The index to remove from the live segment.</param>
    private void RemoveAt(int index)
    {
        var moveCount = Count - index - 1;
        if (moveCount > 0)
        {
            Array.Copy(_dayNumbers, index + 1, _dayNumbers, index, moveCount);
            Array.Copy(_rates, index + 1, _rates, index, moveCount);
        }

        Count--;
        _dayNumbers[Count] = 0;
        _rates[Count] = 0m;
    }

    /// <summary>
    /// Ensures both backing arrays can accommodate at least <paramref name="minimumCapacity" /> slots, growing using
    /// the standard doubling rule.
    /// </summary>
    /// <param name="minimumCapacity">The minimum capacity required.</param>
    private void EnsureCapacity(int minimumCapacity)
    {
        if (_dayNumbers.Length >= minimumCapacity)
            return;

        var newCapacity = _dayNumbers.Length == 0 ? DefaultCapacity : _dayNumbers.Length * 2;
        if (newCapacity < minimumCapacity)
            newCapacity = minimumCapacity;

        Array.Resize(ref _dayNumbers, newCapacity);
        Array.Resize(ref _rates, newCapacity);
    }
}
