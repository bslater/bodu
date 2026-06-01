// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateSeriesBuilder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Financial;

/// <summary>
/// Provides a mutable construction and editing surface for an <see cref="ExchangeRateSeries" />, maintaining strictly
/// ascending unique observation dates and strictly positive rates while supporting single-observation edits and bulk
/// import.
/// </summary>
/// <remarks>
/// <para>
/// The builder is the natural entry point for assembling rate observations imperatively (manual data entry, streaming
/// import, merge with prior history). After mutations complete, call <see cref="ToSeries" /> to produce an immutable
/// <see cref="ExchangeRateSeries" /> snapshot for use in production lookup. Further mutations on the builder do not
/// affect previously produced snapshots, and vice versa.
/// </para>
/// <para>
/// Instances are not thread-safe; concurrent mutation requires external synchronisation.
/// </para>
/// </remarks>
[DebuggerDisplay("{Pair.FromIsoCode,nq}/{Pair.ToIsoCode,nq} ({Provider,nq}) Count={Count}")]
public sealed class ExchangeRateSeriesBuilder
{
    /// <summary>
    /// The mutable buffer holding the live observations.
    /// </summary>
    private readonly ExchangeRateSeriesBuffer _buffer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeRateSeriesBuilder" /> class with no observations.
    /// </summary>
    /// <param name="pair">The currency pair this builder is editing.</param>
    /// <param name="provider">The non-empty identifier of the publishing source.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="provider" /> is empty or white-space.</exception>
    public ExchangeRateSeriesBuilder(ExchangeRatePair pair, string provider)
    {
        FinancialThrowHelper.ThrowIfNullOrWhiteSpaceProvider(provider);

        Pair = pair;
        Provider = provider;
        _buffer = new ExchangeRateSeriesBuffer();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeRateSeriesBuilder" /> class seeded from the supplied
    /// immutable series.
    /// </summary>
    /// <param name="series">The series to copy observations from.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="series" /> is <see langword="null" />.
    /// </exception>
    public ExchangeRateSeriesBuilder(ExchangeRateSeries series)
    {
        ThrowHelper.ThrowIfNull(series);

        Pair = series.Pair;
        Provider = series.Provider;
        _buffer = ExchangeRateSeriesBuffer.FromStorage(series.GetStorage());
    }

    /// <summary>
    /// Gets the currency pair this builder is editing.
    /// </summary>
    /// <returns>The series pair.</returns>
    public ExchangeRatePair Pair { get; }

    /// <summary>
    /// Gets the identifier of the source the builder represents.
    /// </summary>
    /// <returns>A non-empty provider identifier.</returns>
    public string Provider { get; }

    /// <summary>
    /// Gets the number of observations currently held.
    /// </summary>
    /// <returns>A non-negative count.</returns>
    public int Count => _buffer.Count;

    /// <summary>
    /// Gets a value indicating whether the builder holds no observations.
    /// </summary>
    /// <returns><see langword="true" /> if the builder is empty; otherwise <see langword="false" />.</returns>
    public bool IsEmpty => _buffer.IsEmpty;

    /// <summary>
    /// Inserts a new observation; throws if an observation already exists for <paramref name="date" />.
    /// </summary>
    /// <param name="date">The observation date.</param>
    /// <param name="rate">The observed rate.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="rate" /> is zero or negative.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if an observation already exists for <paramref name="date" />.
    /// </exception>
    public void Add(DateOnly date, decimal rate) =>
        _buffer.Add(date.DayNumber, rate, nameof(rate), nameof(date));

    /// <summary>
    /// Attempts to insert a new observation; returns <see langword="false" /> if one already exists for
    /// <paramref name="date" />.
    /// </summary>
    /// <param name="date">The observation date.</param>
    /// <param name="rate">The observed rate.</param>
    /// <returns>
    /// <see langword="true" /> if the observation was inserted; <see langword="false" /> if one already existed.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="rate" /> is zero or negative.
    /// </exception>
    public bool TryAdd(DateOnly date, decimal rate) =>
        _buffer.TryAdd(date.DayNumber, rate, nameof(rate));

    /// <summary>
    /// Updates the rate for an existing observation; throws if no observation exists for <paramref name="date" />.
    /// </summary>
    /// <param name="date">The observation date to update.</param>
    /// <param name="rate">The new rate.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="rate" /> is zero or negative.
    /// </exception>
    /// <exception cref="KeyNotFoundException">Thrown if no observation exists for <paramref name="date" />.</exception>
    public void Set(DateOnly date, decimal rate) =>
        _buffer.Set(date.DayNumber, rate, nameof(rate));

    /// <summary>
    /// Attempts to update the rate for an existing observation; returns <see langword="false" /> if none exists.
    /// </summary>
    /// <param name="date">The observation date to update.</param>
    /// <param name="rate">The new rate.</param>
    /// <returns>
    /// <see langword="true" /> if the rate was updated; <see langword="false" /> if no observation existed.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="rate" /> is zero or negative.
    /// </exception>
    public bool TrySet(DateOnly date, decimal rate) =>
        _buffer.TrySet(date.DayNumber, rate, nameof(rate));

    /// <summary>
    /// Inserts a new observation or replaces the rate of an existing one for <paramref name="date" />.
    /// </summary>
    /// <param name="date">The observation date.</param>
    /// <param name="rate">The rate to record.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="rate" /> is zero or negative.
    /// </exception>
    public void Upsert(DateOnly date, decimal rate) =>
        _buffer.Upsert(date.DayNumber, rate, nameof(rate));

    /// <summary>
    /// Removes the observation for <paramref name="date" /> if present.
    /// </summary>
    /// <param name="date">The observation date to remove.</param>
    /// <returns>
    /// <see langword="true" /> if an observation was removed; <see langword="false" /> if none existed.
    /// </returns>
    public bool Remove(DateOnly date) => _buffer.Remove(date.DayNumber);

    /// <summary>
    /// Reports whether an observation exists for <paramref name="date" />.
    /// </summary>
    /// <param name="date">The observation date to query.</param>
    /// <returns>
    /// <see langword="true" /> if an observation exists; otherwise <see langword="false" />.
    /// </returns>
    public bool ContainsDate(DateOnly date) => _buffer.Contains(date.DayNumber);

    /// <summary>
    /// Attempts to retrieve the rate observed exactly on <paramref name="date" />.
    /// </summary>
    /// <param name="date">The observation date to query.</param>
    /// <param name="rate">
    /// When this method returns <see langword="true" />, the rate observed on <paramref name="date" />; otherwise
    /// <see langword="default" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if an observation exists; otherwise <see langword="false" />.
    /// </returns>
    public bool TryGetRate(DateOnly date, out decimal rate) => _buffer.TryGetRate(date.DayNumber, out rate);

    /// <summary>
    /// Inserts a batch of observations, rejecting duplicate dates inside the batch and dates already present in the
    /// builder. On any validation failure the builder remains unchanged.
    /// </summary>
    /// <param name="observations">The observations to insert.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="observations" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if any rate in <paramref name="observations" /> is zero or negative.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if the batch contains a duplicate date or a date already present in the builder.
    /// </exception>
    public void AddRange(IEnumerable<ExchangeRateObservation> observations)
    {
        ThrowHelper.ThrowIfNull(observations);
        _buffer.AddRange(observations, nameof(observations));
    }

    /// <summary>
    /// Merges a batch of observations into the builder: inserts new dates and replaces rates for dates already present.
    /// Rejects duplicate dates within the batch. On any validation failure the builder remains unchanged.
    /// </summary>
    /// <param name="observations">The observations to merge.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="observations" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if any rate in <paramref name="observations" /> is zero or negative.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown if the batch contains a duplicate date.</exception>
    public void UpsertRange(IEnumerable<ExchangeRateObservation> observations)
    {
        ThrowHelper.ThrowIfNull(observations);
        _buffer.UpsertRange(observations, nameof(observations));
    }

    /// <summary>
    /// Enumerates the observations in strictly ascending date order.
    /// </summary>
    /// <returns>A lazy sequence of <see cref="ExchangeRateObservation" /> values.</returns>
    public IEnumerable<ExchangeRateObservation> GetObservations() => _buffer.Enumerate();

    /// <summary>
    /// Produces an immutable <see cref="ExchangeRateSeries" /> snapshot of the current observations.
    /// </summary>
    /// <returns>A new <see cref="ExchangeRateSeries" /> reflecting the builder's current state.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the builder is empty; an immutable series must contain at least one observation.
    /// </exception>
    public ExchangeRateSeries ToSeries() => new(Pair, Provider, _buffer.ToStorage());
}
