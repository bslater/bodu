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
    /// A representative fetch instant used by the provenance round-trip cases.
    /// </summary>
    private static readonly DateTimeOffset s_fetchedAt = new(2024, 5, 30, 14, 15, 16, TimeSpan.Zero);

    /// <summary>
    /// Builds a representative exchange-rate observation.
    /// </summary>
    private static ExchangeRate SampleRate(bool isInverted = false) =>
        new("USD", "JPY", new DateOnly(2024, 5, 30), 156.42m, "ECB", isInverted);

    /// <summary>
    /// Builds a representative exchange-rate observation carrying a non-null fetch instant.
    /// </summary>
    private static ExchangeRate SampleRateWithFetch(bool isInverted = false) =>
        new("USD", "JPY", new DateOnly(2024, 5, 30), 156.42m, "ECB", isInverted, s_fetchedAt);

    /// <summary>
    /// Verifies that the attribute-driven default emits the canonical object form, with all six fields in
    /// declaration order.
    /// </summary>
    [TestMethod]
    public void DefaultAttribute_WhenSerializing_ShouldEmitCanonicalObjectShape()
    {
        string json = JsonSerializer.Serialize(SampleRate());

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

        string json = JsonSerializer.Serialize(original, options);
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
        string json = JsonSerializer.Serialize(SampleRate(), Options(FinancialJsonPolicy.Compact));

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
        string json = JsonSerializer.Serialize(SampleRate(isInverted: true), Options(FinancialJsonPolicy.Compact));

        // An inverted rate also carries the originally observed rate so the precise divisor survives a round-trip;
        // its serialized form matches how the writer renders the decimal.
        string observedRate = JsonSerializer.Serialize(1m / 156.42m);
        Assert.AreEqual(
            "{\"pair\":\"USD/JPY\",\"date\":\"2024-05-30\",\"rate\":156.42,\"provider\":\"ECB\",\"isInverted\":true,\"observedRate\":" + observedRate + "}",
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

        string json = JsonSerializer.Serialize(original, options);
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
        string json = "{\"pair\":\"USD/JPY\",\"date\":\"2024-05-30\",\"rate\":156.42,\"provider\":\"ECB\"}";

        ExchangeRate result = JsonSerializer.Deserialize<ExchangeRate>(json, Options(FinancialJsonPolicy.Strict));

        Assert.AreEqual(SampleRate(), result);
    }

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

    /// <summary>
    /// Verifies that the lenient policy normalises lowercase ISO codes on read.
    /// </summary>
    [TestMethod]
    public void LenientPolicy_WhenReadingLowercaseCurrency_ShouldSucceed()
    {
        string json = "{\"from\":\"usd\",\"to\":\"jpy\",\"date\":\"2024-05-30\",\"rate\":156.42,\"provider\":\"ECB\",\"isInverted\":false}";

        ExchangeRate result = JsonSerializer.Deserialize<ExchangeRate>(json, Options(FinancialJsonPolicy.Lenient));

        Assert.AreEqual(SampleRate(), result);
    }

    /// <summary>
    /// Verifies that the strict policy rejects lowercase ISO codes on read.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenReadingLowercaseCurrency_ShouldThrowJsonException()
    {
        string json = "{\"from\":\"usd\",\"to\":\"jpy\",\"date\":\"2024-05-30\",\"rate\":156.42,\"provider\":\"ECB\",\"isInverted\":false}";

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
        string json = "{\"from\":\"USD\",\"to\":\"JPY\",\"date\":\"2024-05-30\",\"rate\":1.0,\"rate\":2.0,\"provider\":\"ECB\",\"isInverted\":false}";

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
        string json = "{\"from\":\"USD\",\"to\":\"JPY\",\"date\":\"2024-05-30\",\"rate\":0,\"provider\":\"ECB\",\"isInverted\":false}";

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
        string json = "{\"from\":\"USD\",\"to\":\"JPY\",\"rate\":156.42,\"provider\":\"ECB\",\"isInverted\":false}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<ExchangeRate>(json, Options(FinancialJsonPolicy.Strict));
        });
    }

    /// <summary>
    /// Verifies that the strict policy round-trips a rate carrying a non-null fetch instant, preserving the instant.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenRoundTrippingWithFetchedAtUtc_ShouldPreserveInstant()
    {
        ExchangeRate original = SampleRateWithFetch();
        JsonSerializerOptions options = Options(FinancialJsonPolicy.Strict);

        string json = JsonSerializer.Serialize(original, options);
        ExchangeRate recovered = JsonSerializer.Deserialize<ExchangeRate>(json, options);

        Assert.AreEqual(s_fetchedAt, recovered.FetchedAtUtc);
    }

    /// <summary>
    /// Verifies that the compact policy round-trips a rate carrying a non-null fetch instant, preserving the instant.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenRoundTrippingWithFetchedAtUtc_ShouldPreserveInstant()
    {
        ExchangeRate original = SampleRateWithFetch();
        JsonSerializerOptions options = Options(FinancialJsonPolicy.Compact);

        string json = JsonSerializer.Serialize(original, options);
        ExchangeRate recovered = JsonSerializer.Deserialize<ExchangeRate>(json, options);

        Assert.AreEqual(s_fetchedAt, recovered.FetchedAtUtc);
    }

    /// <summary>
    /// Verifies that reading a pre-provenance blob with no <c>fetchedAtUtc</c> property yields a <see langword="null" />
    /// fetch instant.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenReadingBlobWithoutFetchedAtUtc_ShouldYieldNullInstant()
    {
        string json = "{\"from\":\"USD\",\"to\":\"JPY\",\"date\":\"2024-05-30\",\"rate\":156.42,\"provider\":\"ECB\",\"isInverted\":false}";

        ExchangeRate result = JsonSerializer.Deserialize<ExchangeRate>(json, Options(FinancialJsonPolicy.Strict));

        Assert.IsNull(result.FetchedAtUtc);
    }

    /// <summary>
    /// Verifies that writing a rate whose fetch instant is <see langword="null" /> omits the <c>fetchedAtUtc</c>
    /// property, keeping the canonical byte shape unchanged.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenWritingNullFetchedAtUtc_ShouldOmitProperty()
    {
        string json = JsonSerializer.Serialize(SampleRate(), Options(FinancialJsonPolicy.Strict));

        Assert.AreEqual(
            "{\"from\":\"USD\",\"to\":\"JPY\",\"date\":\"2024-05-30\",\"rate\":156.42,\"provider\":\"ECB\",\"isInverted\":false}",
            json);
    }

    /// <summary>
    /// Verifies that the compact writer omits the <c>fetchedAtUtc</c> property when the fetch instant is
    /// <see langword="null" />, keeping the compact byte shape unchanged.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenWritingNullFetchedAtUtc_ShouldOmitProperty()
    {
        string json = JsonSerializer.Serialize(SampleRate(), Options(FinancialJsonPolicy.Compact));

        Assert.AreEqual(
            "{\"pair\":\"USD/JPY\",\"date\":\"2024-05-30\",\"rate\":156.42,\"provider\":\"ECB\"}",
            json);
    }

    /// <summary>
    /// Verifies that the canonical writer appends <c>fetchedAtUtc</c> as the final property when the fetch instant is
    /// present.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenWritingFetchedAtUtc_ShouldEmitInstantAsFinalProperty()
    {
        string json = JsonSerializer.Serialize(SampleRateWithFetch(), Options(FinancialJsonPolicy.Strict));

        // The instant is written via the round-trippable "O" format; serialize the same string so the comparison
        // accounts for how the JSON encoder escapes the offset's '+' sign.
        string fetched = JsonSerializer.Serialize(s_fetchedAt.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
        Assert.AreEqual(
            "{\"from\":\"USD\",\"to\":\"JPY\",\"date\":\"2024-05-30\",\"rate\":156.42,\"provider\":\"ECB\",\"isInverted\":false,\"fetchedAtUtc\":" + fetched + "}",
            json);
    }
}
