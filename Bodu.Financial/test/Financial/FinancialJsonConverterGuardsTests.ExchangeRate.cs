// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FinancialJsonConverterGuardsTests.ExchangeRate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.ExchangeRates;

namespace Bodu.Financial;

public partial class FinancialJsonConverterGuardsTests
{

    /// <summary>
    /// Verifies that the exchange-rate converter rejects a numeric property supplied as an unparseable string and a
    /// date property supplied as a non-string token.
    /// </summary>
    [TestMethod]
    [DataRow("{\"from\":\"USD\",\"to\":\"JPY\",\"date\":\"2024-01-15\",\"rate\":\"nope\",\"provider\":\"x\"}", DisplayName = "Rate string is not numeric")]
    [DataRow("{\"from\":\"USD\",\"to\":\"JPY\",\"date\":123,\"rate\":1,\"provider\":\"x\"}", DisplayName = "Date is not a string")]
    public void ExchangeRate_WhenValueTokenIsWrong_ShouldThrowJsonException(string json) =>
        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<ExchangeRate>(json));
}
