// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateJsonConverterPolicyTests.ReadingMixedPairAndFrom.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.ExchangeRates;

namespace Bodu.Financial.Serialization.Json;

public partial class ExchangeRateJsonConverterPolicyTests
{

    /// <summary>
    /// Verifies that mixing <c>pair</c> with <c>from</c> in the same object is rejected.
    /// </summary>
    [TestMethod]
    public void ReadingMixedPairAndFrom_ShouldThrowJsonException()
    {
        string json = "{\"pair\":\"USD/JPY\",\"from\":\"USD\",\"to\":\"JPY\",\"date\":\"2024-05-30\",\"rate\":156.42,\"provider\":\"ECB\"}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<ExchangeRate>(json, Options(FinancialJsonPolicy.Strict));
        });
    }
}
