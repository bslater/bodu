// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyJsonConverterTests.Strict.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.Serialization.Json;

public partial class MoneyJsonConverterTests
{

    /// <summary>
    /// Verifies that the default Strict policy round-trips a value through the canonical object shape.
    /// </summary>
    [TestMethod]
    public void Strict_WhenRoundTripped_ShouldPreserveValue()
    {
        string json = JsonSerializer.Serialize(new Money(19.99m, CurrencyCode.USD), OptionsFor(FinancialJsonPolicy.Strict));

        Assert.AreEqual("{\"amount\":19.99,\"currency\":\"USD\"}", json);
        Assert.AreEqual(new Money(19.99m, CurrencyCode.USD), JsonSerializer.Deserialize<Money>(json, OptionsFor(FinancialJsonPolicy.Strict)));
    }

    /// <summary>
    /// Verifies that a numeric string amount is accepted and parsed under the invariant culture.
    /// </summary>
    [TestMethod]
    public void Strict_WhenAmountIsNumericString_ShouldParse() =>
        Assert.AreEqual(new Money(19.99m, CurrencyCode.USD), JsonSerializer.Deserialize<Money>("{\"amount\":\"19.99\",\"currency\":\"USD\"}", OptionsFor(FinancialJsonPolicy.Strict)));

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
        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<Money>(json, OptionsFor(FinancialJsonPolicy.Strict)));

    /// <summary>
    /// Verifies that a JSON payload with duplicate <c>"amount"</c> or <c>"currency"</c> properties is rejected —
    /// last-write-wins on financial payloads is a silent data-integrity hazard.
    /// </summary>
    [TestMethod]
    [DataRow("{\"amount\":10,\"amount\":20,\"currency\":\"USD\"}", DisplayName = "Duplicate amount")]
    [DataRow("{\"amount\":10,\"currency\":\"USD\",\"currency\":\"EUR\"}", DisplayName = "Duplicate currency")]
    public void Strict_WhenPropertyDuplicated_ShouldThrowJsonException(string json) =>
        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<Money>(json, OptionsFor(FinancialJsonPolicy.Strict)));
}
