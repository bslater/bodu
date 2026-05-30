// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyBag.Conversion.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Bodu.Numerics;

public sealed partial class MoneyBag
{
    /// <summary>
    /// Converts the entire bag to a single target currency by applying the supplied rate provider to every
    /// non-target balance.
    /// </summary>
    /// <typeparam name="TTarget">The destination currency type.</typeparam>
    /// <param name="rates">The provider that returns the rate for each (from, to) pair.</param>
    /// <returns>The aggregated <see cref="Money{TTarget}" /> total.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rates" /> is <see langword="null" />.</exception>
    /// <exception cref="KeyNotFoundException"><paramref name="rates" /> cannot supply a rate for one of the bag's currencies.</exception>
    public Money<TTarget> ConvertTo<TTarget>(IExchangeRateProvider rates)
        where TTarget : ICurrency
    {
        ThrowHelper.ThrowIfNull(rates);

        string targetIso = TTarget.IsoCode;
        decimal total = 0m;
        foreach (KeyValuePair<string, decimal> entry in _balances)
        {
            if (string.Equals(entry.Key, targetIso, StringComparison.Ordinal))
                total += entry.Value;
            else
                total += entry.Value * rates.GetRate(entry.Key, targetIso);
        }

        return new Money<TTarget>(total);
    }

    /// <summary>
    /// Converts the entire bag to a single target currency using a delegate-based rate lookup.
    /// </summary>
    /// <typeparam name="TTarget">The destination currency type.</typeparam>
    /// <param name="rateLookup">A delegate that returns the rate for a (from, to) pair.</param>
    /// <returns>The aggregated <see cref="Money{TTarget}" /> total.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rateLookup" /> is <see langword="null" />.</exception>
    public Money<TTarget> ConvertTo<TTarget>(Func<string, string, decimal> rateLookup)
        where TTarget : ICurrency
    {
        ThrowHelper.ThrowIfNull(rateLookup);

        string targetIso = TTarget.IsoCode;
        decimal total = 0m;
        foreach (KeyValuePair<string, decimal> entry in _balances)
        {
            if (string.Equals(entry.Key, targetIso, StringComparison.Ordinal))
                total += entry.Value;
            else
                total += entry.Value * rateLookup(entry.Key, targetIso);
        }

        return new Money<TTarget>(total);
    }
}
