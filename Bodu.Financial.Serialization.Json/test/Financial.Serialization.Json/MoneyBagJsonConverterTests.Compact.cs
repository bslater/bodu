// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyBagJsonConverterTests.Compact.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;

namespace Bodu.Financial.Serialization.Json;

public partial class MoneyBagJsonConverterTests
{

    /// <summary>
    /// Verifies that the Compact policy round-trips a bag through a flat ISO-to-amount map.
    /// </summary>
    [TestMethod]
    public void Compact_WhenRoundTripped_ShouldPreserveBalances()
    {
        JsonSerializerOptions options = OptionsFor(FinancialJsonPolicy.Compact);

        string json = JsonSerializer.Serialize(SampleBag(), options);
        MoneyBag restored = JsonSerializer.Deserialize<MoneyBag>(json, options)!;

        Assert.IsFalse(json.Contains("balances", StringComparison.Ordinal));
        CollectionAssert.AreEquivalent(SampleBag().ToArray(), restored.ToArray());
    }
}
