// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixedExchangeRateTable.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Frozen;
using System.Collections.Generic;

namespace Bodu.Numerics;

/// <summary>
/// Provides a timeless, dictionary-backed implementation of <see cref="IExchangeRateProvider" /> that stores a single
/// rate per currency pair.
/// </summary>
/// <remarks>
/// <para>
/// Same-currency lookups return the identity rate <c>1</c>, and when the directly-keyed pair is missing the table falls
/// back to the inverse pair (returning the reciprocal). For dated lookups with auditable metadata, use
/// <see cref="FixedDatedExchangeRateTable" /> in conjunction with <see cref="IDatedExchangeRateProvider" /> instead.
/// </para>
/// </remarks>
public sealed class FixedExchangeRateTable : IExchangeRateProvider
{
    /// <summary>
    /// The pair-keyed rate store, frozen after construction.
    /// </summary>
    private readonly FrozenDictionary<ExchangeRatePair, decimal> _rates;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedExchangeRateTable" /> class from the supplied pair/rate pairs.
    /// </summary>
    /// <param name="rates">The pair/rate observations to store.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="rates" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="rates" /> contains duplicate pair keys.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if any rate is zero or negative.</exception>
    public FixedExchangeRateTable(IEnumerable<KeyValuePair<ExchangeRatePair, decimal>> rates)
    {
        ThrowHelper.ThrowIfNull(rates);

        Dictionary<ExchangeRatePair, decimal> buffer = new();

        foreach (KeyValuePair<ExchangeRatePair, decimal> entry in rates)
        {
            if (entry.Value <= 0m)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rates),
                    entry.Value,
                    $"Rate for {entry.Key.FromIsoCode}/{entry.Key.ToIsoCode} must be greater than zero.");
            }

            if (!buffer.TryAdd(entry.Key, entry.Value))
            {
                throw new ArgumentException(
                    $"Duplicate pair {entry.Key.FromIsoCode}/{entry.Key.ToIsoCode} supplied.",
                    nameof(rates));
            }
        }

        _rates = buffer.ToFrozenDictionary();
    }

    /// <inheritdoc />
    public decimal GetRate(string fromIsoCode, string toIsoCode)
    {
        ExchangeRateValidation.RequireIsoCode(fromIsoCode);
        ExchangeRateValidation.RequireIsoCode(toIsoCode);

        if (string.Equals(fromIsoCode, toIsoCode, StringComparison.Ordinal))
            return 1m;

        ExchangeRatePair direct = new(fromIsoCode, toIsoCode);

        if (_rates.TryGetValue(direct, out decimal rate))
            return rate;

        ExchangeRatePair inverse = direct.Inverse();

        if (_rates.TryGetValue(inverse, out decimal inverseRate))
            return 1m / inverseRate;

        throw new KeyNotFoundException(
            $"No exchange rate available for {fromIsoCode} -> {toIsoCode}.");
    }
}
