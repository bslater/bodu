// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateJsonConverterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Serialization;

namespace Bodu.Financial;

/// <summary>
/// Verifies <see cref="ExchangeRateJsonConverter" /> across the Strict, Lenient, and Compact policies, the
/// <c>"pair"</c> shorthand, and the malformed-payload rejection paths.
/// </summary>
[TestClass]
public class ExchangeRateJsonConverterTests
{
    private static JsonSerializerOptions OptionsFor(FinancialJsonPolicy policy) =>
        new JsonSerializerOptions().AddFinancialJsonConverters(policy);

    private static ExchangeRate Sample() =>
        new("USD", "JPY", new DateOnly(2024, 1, 15), 150.25m, "ecb");

    /// <summary>
    /// Verifies that the Strict and Compact policies both round-trip the rate's identifying fields.
    /// </summary>
    [TestMethod]
    [DataRow(FinancialJsonPolicy.Strict)]
    [DataRow(FinancialJsonPolicy.Compact)]
    public void RoundTrip_ShouldPreserveIdentifyingFields(FinancialJsonPolicy policy)
    {
        JsonSerializerOptions options = OptionsFor(policy);

        string json = JsonSerializer.Serialize(Sample(), options);
        ExchangeRate restored = JsonSerializer.Deserialize<ExchangeRate>(json, options);

        Assert.AreEqual("USD", restored.FromIsoCode);
        Assert.AreEqual("JPY", restored.ToIsoCode);
        Assert.AreEqual(new DateOnly(2024, 1, 15), restored.Date);
        Assert.AreEqual(150.25m, restored.Rate);
        Assert.AreEqual("ecb", restored.Provider);
    }

    /// <summary>
    /// Verifies that the object form accepts the <c>"pair"</c> shorthand and a string-typed rate.
    /// </summary>
    [TestMethod]
    public void Strict_WhenUsingPairShorthandAndStringRate_ShouldResolve()
    {
        string json = "{\"pair\":\"USD/JPY\",\"date\":\"2024-01-15\",\"rate\":\"150.25\",\"provider\":\"ecb\"}";

        ExchangeRate restored = JsonSerializer.Deserialize<ExchangeRate>(json);

        Assert.AreEqual("USD", restored.FromIsoCode);
        Assert.AreEqual("JPY", restored.ToIsoCode);
        Assert.AreEqual(150.25m, restored.Rate);
    }

    /// <summary>
    /// Verifies that the Lenient policy upper-cases lower-case ISO codes.
    /// </summary>
    [TestMethod]
    public void Lenient_WhenIsoCodesLowercase_ShouldNormalize()
    {
        string json = "{\"from\":\"usd\",\"to\":\"jpy\",\"date\":\"2024-01-15\",\"rate\":150.25,\"provider\":\"ecb\"}";

        ExchangeRate restored = JsonSerializer.Deserialize<ExchangeRate>(json, OptionsFor(FinancialJsonPolicy.Lenient));

        Assert.AreEqual("USD", restored.FromIsoCode);
        Assert.AreEqual("JPY", restored.ToIsoCode);
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
