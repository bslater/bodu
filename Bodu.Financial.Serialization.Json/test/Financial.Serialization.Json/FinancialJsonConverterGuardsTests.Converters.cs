// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FinancialJsonConverterGuardsTests.Converters.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.ExchangeRates;

namespace Bodu.Financial.Serialization.Json;

public partial class FinancialJsonConverterGuardsTests
{

    /// <summary>
    /// Verifies further malformed-payload rejections: a non-string compact pair value, a duplicate property, and a
    /// non-object balances map.
    /// </summary>
    [TestMethod]
    public void Converters_WhenPayloadMalformed_ShouldThrowJsonException()
    {
        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<ExchangeRate>(
            "{\"pair\":123,\"date\":\"2024-01-15\",\"rate\":1,\"provider\":\"x\"}", s_strictOptions));
        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<CurrencyPair>("{\"from\":\"USD\",\"to\":\"JPY\",\"to\":\"GBP\"}", s_strictOptions));
        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<MoneyBag>("{\"balances\":123}", s_strictOptions));
    }
}
