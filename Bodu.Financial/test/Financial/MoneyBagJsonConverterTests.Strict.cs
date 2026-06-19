// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyBagJsonConverterTests.Strict.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;
using System.Text.Json;
using Bodu.Financial.Currencies;
using Bodu.Financial.Serialization;

namespace Bodu.Financial;

public partial class MoneyBagJsonConverterTests
{

    /// <summary>
    /// Verifies that the default Strict policy round-trips a bag through the wrapped <c>"balances"</c> object.
    /// </summary>
    [TestMethod]
    public void Strict_WhenRoundTripped_ShouldPreserveBalances()
    {
        string json = JsonSerializer.Serialize(SampleBag());
        MoneyBag restored = JsonSerializer.Deserialize<MoneyBag>(json)!;

        StringAssert.Contains(json, "balances");
        CollectionAssert.AreEquivalent(SampleBag().ToArray(), restored.ToArray());
    }

    /// <summary>
    /// Verifies that an empty bag round-trips to an empty bag.
    /// </summary>
    [TestMethod]
    public void Strict_WhenEmpty_ShouldRoundTripToEmpty()
    {
        string json = JsonSerializer.Serialize(new MoneyBag());
        MoneyBag restored = JsonSerializer.Deserialize<MoneyBag>(json)!;

        Assert.AreEqual(0, restored.Count);
    }

    /// <summary>
    /// Verifies that a balance supplied as a numeric string is accepted.
    /// </summary>
    [TestMethod]
    public void Strict_WhenBalanceIsNumericString_ShouldParse()
    {
        MoneyBag restored = JsonSerializer.Deserialize<MoneyBag>("{\"balances\":{\"USD\":\"10.50\"}}")!;

        Assert.AreEqual(new Money(10.50m, CurrencyCode.USD), restored.Single());
    }

    /// <summary>
    /// Verifies that malformed payloads — a non-object root or a non-numeric balance — are rejected with a
    /// <see cref="JsonException" />.
    /// </summary>
    [TestMethod]
    [DataRow("[1,2]", DisplayName = "Root is not an object")]
    [DataRow("{\"balances\":{\"USD\":true}}", DisplayName = "Balance is boolean")]
    [DataRow("{\"balances\":{\"USD\":\"x\"}}", DisplayName = "Balance is non-numeric string")]
    public void Strict_WhenPayloadMalformed_ShouldThrowJsonException(string json) =>
        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<MoneyBag>(json));
}
