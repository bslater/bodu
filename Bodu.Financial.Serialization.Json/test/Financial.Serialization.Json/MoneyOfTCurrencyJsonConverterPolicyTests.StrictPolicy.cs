// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyOfTCurrencyJsonConverterPolicyTests.StrictPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.Serialization.Json;

public partial class MoneyOfTCurrencyJsonConverterPolicyTests
{

    /// <summary>
    /// Verifies that the strict policy continues to reject lowercase ISO codes — i.e. lenient adds tolerance
    /// without back-porting it onto Strict.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenReadingLowercaseCurrency_ShouldThrowJsonException()
    {
        string json = "{\"amount\":19.99,\"currency\":\"usd\"}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Money<USD>>(json, Options(FinancialJsonPolicy.Strict));
        });
    }
}
