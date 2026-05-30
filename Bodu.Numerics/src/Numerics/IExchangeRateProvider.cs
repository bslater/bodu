// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IExchangeRateProvider.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

/// <summary>
/// Source of foreign-exchange rates used to convert between currencies.
/// </summary>
/// <remarks>
/// Implementations decide where rates come from: a static table, a daily snapshot, a live ticker, a mid-market
/// computed from bid/ask, and so on. Consumers of <see cref="MoneyBag.ConvertTo{TTarget}(IExchangeRateProvider)" />
/// only need the abstract <see cref="GetRate(string, string)" /> contract.
/// </remarks>
public interface IExchangeRateProvider
{
    /// <summary>
    /// Returns the exchange rate that converts one unit of <paramref name="fromIsoCode" /> to units of
    /// <paramref name="toIsoCode" />.
    /// </summary>
    /// <param name="fromIsoCode">The source currency's ISO 4217 code.</param>
    /// <param name="toIsoCode">The destination currency's ISO 4217 code.</param>
    /// <returns>The rate.</returns>
    /// <exception cref="KeyNotFoundException">No rate is available for the requested pair.</exception>
    decimal GetRate(string fromIsoCode, string toIsoCode);
}
