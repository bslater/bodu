// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AttributeBoundJsonContractTests.MoneyOfTCurrency.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.Serialization;

public partial class AttributeBoundJsonContractTests
{

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
}
