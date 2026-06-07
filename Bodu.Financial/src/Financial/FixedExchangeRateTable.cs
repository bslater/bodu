// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixedExchangeRateTable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Frozen;
using System.Globalization;

namespace Bodu.Financial;

/// <summary>
/// An <see cref="IExchangeRateProvider" /> backed by a fixed dictionary of (from, to) → rate mappings.
/// </summary>
/// <remarks>
/// <para>
/// Same-currency lookups return <c>1m</c> without consulting the table. When a direct (from, to) pair is missing the
/// provider also tries the inverse (to, from) pair and, if found, returns <c>1 / rate</c>. This is the convention most
/// FX feeds use to keep the table minimal.
/// </para>
/// <para>
/// The table is immutable after construction. The constructor copies the supplied rates into a
/// <see cref="FrozenDictionary{TKey, TValue}" />, so subsequent mutations to the source collection cannot affect the
/// table. Each entry's ISO codes and rate are validated at construction time.
/// </para>
/// </remarks>
public sealed class FixedExchangeRateTable : IExchangeRateProvider
{
    /// <summary>
    /// The underlying rate table keyed by validated <see cref="ExchangeRatePair" /> values, frozen after construction
    /// for fast read-only access.
    /// </summary>
    private readonly FrozenDictionary<ExchangeRatePair, decimal> _rates;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedExchangeRateTable" /> class from the supplied dictionary of
    /// (from, to) → rate mappings, validating each entry and copying the contents into an immutable store.
    /// </summary>
    /// <param name="rates">The rate table to copy.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="rates" /> is <see langword="null" />, or if any key tuple contains a
    /// <see langword="null" /> ISO code.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if any key tuple's ISO code is not a three-character uppercase ASCII code.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if any rate in <paramref name="rates" /> is zero or negative.
    /// </exception>
    public FixedExchangeRateTable(IReadOnlyDictionary<(string From, string To), decimal> rates)
    {
        ThrowHelper.ThrowIfNull(rates);

        Dictionary<ExchangeRatePair, decimal> validated = new(rates.Count);
        foreach (KeyValuePair<(string From, string To), decimal> entry in rates)
        {
            FinancialThrowHelper.ThrowIfNotValidIsoCode(entry.Key.From, nameof(rates));
            FinancialThrowHelper.ThrowIfNotValidIsoCode(entry.Key.To, nameof(rates));
            FinancialThrowHelper.ThrowIfExchangeRateNotPositive(entry.Value, nameof(rates));

            validated[new ExchangeRatePair(entry.Key.From, entry.Key.To)] = entry.Value;
        }

        _rates = validated.ToFrozenDictionary();
    }

    /// <inheritdoc />
    public decimal GetRate(string fromIsoCode, string toIsoCode)
    {
        ThrowHelper.ThrowIfNull(fromIsoCode);
        ThrowHelper.ThrowIfNull(toIsoCode);

        if (string.Equals(fromIsoCode, toIsoCode, StringComparison.Ordinal))
            return 1m;

        ExchangeRatePair direct = new(fromIsoCode, toIsoCode);
        if (_rates.TryGetValue(direct, out var directRate))
            return directRate;

        ExchangeRatePair inverse = direct.Inverse();
        if (_rates.TryGetValue(inverse, out var inverseRate) && inverseRate != 0m)
            return 1m / inverseRate;

        throw new KeyNotFoundException(
            string.Format(CultureInfo.CurrentCulture, FinancialResourceStrings.IO_KeyNotFound_ExchangeRate, fromIsoCode, toIsoCode));
    }
}
