// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateJsonConverterPolicyTests.CompactPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;

namespace Bodu.Financial.Serialization;

public partial class ExchangeRateJsonConverterPolicyTests
{

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
}
