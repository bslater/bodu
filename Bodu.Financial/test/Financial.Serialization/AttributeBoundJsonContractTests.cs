// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AttributeBoundJsonContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.Serialization;

/// <summary>
/// Verifies the frozen serialization contract of the type-level <c>[JsonConverter]</c> attributes: serializing without
/// any registered options always produces the canonical <see cref="FinancialJsonPolicy.Strict" /> object shape, never
/// the compact or lenient variants.
/// </summary>
[TestClass]
public class AttributeBoundJsonContractTests
{
    /// <summary>
    /// Verifies that serializing a <see cref="Money" /> through the attribute (no options) emits the Strict object
    /// shape and round-trips.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Money_WhenSerializedViaAttribute_ShouldEmitStrictObjectShapeAndRoundTrip()
    {
        var value = new Money(19.99m, "USD");

        string json = JsonSerializer.Serialize(value);

        Assert.AreEqual("{\"amount\":19.99,\"currency\":\"USD\"}", json);
        Assert.AreEqual(value, JsonSerializer.Deserialize<Money>(json));
    }

    /// <summary>
    /// Verifies that serializing a <see cref="Money{TCurrency}" /> through the attribute (no options) emits the Strict
    /// object shape and round-trips.
    /// </summary>
    [TestMethod]
    public void MoneyOfTCurrency_WhenSerializedViaAttribute_ShouldEmitStrictObjectShapeAndRoundTrip()
    {
        var value = new Money<USD>(19.99m);

        string json = JsonSerializer.Serialize(value);

        Assert.AreEqual("{\"amount\":19.99,\"currency\":\"USD\"}", json);
        Assert.AreEqual(value, JsonSerializer.Deserialize<Money<USD>>(json));
    }

    /// <summary>
    /// Verifies that serializing a <see cref="MoneyBag" /> through the attribute (no options) emits the Strict
    /// <c>balances</c> object shape and round-trips.
    /// </summary>
    [TestMethod]
    public void MoneyBag_WhenSerializedViaAttribute_ShouldEmitStrictBalancesShapeAndRoundTrip()
    {
        MoneyBag bag = MoneyBag.Empty
            .Add(new Money(10m, "USD"))
            .Add(new Money(5m, "EUR"));

        string json = JsonSerializer.Serialize(bag);

        Assert.AreEqual("{\"balances\":{\"EUR\":5,\"USD\":10}}", json);
        Assert.AreEqual(bag, JsonSerializer.Deserialize<MoneyBag>(json));
    }

    /// <summary>
    /// Verifies that the attribute path never emits the compact string form for <see cref="Money" /> (which the compact
    /// policy would otherwise produce).
    /// </summary>
    [TestMethod]
    public void Money_WhenSerializedViaAttribute_ShouldNotEmitCompactString()
    {
        string json = JsonSerializer.Serialize(new Money(19.99m, "USD"));

        Assert.IsTrue(json.StartsWith("{", StringComparison.Ordinal), $"Expected Strict object shape, got '{json}'.");
    }
}
