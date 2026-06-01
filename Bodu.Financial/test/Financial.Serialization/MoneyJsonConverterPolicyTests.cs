// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyJsonConverterPolicyTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;

namespace Bodu.Financial.Serialization;

/// <summary>
/// Verifies that <see cref="MoneyJsonConverter" /> honours each <see cref="FinancialJsonPolicy" />.
/// </summary>
[TestClass]
public class MoneyJsonConverterPolicyTests
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
    /// Verifies that the compact policy emits a single JSON string of the form <c>"&lt;amount&gt; &lt;ISO&gt;"</c>.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenSerializing_ShouldEmitAmountSpaceIsoString()
    {
        var value = new Money(19.99m, "USD");

        var json = JsonSerializer.Serialize(value, Options(FinancialJsonPolicy.Compact));

        Assert.AreEqual("\"19.99 USD\"", json);
    }

    /// <summary>
    /// Verifies that the compact policy round-trips through write and read.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenRoundTripping_ShouldPreserveValue()
    {
        var original = new Money(1234.56m, "USD");
        JsonSerializerOptions options = Options(FinancialJsonPolicy.Compact);

        var json = JsonSerializer.Serialize(original, options);
        Money recovered = JsonSerializer.Deserialize<Money>(json, options);

        Assert.AreEqual(original, recovered);
    }

    /// <summary>
    /// Verifies that compact reads accept the ISO-prefix arrangement.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenReadingIsoPrefix_ShouldSucceed()
    {
        var json = "\"USD 19.99\"";

        Money result = JsonSerializer.Deserialize<Money>(json, Options(FinancialJsonPolicy.Compact));

        Assert.AreEqual(new Money(19.99m, "USD"), result);
    }

    /// <summary>
    /// Verifies that compact reads reject the canonical object form (which Strict / Lenient handle).
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenReadingObjectForm_ShouldThrowJsonException()
    {
        var json = "{\"amount\":19.99,\"currency\":\"USD\"}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Money>(json, Options(FinancialJsonPolicy.Compact));
        });
    }

    /// <summary>
    /// Verifies that the lenient policy accepts lowercase currency in the object form and normalises it.
    /// </summary>
    [TestMethod]
    public void LenientPolicy_WhenReadingLowercaseCurrency_ShouldSucceed()
    {
        var json = "{\"amount\":19.99,\"currency\":\"usd\"}";

        Money result = JsonSerializer.Deserialize<Money>(json, Options(FinancialJsonPolicy.Lenient));

        Assert.AreEqual(new Money(19.99m, "USD"), result);
    }

    /// <summary>
    /// Verifies that the strict policy rejects lowercase currency on read.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenReadingLowercaseCurrency_ShouldThrowJsonException()
    {
        var json = "{\"amount\":19.99,\"currency\":\"usd\"}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Money>(json, Options(FinancialJsonPolicy.Strict));
        });
    }
}
