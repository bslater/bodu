// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PriorityFallbackStrategyTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies the first-available behaviour of <see cref="PriorityFallbackStrategy" />.
/// </summary>
[TestClass]
public sealed partial class PriorityFallbackStrategyTests
{
    /// <summary>
    /// A fixed date used by the tests.
    /// </summary>
    private static readonly DateOnly D1 = new(2024, 1, 3);

    /// <summary>
    /// Builds a named candidate from observation rows.
    /// </summary>
    /// <param name="name">The candidate name.</param>
    /// <param name="rows">The observation rows.</param>
    /// <returns>The named candidate.</returns>
    private static NamedDatedExchangeRateProvider Named(string name, params (string From, string To, DateOnly Date, decimal Rate)[] rows) =>
        new(name, new FixedDatedRateProvider(rows.Select(r => new ExchangeRate(CurrencyInfo.ParseCurrencyCode(r.From), CurrencyInfo.ParseCurrencyCode(r.To), r.Date, r.Rate, name))));
}
