// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateBookTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

[TestClass]
public partial class RateBookTests
{
    private static readonly CurrencyPair s_usdAud = new(CurrencyCode.USD, CurrencyCode.AUD);
    private static readonly CurrencyPair s_eurAud = new(CurrencyCode.EUR, CurrencyCode.AUD);

    private static RateSeries BuildSeries(CurrencyPair pair, string provider, decimal rate) =>
        new(pair, provider, [(new DateOnly(2024, 1, 1), rate)]);
}
