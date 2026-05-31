// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRatePairJsonConverterPolicyTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bodu.Financial.Serialization;

/// <summary>
/// Verifies that <see cref="ExchangeRatePairJsonConverter" /> honours each <see cref="FinancialJsonPolicy" /> on
/// both the read and write paths.
/// </summary>
[TestClass]
public class ExchangeRatePairJsonConverterPolicyTests
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
    /// Verifies that the attribute-driven default serializer emits the canonical object form.
    /// </summary>
    [TestMethod]
    public void DefaultAttribute_WhenSerializing_ShouldEmitCanonicalObjectShape()
    {
        var pair = new ExchangeRatePair("USD", "JPY");

        var json = JsonSerializer.Serialize(pair);

        Assert.AreEqual("{\"from\":\"USD\",\"to\":\"JPY\"}", json);
    }

    /// <summary>
    /// Verifies that the <c>[JsonConverter]</c> attribute remains present so the no-options happy path picks the
    /// strict shape automatically.
    /// </summary>
    [TestMethod]
    public void ExchangeRatePair_WhenInspected_ShouldDeclareJsonConverterAttribute() => Assert.IsTrue(typeof(ExchangeRatePair).IsDefined(typeof(JsonConverterAttribute), inherit: false));

    /// <summary>
    /// Verifies that the canonical object form round-trips under <see cref="FinancialJsonPolicy.Strict" />.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenRoundTripping_ShouldPreserveValue()
    {
        var pair = new ExchangeRatePair("USD", "JPY");
        JsonSerializerOptions options = Options(FinancialJsonPolicy.Strict);

        var json = JsonSerializer.Serialize(pair, options);
        ExchangeRatePair recovered = JsonSerializer.Deserialize<ExchangeRatePair>(json, options);

        Assert.AreEqual(pair, recovered);
    }

    /// <summary>
    /// Verifies that the compact policy emits the slash-separated string form.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenSerializing_ShouldEmitFromSlashToString()
    {
        var pair = new ExchangeRatePair("USD", "JPY");

        var json = JsonSerializer.Serialize(pair, Options(FinancialJsonPolicy.Compact));

        Assert.AreEqual("\"USD/JPY\"", json);
    }

    /// <summary>
    /// Verifies that compact round-trip preserves the pair.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenRoundTripping_ShouldPreserveValue()
    {
        var pair = new ExchangeRatePair("EUR", "GBP");
        JsonSerializerOptions options = Options(FinancialJsonPolicy.Compact);

        var json = JsonSerializer.Serialize(pair, options);
        ExchangeRatePair recovered = JsonSerializer.Deserialize<ExchangeRatePair>(json, options);

        Assert.AreEqual(pair, recovered);
    }

    /// <summary>
    /// Verifies that compact reads reject the canonical object form.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenReadingObjectForm_ShouldThrowJsonException()
    {
        var json = "{\"from\":\"USD\",\"to\":\"JPY\"}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<ExchangeRatePair>(json, Options(FinancialJsonPolicy.Compact));
        });
    }

    /// <summary>
    /// Verifies that compact reads reject a string without a slash separator.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenReadingStringWithoutSlash_ShouldThrowJsonException()
    {
        var json = "\"USDJPY\"";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<ExchangeRatePair>(json, Options(FinancialJsonPolicy.Compact));
        });
    }

    /// <summary>
    /// Verifies that the lenient policy normalises lowercase ISO codes during object-form read.
    /// </summary>
    [TestMethod]
    public void LenientPolicy_WhenReadingLowercaseCurrency_ShouldSucceed()
    {
        var json = "{\"from\":\"usd\",\"to\":\"jpy\"}";

        ExchangeRatePair pair = JsonSerializer.Deserialize<ExchangeRatePair>(json, Options(FinancialJsonPolicy.Lenient));

        Assert.AreEqual(new ExchangeRatePair("USD", "JPY"), pair);
    }

    /// <summary>
    /// Verifies that the strict policy rejects lowercase ISO codes during object-form read.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenReadingLowercaseCurrency_ShouldThrowJsonException()
    {
        var json = "{\"from\":\"usd\",\"to\":\"jpy\"}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<ExchangeRatePair>(json, Options(FinancialJsonPolicy.Strict));
        });
    }

    /// <summary>
    /// Verifies that a duplicate <c>"from"</c> property is rejected.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenDuplicateFromProperty_ShouldThrowJsonException()
    {
        var json = "{\"from\":\"USD\",\"from\":\"EUR\",\"to\":\"JPY\"}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<ExchangeRatePair>(json, Options(FinancialJsonPolicy.Strict));
        });
    }

    /// <summary>
    /// Verifies that a missing <c>"from"</c> property is rejected.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenMissingFromProperty_ShouldThrowJsonException()
    {
        var json = "{\"to\":\"JPY\"}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<ExchangeRatePair>(json, Options(FinancialJsonPolicy.Strict));
        });
    }
}
