// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyOfTCurrencyExchangeRateExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;
using Bodu.Test.Assertions;

namespace Bodu.Financial;

[TestClass]
public partial class MoneyOfTCurrencyExchangeRateExtensionsTests
{
    private static readonly DateOnly s_d1 = new(2024, 1, 3);

    private static FixedDatedExchangeRateProvider BuildProvider() => new(
    [
        new ExchangeRate("USD", "AUD", s_d1, 1.50m, "RBA"),
    ]);
}
