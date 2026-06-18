// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AverageStrategyTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies the mean-of-contributors behaviour of <see cref="AverageStrategy" />.
/// </summary>
[TestClass]
public sealed partial class AverageStrategyTests
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
        new(name, new FixedDatedExchangeRateProvider(rows.Select(r => new ExchangeRate(r.From, r.To, r.Date, r.Rate, name))));
}
