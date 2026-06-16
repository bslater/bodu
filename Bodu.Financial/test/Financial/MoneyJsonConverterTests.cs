// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyJsonConverterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Serialization;

namespace Bodu.Financial;

/// <summary>
/// Verifies <see cref="MoneyJsonConverter" /> across the Strict, Lenient, and Compact policies, including the
/// malformed-payload rejection paths.
/// </summary>
[TestClass]
public class MoneyJsonConverterTests
{
    private static JsonSerializerOptions OptionsFor(FinancialJsonPolicy policy) =>
        new JsonSerializerOptions().AddFinancialJsonConverters(policy);

    /// <summary>
    /// Verifies that the default Strict policy round-trips a value through the canonical object shape.
    /// </summary>
    [TestMethod]
    public void Strict_WhenRoundTripped_ShouldPreserveValue()
    {
        string json = JsonSerializer.Serialize(new Money(19.99m, "USD"));

        Assert.AreEqual("{\"amount\":19.99,\"currency\":\"USD\"}", json);
        Assert.AreEqual(new Money(19.99m, "USD"), JsonSerializer.Deserialize<Money>(json));
    }

    /// <summary>
    /// Verifies that a numeric string amount is accepted and parsed under the invariant culture.
    /// </summary>
    [TestMethod]
    public void Strict_WhenAmountIsNumericString_ShouldParse() =>
        Assert.AreEqual(new Money(19.99m, "USD"), JsonSerializer.Deserialize<Money>("{\"amount\":\"19.99\",\"currency\":\"USD\"}"));

    /// <summary>
    /// Verifies that the Lenient policy trims and upper-cases the currency code.
    /// </summary>
    [TestMethod]
    public void Lenient_WhenCurrencyLowercaseWithWhitespace_ShouldNormalize()
    {
        Money money = JsonSerializer.Deserialize<Money>("{\"amount\":1,\"currency\":\" usd \"}", OptionsFor(FinancialJsonPolicy.Lenient));

        Assert.AreEqual("USD", money.IsoCode);
    }

    /// <summary>
    /// Verifies that the Compact policy round-trips through the <c>"amount ISO"</c> string form.
    /// </summary>
    [TestMethod]
    public void Compact_WhenRoundTripped_ShouldPreserveValue()
    {
        JsonSerializerOptions options = OptionsFor(FinancialJsonPolicy.Compact);

        string json = JsonSerializer.Serialize(new Money(19.99m, "USD"), options);

        Assert.AreEqual("\"19.99 USD\"", json);
        Assert.AreEqual(new Money(19.99m, "USD"), JsonSerializer.Deserialize<Money>(json, options));
    }

    /// <summary>
    /// Verifies that the Compact policy rejects a non-string token and an unparseable string.
    /// </summary>
    [TestMethod]
    [DataRow("123", DisplayName = "Compact token is a number")]
    [DataRow("\"not money\"", DisplayName = "Compact string is not money")]
    public void Compact_WhenPayloadInvalid_ShouldThrowJsonException(string json) =>
        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<Money>(json, OptionsFor(FinancialJsonPolicy.Compact)));

    /// <summary>
    /// Verifies that the canonical object form rejects malformed payloads — a non-object, a non-numeric amount, a
    /// non-string currency, a missing property, or a wrong-length ISO code — with a <see cref="JsonException" />.
    /// </summary>
    [TestMethod]
    [DataRow("[1,2]", DisplayName = "Not an object")]
    [DataRow("{\"amount\":true,\"currency\":\"USD\"}", DisplayName = "Amount is boolean")]
    [DataRow("{\"amount\":\"x\",\"currency\":\"USD\"}", DisplayName = "Amount is non-numeric string")]
    [DataRow("{\"amount\":1,\"currency\":123}", DisplayName = "Currency is a number")]
    [DataRow("{\"currency\":\"USD\"}", DisplayName = "Missing amount")]
    [DataRow("{\"amount\":1}", DisplayName = "Missing currency")]
    [DataRow("{\"amount\":1,\"currency\":\"US\"}", DisplayName = "Currency wrong length")]
    public void Strict_WhenObjectPayloadMalformed_ShouldThrowJsonException(string json) =>
        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<Money>(json));
}
