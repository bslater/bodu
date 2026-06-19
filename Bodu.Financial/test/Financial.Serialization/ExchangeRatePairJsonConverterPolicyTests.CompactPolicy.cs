// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRatePairJsonConverterPolicyTests.CompactPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.Serialization;

public partial class ExchangeRatePairJsonConverterPolicyTests
{

    /// <summary>
    /// Verifies that the compact policy emits the slash-separated string form.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenSerializing_ShouldEmitFromSlashToString()
    {
        var pair = new ExchangeRatePair(CurrencyCode.USD, CurrencyCode.JPY);

        string json = JsonSerializer.Serialize(pair, Options(FinancialJsonPolicy.Compact));

        Assert.AreEqual("\"USD/JPY\"", json);
    }

    /// <summary>
    /// Verifies that compact round-trip preserves the pair.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenRoundTripping_ShouldPreserveValue()
    {
        var pair = new ExchangeRatePair(CurrencyCode.EUR, CurrencyCode.GBP);
        JsonSerializerOptions options = Options(FinancialJsonPolicy.Compact);

        string json = JsonSerializer.Serialize(pair, options);
        ExchangeRatePair recovered = JsonSerializer.Deserialize<ExchangeRatePair>(json, options);

        Assert.AreEqual(pair, recovered);
    }

    /// <summary>
    /// Verifies that compact reads reject the canonical object form.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenReadingObjectForm_ShouldThrowJsonException()
    {
        string json = "{\"from\":\"USD\",\"to\":\"JPY\"}";

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
        string json = "\"USDJPY\"";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<ExchangeRatePair>(json, Options(FinancialJsonPolicy.Compact));
        });
    }
}
