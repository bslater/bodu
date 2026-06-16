// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateRangeResult.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Financial;

/// <summary>
/// Represents the outcome of a range exchange-rate lookup: the observations that fall within the requested window,
/// ordered by date, together with the metadata that describes the request and the span actually covered.
/// </summary>
/// <remarks>
/// <para>
/// The type implements <see cref="IReadOnlyList{T}" /> over its <see cref="Rates" />, so it enumerates, indexes, and
/// composes with LINQ exactly like a plain sequence (<c>foreach (var rate in result)</c>, <c>result[0]</c>,
/// <c>result.Where(...)</c>) while still exposing the requested window through <see cref="RequestedStartDate" /> and
/// <see cref="RequestedEndDate" />, and the observed span through <see cref="FirstObservedDate" /> and
/// <see cref="LastObservedDate" />. Comparing the observed span to the requested window lets a caller see how much of
/// the window carried data without re-deriving it from the elements.
/// </para>
/// <para>
/// The range lookup does not apply a date-resolution policy and the per-observation provider and inversion flag are
/// carried on each <see cref="ExchangeRate" />, so no per-row resolution metadata is repeated here.
/// </para>
/// </remarks>
public sealed class ExchangeRateRangeResult
    : IReadOnlyList<ExchangeRate>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeRateRangeResult" /> class.
    /// </summary>
    /// <param name="fromIsoCode">The requested source-currency ISO code.</param>
    /// <param name="toIsoCode">The requested destination-currency ISO code.</param>
    /// <param name="requestedStartDate">The inclusive start of the requested window.</param>
    /// <param name="requestedEndDate">The inclusive end of the requested window.</param>
    /// <param name="rates">The observations within the window, ordered by date.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="fromIsoCode" />, <paramref name="toIsoCode" />, or <paramref name="rates" /> is
    /// <see langword="null" />.
    /// </exception>
    public ExchangeRateRangeResult(
        string fromIsoCode,
        string toIsoCode,
        DateOnly requestedStartDate,
        DateOnly requestedEndDate,
        IReadOnlyList<ExchangeRate> rates)
    {
        ThrowHelper.ThrowIfNull(fromIsoCode);
        ThrowHelper.ThrowIfNull(toIsoCode);
        ThrowHelper.ThrowIfNull(rates);

        FromIsoCode = fromIsoCode;
        ToIsoCode = toIsoCode;
        RequestedStartDate = requestedStartDate;
        RequestedEndDate = requestedEndDate;
        Rates = rates;
    }

    /// <summary>
    /// Gets the requested source-currency ISO code.
    /// </summary>
    /// <returns>The source-currency ISO code.</returns>
    public string FromIsoCode { get; }

    /// <summary>
    /// Gets the requested destination-currency ISO code.
    /// </summary>
    /// <returns>The destination-currency ISO code.</returns>
    public string ToIsoCode { get; }

    /// <summary>
    /// Gets the inclusive start of the requested window.
    /// </summary>
    /// <returns>The requested start date.</returns>
    public DateOnly RequestedStartDate { get; }

    /// <summary>
    /// Gets the inclusive end of the requested window.
    /// </summary>
    /// <returns>The requested end date.</returns>
    public DateOnly RequestedEndDate { get; }

    /// <summary>
    /// Gets the observations within the window, ordered by date.
    /// </summary>
    /// <returns>The rates in the range.</returns>
    public IReadOnlyList<ExchangeRate> Rates { get; }

    /// <summary>
    /// Gets the number of observations in the range.
    /// </summary>
    /// <returns>The observation count.</returns>
    public int Count => Rates.Count;

    /// <summary>
    /// Gets a value indicating whether the range contains no observations.
    /// </summary>
    /// <returns><see langword="true" /> when no observations are present; otherwise <see langword="false" />.</returns>
    public bool IsEmpty => Rates.Count == 0;

    /// <summary>
    /// Gets the earliest observation date present in the range, or <see langword="null" /> when the range is empty.
    /// </summary>
    /// <returns>The first observed date, or <see langword="null" />.</returns>
    public DateOnly? FirstObservedDate => Rates.Count == 0 ? null : Rates[0].Date;

    /// <summary>
    /// Gets the latest observation date present in the range, or <see langword="null" /> when the range is empty.
    /// </summary>
    /// <returns>The last observed date, or <see langword="null" />.</returns>
    public DateOnly? LastObservedDate => Rates.Count == 0 ? null : Rates[Rates.Count - 1].Date;

    /// <summary>
    /// Gets the observation at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index.</param>
    /// <returns>The observation at <paramref name="index" />.</returns>
    public ExchangeRate this[int index] => Rates[index];

    /// <summary>
    /// Returns an enumerator that iterates the observations in date order.
    /// </summary>
    /// <returns>An enumerator over the observations.</returns>
    public IEnumerator<ExchangeRate> GetEnumerator() => Rates.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
