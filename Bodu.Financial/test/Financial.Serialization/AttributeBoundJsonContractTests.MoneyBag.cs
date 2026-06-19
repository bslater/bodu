// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AttributeBoundJsonContractTests.MoneyBag.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.Serialization;

public partial class AttributeBoundJsonContractTests
{

    /// <summary>
    /// Verifies that serializing a <see cref="MoneyBag" /> through the attribute (no options) emits the Strict
    /// <c>balances</c> object shape and round-trips.
    /// </summary>
    [TestMethod]
    public void MoneyBag_WhenSerializedViaAttribute_ShouldEmitStrictBalancesShapeAndRoundTrip()
    {
        MoneyBag bag = MoneyBag.Empty
            .Add(new Money(10m, CurrencyCode.USD))
            .Add(new Money(5m, CurrencyCode.EUR));

        string json = JsonSerializer.Serialize(bag);

        Assert.AreEqual("{\"balances\":{\"EUR\":5,\"USD\":10}}", json);
        Assert.AreEqual(bag, JsonSerializer.Deserialize<MoneyBag>(json));
    }
}
