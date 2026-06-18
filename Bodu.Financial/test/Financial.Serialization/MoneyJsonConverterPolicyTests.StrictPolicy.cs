// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyJsonConverterPolicyTests.StrictPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;

namespace Bodu.Financial.Serialization;

public partial class MoneyJsonConverterPolicyTests
{

    /// <summary>
    /// Verifies that the strict policy rejects lowercase currency on read.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenReadingLowercaseCurrency_ShouldThrowJsonException()
    {
        string json = "{\"amount\":19.99,\"currency\":\"usd\"}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Money>(json, Options(FinancialJsonPolicy.Strict));
        });
    }
}
