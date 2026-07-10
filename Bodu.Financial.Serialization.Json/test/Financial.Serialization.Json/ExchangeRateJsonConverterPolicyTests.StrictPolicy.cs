// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateJsonConverterPolicyTests.StrictPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.ExchangeRates;

namespace Bodu.Financial.Serialization.Json;

public partial class ExchangeRateJsonConverterPolicyTests
{

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
