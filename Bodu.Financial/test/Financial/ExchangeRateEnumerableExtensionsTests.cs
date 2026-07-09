// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateEnumerableExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates;
using Bodu.Financial.Extensions;

namespace Bodu.Financial;

/// <summary>
/// Verifies the behaviour of the <see cref="ExchangeRateEnumerableExtensions" /> materializers.
/// </summary>
[TestClass]
public partial class ExchangeRateEnumerableExtensionsTests
{
    private static readonly CurrencyPair AudUsd = new(CurrencyCode.AUD, CurrencyCode.USD);
    private static readonly DateOnly D1 = new(2024, 1, 3);

    private static ExchangeRate Rate(string from, string to, DateOnly date, decimal rate, string provider, DateTimeOffset? fetchedAtUtc = null) =>
        new(CurrencyInfo.ParseCurrencyCode(from), CurrencyInfo.ParseCurrencyCode(to), date, rate, provider, isInverted: false, fetchedAtUtc);
}
