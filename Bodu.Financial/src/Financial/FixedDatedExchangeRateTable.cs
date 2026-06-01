// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixedDatedExchangeRateTable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Frozen;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Bodu.Financial;

/// <summary>
/// Provides an immutable, in-memory implementation of <see cref="IDatedExchangeRateProvider" /> backed by a
/// <see cref="FrozenDictionary{TKey, TValue}" /> of currency pairs to <see cref="ExchangeRateSeries" /> instances.
/// </summary>
/// <remarks>
/// <para>
/// Each pair in the table is described by exactly one series and therefore one provider. To compose rates from multiple
/// publishing sources, stack several <see cref="FixedDatedExchangeRateTable" /> instances behind a
/// <see cref="CompositeDatedExchangeRateProvider" /> rather than mixing providers in a single table.
/// </para>
/// <para>
/// Lookups perform two levels of binary search — a <see cref="FrozenDictionary{TKey, TValue}" /> probe for the pair,
/// then an <see cref="Array.BinarySearch{T}(T[], T)" /> over the series' date array — and allocate no managed memory on
/// success or failure.
/// </para>
/// </remarks>
public sealed class FixedDatedExchangeRateTable : IDatedExchangeRateProvider
{
    /// <summary>
    /// The label used as the provider name on synthetic same-currency identity results. Exposed publicly so audit
    /// consumers can filter by it without depending on a magic-string literal.
    /// </summary>
    public const string IdentityProviderName = "Identity";

    /// <summary>
    /// The per-pair series store, frozen after construction for fast read-only access.
    /// </summary>
    private readonly FrozenDictionary<ExchangeRatePair, ExchangeRateSeries> _series;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedDatedExchangeRateTable" /> class from the supplied dated rate
    /// observations.
    /// </summary>
    /// <param name="rates">The exchange-rate observations to store, in any order.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="rates" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="rates" /> contains rates for the same currency pair from differing providers, or if
    /// two rates share the same pair and date.
    /// </exception>
    public FixedDatedExchangeRateTable(IEnumerable<ExchangeRate> rates)
    {
        ThrowHelper.ThrowIfNull(rates);

        _series = BuildSeries(rates);
    }

    /// <inheritdoc />
    public ExchangeRateLookupResult GetRate(
        string fromIsoCode,
        string toIsoCode,
        DateOnly date,
        ExchangeRateLookupOptions? options = null)
    {
        return TryGetRate(fromIsoCode, toIsoCode, date, options, out ExchangeRateLookupResult result)
            ? result
            : throw new KeyNotFoundException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    FinancialResourceStrings.IO_KeyNotFound_DatedExchangeRate,
                    fromIsoCode,
                    toIsoCode,
                    date,
                    (options ?? ExchangeRateLookupOptions.Exact).DateResolution,
                    (options ?? ExchangeRateLookupOptions.Exact).ToleranceDays));
    }

    /// <inheritdoc />
    public bool TryGetRate(
        string fromIsoCode,
        string toIsoCode,
        DateOnly date,
        ExchangeRateLookupOptions? options,
        out ExchangeRateLookupResult result)
    {
        FinancialThrowHelper.ThrowIfNotValidIsoCode(fromIsoCode);
        FinancialThrowHelper.ThrowIfNotValidIsoCode(toIsoCode);
        options ??= ExchangeRateLookupOptions.Exact;
        options.Validate();

        if (options.AllowSameCurrencyIdentityRate &&
            string.Equals(fromIsoCode, toIsoCode, StringComparison.Ordinal))
        {
            ExchangeRate identity = new(fromIsoCode, toIsoCode, date, 1m, IdentityProviderName);
            result = new ExchangeRateLookupResult(identity, date, options.DateResolution, 0);
            return true;
        }

        ExchangeRatePair directPair = new(fromIsoCode, toIsoCode);

        if (TryGetDirectRate(directPair, date, options, isInverted: false, out result))
            return true;

        if (options.AllowInverse)
        {
            ExchangeRatePair inversePair = directPair.Inverse();

            if (TryGetDirectRate(inversePair, date, options, isInverted: true, out result))
                return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Groups the supplied observations into one <see cref="ExchangeRateSeries" /> per <see cref="ExchangeRatePair" />,
    /// validating that every observation for a given pair carries the same provider.
    /// </summary>
    /// <param name="rates">The observations to group.</param>
    /// <returns>A frozen mapping from currency pair to series.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if observations for the same pair carry differing provider identifiers, or if duplicate dates are
    /// detected while building any series.
    /// </exception>
    private static FrozenDictionary<ExchangeRatePair, ExchangeRateSeries> BuildSeries(IEnumerable<ExchangeRate> rates)
    {
        Dictionary<ExchangeRatePair, (string Provider, List<(DateOnly Date, decimal Rate)> Entries)> grouped = new();

        foreach (ExchangeRate observation in rates)
        {
            ExchangeRatePair pair = new(observation.FromIsoCode, observation.ToIsoCode);

            if (!grouped.TryGetValue(pair, out (string Provider, List<(DateOnly, decimal)> Entries) bucket))
            {
                bucket = (observation.Provider, new List<(DateOnly, decimal)>());
                grouped[pair] = bucket;
            }
            else if (!string.Equals(bucket.Provider, observation.Provider, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        FinancialResourceStrings.Arg_Invalid_RateSeriesProviderConflict,
                        pair.FromIsoCode,
                        pair.ToIsoCode,
                        bucket.Provider,
                        observation.Provider),
                    nameof(rates));
            }

            bucket.Entries.Add((observation.Date, observation.Rate));
        }

        Dictionary<ExchangeRatePair, ExchangeRateSeries> materialised = new(grouped.Count);

        foreach (KeyValuePair<ExchangeRatePair, (string Provider, List<(DateOnly Date, decimal Rate)> Entries)> entry in grouped)
        {
            materialised[entry.Key] = new ExchangeRateSeries(entry.Key, entry.Value.Provider, entry.Value.Entries);
        }

        return materialised.ToFrozenDictionary();
    }

    /// <summary>
    /// Attempts to resolve a rate by direct probe of the supplied <paramref name="pair" />.
    /// </summary>
    /// <param name="pair">The pair to probe, possibly the inverse of the user's requested pair.</param>
    /// <param name="requestedDate">The original requested date.</param>
    /// <param name="options">The lookup options to apply.</param>
    /// <param name="isInverted">
    /// <see langword="true" /> if <paramref name="pair" /> is the inverse of the originally requested pair, in which
    /// case the returned rate is inverted before being reported back to the caller.
    /// </param>
    /// <param name="result">When this method returns <see langword="true" />, the resolved lookup result.</param>
    /// <returns><see langword="true" /> if a rate was resolved; otherwise <see langword="false" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryGetDirectRate(
        ExchangeRatePair pair,
        DateOnly requestedDate,
        ExchangeRateLookupOptions options,
        bool isInverted,
        out ExchangeRateLookupResult result)
    {
        if (!_series.TryGetValue(pair, out ExchangeRateSeries? series))
        {
            result = default;
            return false;
        }

        if (!series.TryGetRate(requestedDate, options, out DateOnly resolvedDate, out var rawRate))
        {
            result = default;
            return false;
        }

        var resolvedRate = isInverted ? 1m / rawRate : rawRate;

        var reportedFrom = isInverted ? pair.ToIsoCode : pair.FromIsoCode;
        var reportedTo = isInverted ? pair.FromIsoCode : pair.ToIsoCode;

        ExchangeRate rate = new(reportedFrom, reportedTo, resolvedDate, resolvedRate, series.Provider, isInverted);

        var offsetDays = Math.Abs(resolvedDate.DayNumber - requestedDate.DayNumber);
        result = new ExchangeRateLookupResult(rate, requestedDate, options.DateResolution, offsetDays);
        return true;
    }
}
