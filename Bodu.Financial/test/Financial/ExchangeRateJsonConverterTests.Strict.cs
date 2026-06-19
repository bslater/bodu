// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateJsonConverterTests.Strict.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;
using Bodu.Financial.Serialization;

namespace Bodu.Financial;

public partial class ExchangeRateJsonConverterTests
{

    /// <summary>
    /// Verifies that the object form accepts the <c>"pair"</c> shorthand and a string-typed rate.
    /// </summary>
    [TestMethod]
    public void Strict_WhenUsingPairShorthandAndStringRate_ShouldResolve()
    {
        string json = "{\"pair\":\"USD/JPY\",\"date\":\"2024-01-15\",\"rate\":\"150.25\",\"provider\":\"ecb\"}";

        ExchangeRate restored = JsonSerializer.Deserialize<ExchangeRate>(json);

        Assert.AreEqual(CurrencyCode.USD, restored.From);
        Assert.AreEqual(CurrencyCode.JPY, restored.To);
        Assert.AreEqual(150.25m, restored.Rate);
    }

    /// <summary>
    /// Verifies that malformed payloads — a non-object root, a slashless pair, a malformed date or rate, or a missing
    /// required property — are rejected with a <see cref="JsonException" />.
    /// </summary>
    [TestMethod]
    [DataRow("[1,2]", DisplayName = "Root is not an object")]
    [DataRow("{\"pair\":\"USDJPY\",\"date\":\"2024-01-15\",\"rate\":1,\"provider\":\"x\"}", DisplayName = "Pair lacks a slash")]
    [DataRow("{\"from\":\"USD\",\"to\":\"JPY\",\"date\":\"nope\",\"rate\":1,\"provider\":\"x\"}", DisplayName = "Date is malformed")]
    [DataRow("{\"from\":\"USD\",\"to\":\"JPY\",\"date\":\"2024-01-15\",\"rate\":true,\"provider\":\"x\"}", DisplayName = "Rate is boolean")]
    [DataRow("{\"from\":\"USD\",\"date\":\"2024-01-15\",\"rate\":1,\"provider\":\"x\"}", DisplayName = "Missing 'to'")]
    public void Strict_WhenPayloadMalformed_ShouldThrowJsonException(string json) =>
        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<ExchangeRate>(json));
}
