// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DatedRateProviderExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates;

namespace Bodu.Financial;

/// <summary>
/// Verifies the behaviour of the <see cref="Bodu.Financial.Extensions.DatedRateProviderExtensions" />
/// materializers.
/// </summary>
[TestClass]
public partial class DatedRateProviderExtensionsTests
{
    private static readonly CurrencyPair AudUsd = new(CurrencyCode.AUD, CurrencyCode.USD);
    private static readonly CurrencyPair AudEur = new(CurrencyCode.AUD, CurrencyCode.EUR);
    private static readonly DateOnly RangeStart = new(2023, 1, 1);
    private static readonly DateOnly RangeEnd = new(2023, 1, 31);
    private static readonly DateOnly Known = new(2023, 1, 3);

    private static ExchangeRate Rate(string from, string to, DateOnly date, decimal rate, string provider = "Test", DateTimeOffset? fetchedAtUtc = null) =>
        new(CurrencyInfo.ParseCurrencyCode(from), CurrencyInfo.ParseCurrencyCode(to), date, rate, provider, isInverted: false, fetchedAtUtc);
}
