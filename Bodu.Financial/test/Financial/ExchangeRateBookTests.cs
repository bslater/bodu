// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateBookTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;
using Bodu.Test.Assertions;

namespace Bodu.Financial;

[TestClass]
public partial class ExchangeRateBookTests
{
    private static readonly ExchangeRatePair s_usdAud = new("USD", "AUD");
    private static readonly ExchangeRatePair s_eurAud = new("EUR", "AUD");

    private static ExchangeRateSeries BuildSeries(ExchangeRatePair pair, string provider, decimal rate) =>
        new(pair, provider, [(new DateOnly(2024, 1, 1), rate)]);
}
