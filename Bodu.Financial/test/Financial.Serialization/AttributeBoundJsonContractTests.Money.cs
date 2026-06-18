// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AttributeBoundJsonContractTests.Money.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.Serialization;

public partial class AttributeBoundJsonContractTests
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
