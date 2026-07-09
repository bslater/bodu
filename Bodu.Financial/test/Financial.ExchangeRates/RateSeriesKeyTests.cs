// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateSeriesKeyTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

[TestClass]
public partial class RateSeriesKeyTests
{
    private static readonly CurrencyPair s_usdAud = new(CurrencyCode.USD, CurrencyCode.AUD);
}
