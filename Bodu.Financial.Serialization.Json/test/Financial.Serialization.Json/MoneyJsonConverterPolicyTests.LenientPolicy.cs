// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyJsonConverterPolicyTests.LenientPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.Serialization.Json;

public partial class MoneyJsonConverterPolicyTests
{

    /// <summary>
    /// Verifies that the lenient policy accepts lowercase currency in the object form and normalises it.
    /// </summary>
    [TestMethod]
    public void LenientPolicy_WhenReadingLowercaseCurrency_ShouldSucceed()
    {
        string json = "{\"amount\":19.99,\"currency\":\"usd\"}";

        Money result = JsonSerializer.Deserialize<Money>(json, Options(FinancialJsonPolicy.Lenient));

        Assert.AreEqual(new Money(19.99m, CurrencyCode.USD), result);
    }
}
