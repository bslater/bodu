// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateSeriesKeyTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Test;
using Bodu.Test.Assertions;

namespace Bodu.Financial;

[TestClass]
public partial class ExchangeRateSeriesKeyTests
{
    private static readonly ExchangeRatePair s_usdAud = new(CurrencyCode.USD, CurrencyCode.AUD);
}
