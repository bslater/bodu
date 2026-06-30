// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyOfTCurrencyJsonConverterPolicyTests.LenientPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.Serialization;

public partial class MoneyOfTCurrencyJsonConverterPolicyTests
{

    /// <summary>
    /// Verifies that <see cref="FinancialJsonPolicy.Lenient" /> normalises lowercase ISO codes during read.
    /// </summary>
    [TestMethod]
    public void LenientPolicy_WhenReadingLowercaseCurrency_ShouldSucceed()
    {
        string json = "{\"amount\":19.99,\"currency\":\"usd\"}";

        Money<USD> result = JsonSerializer.Deserialize<Money<USD>>(json, Options(FinancialJsonPolicy.Lenient));

        Assert.AreEqual(new Money<USD>(19.99m), result);
    }

    /// <summary>
    /// Verifies that <see cref="FinancialJsonPolicy.Lenient" /> trims whitespace around ISO codes during read.
    /// </summary>
    [TestMethod]
    public void LenientPolicy_WhenReadingPaddedCurrency_ShouldSucceed()
    {
        string json = "{\"amount\":19.99,\"currency\":\"  USD  \"}";

        Money<USD> result = JsonSerializer.Deserialize<Money<USD>>(json, Options(FinancialJsonPolicy.Lenient));

        Assert.AreEqual(new Money<USD>(19.99m), result);
    }
}
