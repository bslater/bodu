// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixedExchangeRateTable.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
/// The table is immutable after construction; consumers wanting live rates should implement
/// <see cref="IExchangeRateProvider" /> directly.
/// </para>
/// </remarks>
public sealed class FixedExchangeRateTable : IExchangeRateProvider
{
    /// <summary>
    /// The underlying rate table keyed by (source, destination) ISO code pairs.
    /// </summary>
    private readonly IReadOnlyDictionary<(string From, string To), decimal> _rates;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedExchangeRateTable" /> class from the supplied dictionary.
    /// </summary>
    /// <param name="rates">The rate table.</param>
    /// <exception cref="ArgumentNullException"><paramref name="rates" /> is <see langword="null" />.</exception>
    public FixedExchangeRateTable(IReadOnlyDictionary<(string From, string To), decimal> rates)
    {
        ThrowHelper.ThrowIfNull(rates);
        _rates = rates;
    }

    /// <inheritdoc />
    public decimal GetRate(string fromIsoCode, string toIsoCode)
    {
        ThrowHelper.ThrowIfNull(fromIsoCode);
        ThrowHelper.ThrowIfNull(toIsoCode);

        if (string.Equals(fromIsoCode, toIsoCode, StringComparison.Ordinal))
            return 1m;

        if (_rates.TryGetValue((fromIsoCode, toIsoCode), out var direct))
            return direct;

        if (_rates.TryGetValue((toIsoCode, fromIsoCode), out var inverse) && inverse != 0m)
            return 1m / inverse;

        throw new KeyNotFoundException(
            string.Format(CultureInfo.InvariantCulture, FinancialResourceStrings.IO_KeyNotFound_ExchangeRate, fromIsoCode, toIsoCode));
    }
}
