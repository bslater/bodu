// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyExchangeRateExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

/// <summary>
/// Verifies <see cref="MoneyExchangeRateExtensions" /> — the dated-conversion bridge for runtime-tagged money.
/// </summary>
[TestClass]
public partial class MoneyExchangeRateExtensionsTests
{
    /// <summary>
    /// Shared as-of date for the fixture.
    /// </summary>
    private static readonly DateOnly s_asOf = new(2024, 6, 30);

    /// <summary>
    /// Returns the shared test rate table.
    /// </summary>
    /// <returns>A provider with EUR/USD=1.10, JPY/USD=0.0067, USD/EUR=0.9091 (for inverse coverage).</returns>
    private static IDatedExchangeRateProvider BuildProvider() => new FixedDatedExchangeRateProvider(
    [
        new ExchangeRate("EUR", "USD", s_asOf, 1.10m, "RBA"),
        new ExchangeRate("JPY", "USD", s_asOf, 0.0067m, "RBA"),
    ]);
}
