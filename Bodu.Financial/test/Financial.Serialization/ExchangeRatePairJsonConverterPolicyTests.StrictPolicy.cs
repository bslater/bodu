// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRatePairJsonConverterPolicyTests.StrictPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.Serialization;

public partial class ExchangeRatePairJsonConverterPolicyTests
{

    /// <summary>
    /// Verifies that the canonical object form round-trips under <see cref="FinancialJsonPolicy.Strict" />.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenRoundTripping_ShouldPreserveValue()
    {
        var pair = new ExchangeRatePair(CurrencyCode.USD, CurrencyCode.JPY);
        JsonSerializerOptions options = Options(FinancialJsonPolicy.Strict);

        string json = JsonSerializer.Serialize(pair, options);
        ExchangeRatePair recovered = JsonSerializer.Deserialize<ExchangeRatePair>(json, options);

        Assert.AreEqual(pair, recovered);
    }

    /// <summary>
    /// Verifies that the strict policy rejects lowercase ISO codes during object-form read.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenReadingLowercaseCurrency_ShouldThrowJsonException()
    {
        string json = "{\"from\":\"usd\",\"to\":\"jpy\"}";

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
        string json = "{\"from\":\"USD\",\"from\":\"EUR\",\"to\":\"JPY\"}";

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
        string json = "{\"to\":\"JPY\"}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<ExchangeRatePair>(json, Options(FinancialJsonPolicy.Strict));
        });
    }
}
