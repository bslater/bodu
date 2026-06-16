// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FinancialJsonConverterGuardsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bodu.Financial.Currencies;
using Bodu.Financial.Serialization;

namespace Bodu.Financial;

/// <summary>
/// Verifies the defensive reader guards in the financial JSON converters. The unexpected-end and
/// expected-property-name guards cannot be reached through <see cref="JsonSerializer" /> with a complete, comment-free
/// document — the reader enforces object grammar first — so they are driven directly: an unexpected end via a
/// truncated buffer read in non-final-block mode, and an unexpected token via comment-handling that surfaces a comment
/// where a property name is required.
/// </summary>
[TestClass]
public class FinancialJsonConverterGuardsTests
{
    private static readonly JsonSerializerOptions s_options = JsonSerializerOptions.Default;

    /// <summary>
    /// Invokes a converter's <c>Read</c> against a hand-built reader and asserts that it throws
    /// <see cref="JsonException" />.
    /// </summary>
    /// <typeparam name="T">The converted type.</typeparam>
    /// <param name="converter">The converter under test.</param>
    /// <param name="json">The (possibly truncated) JSON fragment.</param>
    /// <param name="isFinalBlock"><see langword="false" /> to simulate a partial streaming buffer.</param>
    /// <param name="allowComments"><see langword="true" /> to surface comment tokens to the converter.</param>
    private static void AssertReadThrowsJsonException<T>(JsonConverter<T> converter, string json, bool isFinalBlock = true, bool allowComments = false)
    {
        byte[] utf8 = Encoding.UTF8.GetBytes(json);
        var readerOptions = new JsonReaderOptions
        {
            CommentHandling = allowComments ? JsonCommentHandling.Allow : JsonCommentHandling.Disallow,
        };

        var reader = new Utf8JsonReader(utf8, isFinalBlock, new JsonReaderState(readerOptions));
        Assert.IsTrue(reader.Read(), "Expected the fragment to begin with a readable token.");

        bool threw = false;
        try
        {
            _ = converter.Read(ref reader, typeof(T), s_options);
        }
        catch (JsonException)
        {
            threw = true;
        }

        Assert.IsTrue(threw, $"Expected a JsonException reading: {json}");
    }

    /// <summary>
    /// Verifies that every object-form converter throws when a property name is required but a non-property token (a
    /// comment) is surfaced instead.
    /// </summary>
    [TestMethod]
    public void ObjectConverters_WhenTokenIsNotaPropertyName_ShouldThrowJsonException()
    {
        AssertReadThrowsJsonException(new MoneyJsonConverter(FinancialJsonPolicy.Strict), "{ /* c */ \"amount\":1}", allowComments: true);
        AssertReadThrowsJsonException(new MoneyOfTCurrencyJsonConverter<USD>(FinancialJsonPolicy.Strict), "{ /* c */ \"amount\":1}", allowComments: true);
        AssertReadThrowsJsonException(new MoneyBagJsonConverter(FinancialJsonPolicy.Strict), "{ /* c */ \"balances\":{}}", allowComments: true);
        AssertReadThrowsJsonException(new MoneyBagJsonConverter(FinancialJsonPolicy.Strict), "{\"balances\":{ /* c */ \"USD\":1}}", allowComments: true);
        AssertReadThrowsJsonException(new ExchangeRateJsonConverter(FinancialJsonPolicy.Strict), "{ /* c */ \"from\":\"USD\"}", allowComments: true);
        AssertReadThrowsJsonException(new ExchangeRatePairJsonConverter(FinancialJsonPolicy.Strict), "{ /* c */ \"from\":\"USD\"}", allowComments: true);
    }

    /// <summary>
    /// Verifies that every object-form converter throws when the buffer ends immediately after a property name, before
    /// its value can be read.
    /// </summary>
    [TestMethod]
    public void ObjectConverters_WhenBufferEndsAfterPropertyName_ShouldThrowJsonException()
    {
        AssertReadThrowsJsonException(new MoneyJsonConverter(FinancialJsonPolicy.Strict), "{\"amount\":", isFinalBlock: false);
        AssertReadThrowsJsonException(new MoneyOfTCurrencyJsonConverter<USD>(FinancialJsonPolicy.Strict), "{\"amount\":", isFinalBlock: false);
        AssertReadThrowsJsonException(new MoneyBagJsonConverter(FinancialJsonPolicy.Strict), "{\"balances\":", isFinalBlock: false);
        AssertReadThrowsJsonException(new MoneyBagJsonConverter(FinancialJsonPolicy.Strict), "{\"balances\":{\"USD\":", isFinalBlock: false);
        AssertReadThrowsJsonException(new ExchangeRateJsonConverter(FinancialJsonPolicy.Strict), "{\"from\":", isFinalBlock: false);
        AssertReadThrowsJsonException(new ExchangeRatePairJsonConverter(FinancialJsonPolicy.Strict), "{\"from\":", isFinalBlock: false);
    }

    /// <summary>
    /// Verifies that the object-form converters ignore unknown properties, exercising the skip branch.
    /// </summary>
    [TestMethod]
    public void ObjectConverters_WhenUnknownPropertyPresent_ShouldIgnoreIt()
    {
        Assert.AreEqual(new Money(1m, "USD"), JsonSerializer.Deserialize<Money>("{\"amount\":1,\"currency\":\"USD\",\"note\":\"x\"}"));
        Assert.AreEqual(new Money<USD>(1m), JsonSerializer.Deserialize<Money<USD>>("{\"amount\":1,\"currency\":\"USD\",\"note\":\"x\"}"));

        MoneyBag bag = JsonSerializer.Deserialize<MoneyBag>("{\"note\":\"x\",\"balances\":{\"USD\":1}}")!;
        Assert.AreEqual(new Money(1m, "USD"), bag.GetBalance("USD"));

        ExchangeRatePair pair = JsonSerializer.Deserialize<ExchangeRatePair>("{\"from\":\"USD\",\"to\":\"JPY\",\"note\":\"x\"}");
        Assert.AreEqual("USD", pair.FromIsoCode);

        ExchangeRate rate = JsonSerializer.Deserialize<ExchangeRate>(
            "{\"from\":\"USD\",\"to\":\"JPY\",\"date\":\"2024-01-15\",\"rate\":1.5,\"provider\":\"x\",\"note\":\"y\"}");
        Assert.AreEqual("USD", rate.FromIsoCode);
    }

    /// <summary>
    /// Verifies further malformed-payload rejections: a non-string compact pair value, a duplicate property, and a
    /// non-object balances map.
    /// </summary>
    [TestMethod]
    public void Converters_WhenPayloadMalformed_ShouldThrowJsonException()
    {
        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<ExchangeRate>(
            "{\"pair\":123,\"date\":\"2024-01-15\",\"rate\":1,\"provider\":\"x\"}"));
        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<ExchangeRatePair>("{\"from\":\"USD\",\"to\":\"JPY\",\"to\":\"GBP\"}"));
        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<MoneyBag>("{\"balances\":123}"));
    }

    /// <summary>
    /// Verifies that the exchange-rate converter rejects a numeric property supplied as an unparseable string and a
    /// date property supplied as a non-string token.
    /// </summary>
    [TestMethod]
    [DataRow("{\"from\":\"USD\",\"to\":\"JPY\",\"date\":\"2024-01-15\",\"rate\":\"nope\",\"provider\":\"x\"}", DisplayName = "Rate string is not numeric")]
    [DataRow("{\"from\":\"USD\",\"to\":\"JPY\",\"date\":123,\"rate\":1,\"provider\":\"x\"}", DisplayName = "Date is not a string")]
    public void ExchangeRate_WhenValueTokenIsWrong_ShouldThrowJsonException(string json) =>
        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<ExchangeRate>(json));

    /// <summary>
    /// Verifies that a compact pair whose slash-separated codes are not valid ISO codes is rejected with a
    /// <see cref="JsonException" /> wrapping the constructor's argument error.
    /// </summary>
    [TestMethod]
    public void ExchangeRatePair_WhenCompactCodesInvalid_ShouldThrowJsonException()
    {
        var options = new JsonSerializerOptions().AddFinancialJsonConverters(FinancialJsonPolicy.Compact);

        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<ExchangeRatePair>("\"XX/YY\"", options));
    }
}
