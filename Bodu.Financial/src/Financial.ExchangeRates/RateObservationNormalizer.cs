// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateObservationNormalizer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Provides the shared validation, sorting, and deduplication pipeline used by <see cref="RateSeriesStorage" /> and
/// <see cref="RateSeriesBuffer" /> to turn raw observation input into a sorted strictly-ascending sequence of
/// <c>(dayNumber, rate)</c> pairs.
/// </summary>
internal static class RateObservationNormalizer
{
    /// <summary>
    /// Validates, sorts, and deduplicates the supplied observation sequence.
    /// </summary>
    /// <param name="observations">The candidate observations.</param>
    /// <param name="paramName">The caller-side parameter name reported in raised exceptions.</param>
    /// <param name="allowEmpty">
    /// When <see langword="true" />, an empty input enumerable returns an empty list. When <see langword="false" />, an
    /// empty input enumerable throws <see cref="ArgumentException" />.
    /// </param>
    /// <returns>A sorted strictly-ascending list of <c>(dayNumber, rate)</c> pairs.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="observations" /> is empty (and <paramref name="allowEmpty" /> is
    /// <see langword="false" />) or contains duplicate dates.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="observations" /> contains a rate that is zero or negative.
    /// </exception>
    public static List<(int DayNumber, decimal Rate)> ToSortedUniqueList(
        IEnumerable<RateObservation> observations,
        string paramName,
        bool allowEmpty)
    {
        List<(int DayNumber, decimal Rate)> buffer = new();
        foreach (RateObservation observation in observations)
        {
            if (observation.Rate <= 0m)
            {
                throw new ArgumentOutOfRangeException(
                    paramName,
                    observation.Rate,
                    FinancialResourceStrings.Arg_OutOfRange_RateNotPositive);
            }

            buffer.Add((observation.Date.DayNumber, observation.Rate));
        }

        return NormalizeBuffer(buffer, paramName, allowEmpty);
    }

    /// <summary>
    /// Validates, sorts, and deduplicates the supplied (date, rate) tuple sequence.
    /// </summary>
    /// <param name="rates">The candidate observations.</param>
    /// <param name="paramName">The caller-side parameter name reported in raised exceptions.</param>
    /// <param name="allowEmpty">
    /// When <see langword="true" />, an empty input enumerable returns an empty list. When <see langword="false" />, an
    /// empty input enumerable throws <see cref="ArgumentException" />.
    /// </param>
    /// <returns>A sorted strictly-ascending list of <c>(dayNumber, rate)</c> pairs.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="rates" /> is empty (and <paramref name="allowEmpty" /> is <see langword="false" />)
    /// or contains duplicate dates.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="rates" /> contains a rate that is zero or negative.
    /// </exception>
    public static List<(int DayNumber, decimal Rate)> ToSortedUniqueList(
        IEnumerable<(DateOnly Date, decimal Rate)> rates,
        string paramName,
        bool allowEmpty)
    {
        List<(int DayNumber, decimal Rate)> buffer = new();
        foreach ((DateOnly Date, decimal Rate) observation in rates)
        {
            if (observation.Rate <= 0m)
            {
                throw new ArgumentOutOfRangeException(
                    paramName,
                    observation.Rate,
                    FinancialResourceStrings.Arg_OutOfRange_RateNotPositive);
            }

            buffer.Add((observation.Date.DayNumber, observation.Rate));
        }

        return NormalizeBuffer(buffer, paramName, allowEmpty);
    }

    /// <summary>
    /// Sorts the supplied buffer in place, rejects duplicate day numbers, and applies the empty-input policy.
    /// </summary>
    /// <param name="buffer">The buffer to normalise.</param>
    /// <param name="paramName">The caller-side parameter name reported in raised exceptions.</param>
    /// <param name="allowEmpty">Whether an empty buffer is acceptable.</param>
    /// <returns>The normalised buffer.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="buffer" /> is empty (and <paramref name="allowEmpty" /> is <see langword="false" />)
    /// or contains duplicate day numbers.
    /// </exception>
    private static List<(int DayNumber, decimal Rate)> NormalizeBuffer(
        List<(int DayNumber, decimal Rate)> buffer,
        string paramName,
        bool allowEmpty)
    {
        if (buffer.Count == 0)
        {
            if (allowEmpty)
                return buffer;

            throw new ArgumentException(FinancialResourceStrings.Arg_Invalid_RateSeriesEmpty, paramName);
        }

        buffer.Sort(static (a, b) => a.DayNumber.CompareTo(b.DayNumber));

        for (int i = 1; i < buffer.Count; i++)
        {
            if (buffer[i].DayNumber == buffer[i - 1].DayNumber)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        FinancialResourceStrings.Arg_Invalid_RateSeriesDuplicateDate,
                        DateOnly.FromDayNumber(buffer[i].DayNumber)),
                    paramName);
            }
        }

        return buffer;
    }
}
