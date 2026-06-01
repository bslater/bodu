// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateJsonConverterPolicyTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bodu.Financial.Serialization;

/// <summary>
/// Verifies that <see cref="ExchangeRateJsonConverter" /> honours each <see cref="FinancialJsonPolicy" /> on both
/// the read and write paths.
/// </summary>
[TestClass]
public class ExchangeRateJsonConverterPolicyTests
{
    /// <summary>
    /// Builds a <see cref="JsonSerializerOptions" /> seeded with the financial converters under
    /// <paramref name="policy" />.
    /// </summary>
    /// <param name="policy">The policy under test.</param>
    /// <returns>The configured options.</returns>
    private static JsonSerializerOptions Options(FinancialJsonPolicy policy) =>
        new JsonSerializerOptions().AddFinancialJsonConverters(policy);

    /// <summary>
    /// Builds a representative exchange-rate observation.
    /// </summary>
    private static ExchangeRate SampleRate(bool isInverted = false) =>
        new("USD", "JPY", new DateOnly(2024, 5, 30), 156.42m, "ECB", isInverted);

    /// <summary>
    /// Verifies that the attribute-driven default emits the canonical object form, with all six fields in
    /// declaration order.
    /// </summary>
    [TestMethod]
    public void DefaultAttribute_WhenSerializing_ShouldEmitCanonicalObjectShape()
    {
        var json = JsonSerializer.Serialize(SampleRate());

        Assert.AreEqual(
            "{\"from\":\"USD\",\"to\":\"JPY\",\"date\":\"2024-05-30\",\"rate\":156.42,\"provider\":\"ECB\",\"isInverted\":false}",
            json);
    }

    /// <summary>
    /// Verifies that the <c>[JsonConverter]</c> attribute remains present.
    /// </summary>
    [TestMethod]
    public void ExchangeRate_WhenInspected_ShouldDeclareJsonConverterAttribute() => Assert.IsTrue(typeof(ExchangeRate).IsDefined(typeof(JsonConverterAttribute), inherit: false));

    /// <summary>
    /// Verifies a round-trip under <see cref="FinancialJsonPolicy.Strict" />.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenRoundTripping_ShouldPreserveValue()
    {
        ExchangeRate original = SampleRate(isInverted: true);
        JsonSerializerOptions options = Options(FinancialJsonPolicy.Strict);

        var json = JsonSerializer.Serialize(original, options);
        ExchangeRate recovered = JsonSerializer.Deserialize<ExchangeRate>(json, options);

        Assert.AreEqual(original, recovered);
    }

    /// <summary>
    /// Verifies that the compact policy combines the currencies into a <c>"pair"</c> property and drops
    /// <c>isInverted</c> when it is <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenSerializingNonInverted_ShouldEmitPairAndOmitIsInverted()
    {
        var json = JsonSerializer.Serialize(SampleRate(), Options(FinancialJsonPolicy.Compact));

        Assert.AreEqual(
            "{\"pair\":\"USD/JPY\",\"date\":\"2024-05-30\",\"rate\":156.42,\"provider\":\"ECB\"}",
            json);
    }

    /// <summary>
    /// Verifies that the compact policy keeps <c>isInverted</c> when it is <see langword="true" /> so the audit
    /// trail does not silently lose the flag.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenSerializingInverted_ShouldEmitIsInverted()
    {
        var json = JsonSerializer.Serialize(SampleRate(isInverted: true), Options(FinancialJsonPolicy.Compact));

        Assert.AreEqual(
            "{\"pair\":\"USD/JPY\",\"date\":\"2024-05-30\",\"rate\":156.42,\"provider\":\"ECB\",\"isInverted\":true}",
            json);
    }

    /// <summary>
    /// Verifies that the compact round-trip preserves every observation field.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenRoundTripping_ShouldPreserveValue()
    {
        ExchangeRate original = SampleRate(isInverted: true);
        JsonSerializerOptions options = Options(FinancialJsonPolicy.Compact);

        var json = JsonSerializer.Serialize(original, options);
        ExchangeRate recovered = JsonSerializer.Deserialize<ExchangeRate>(json, options);

        Assert.AreEqual(original, recovered);
    }

    /// <summary>
    /// Verifies that the read path accepts both the canonical <c>from</c>/<c>to</c> and the compact
    /// <c>pair</c> shapes interchangeably under any policy.
    /// </summary>
    [TestMethod]
    public void ReadingFromToShape_ShouldSucceedRegardlessOfWritePolicy()
    {
        var json = "{\"pair\":\"USD/JPY\",\"date\":\"2024-05-30\",\"rate\":156.42,\"provider\":\"ECB\"}";

        ExchangeRate result = JsonSerializer.Deserialize<ExchangeRate>(json, Options(FinancialJsonPolicy.Strict));

        Assert.AreEqual(SampleRate(), result);
    }

    /// <summary>
    /// Verifies that mixing <c>pair</c> with <c>from</c> in the same object is rejected.
    /// </summary>
    [TestMethod]
    public void ReadingMixedPairAndFrom_ShouldThrowJsonException()
    {
        var json = "{\"pair\":\"USD/JPY\",\"from\":\"USD\",\"to\":\"JPY\",\"date\":\"2024-05-30\",\"rate\":156.42,\"provider\":\"ECB\"}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<ExchangeRate>(json, Options(FinancialJsonPolicy.Strict));
        });
    }

    /// <summary>
    /// Verifies that the lenient policy normalises lowercase ISO codes on read.
    /// </summary>
    [TestMethod]
    public void LenientPolicy_WhenReadingLowercaseCurrency_ShouldSucceed()
    {
        var json = "{\"from\":\"usd\",\"to\":\"jpy\",\"date\":\"2024-05-30\",\"rate\":156.42,\"provider\":\"ECB\",\"isInverted\":false}";

        ExchangeRate result = JsonSerializer.Deserialize<ExchangeRate>(json, Options(FinancialJsonPolicy.Lenient));

        Assert.AreEqual(SampleRate(), result);
    }

    /// <summary>
    /// Verifies that the strict policy rejects lowercase ISO codes on read.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenReadingLowercaseCurrency_ShouldThrowJsonException()
    {
        var json = "{\"from\":\"usd\",\"to\":\"jpy\",\"date\":\"2024-05-30\",\"rate\":156.42,\"provider\":\"ECB\",\"isInverted\":false}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<ExchangeRate>(json, Options(FinancialJsonPolicy.Strict));
        });
    }

    /// <summary>
    /// Verifies that a duplicate <c>"rate"</c> property is rejected.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenDuplicateRateProperty_ShouldThrowJsonException()
    {
        var json = "{\"from\":\"USD\",\"to\":\"JPY\",\"date\":\"2024-05-30\",\"rate\":1.0,\"rate\":2.0,\"provider\":\"ECB\",\"isInverted\":false}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<ExchangeRate>(json, Options(FinancialJsonPolicy.Strict));
        });
    }

    /// <summary>
    /// Verifies that the strict policy rejects a zero rate at the ExchangeRate ctor boundary, surfacing the
    /// failure as <see cref="JsonException" />.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenRateIsZero_ShouldThrowJsonException()
    {
        var json = "{\"from\":\"USD\",\"to\":\"JPY\",\"date\":\"2024-05-30\",\"rate\":0,\"provider\":\"ECB\",\"isInverted\":false}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<ExchangeRate>(json, Options(FinancialJsonPolicy.Strict));
        });
    }

    /// <summary>
    /// Verifies that a missing required field is rejected.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenMissingDate_ShouldThrowJsonException()
    {
        var json = "{\"from\":\"USD\",\"to\":\"JPY\",\"rate\":156.42,\"provider\":\"ECB\",\"isInverted\":false}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<ExchangeRate>(json, Options(FinancialJsonPolicy.Strict));
        });
    }
}
