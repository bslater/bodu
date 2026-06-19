// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyBagJsonConverterPolicyTests.CompactPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.Serialization;

public partial class MoneyBagJsonConverterPolicyTests
{

    /// <summary>
    /// Verifies that the compact policy emits a flat ISO-to-amount map with no <c>"balances"</c> wrapper.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenSerializing_ShouldEmitFlatMapWithoutBalancesWrapper()
    {
        MoneyBag bag = MoneyBag.Empty
            .Add(new Money(12.34m, CurrencyCode.AUD))
            .Add(new Money(56.78m, CurrencyCode.USD));

        string json = JsonSerializer.Serialize(bag, Options(FinancialJsonPolicy.Compact));

        Assert.AreEqual("{\"AUD\":12.34,\"USD\":56.78}", json);
    }

    /// <summary>
    /// Verifies that the compact policy reads the flat ISO-to-amount map.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenReading_ShouldSucceed()
    {
        string json = "{\"AUD\":12.34,\"USD\":56.78}";

        MoneyBag bag = JsonSerializer.Deserialize<MoneyBag>(json, Options(FinancialJsonPolicy.Compact))!;

        Assert.AreEqual(2, bag.Count);
        Assert.AreEqual(12.34m, bag.GetBalance(CurrencyCode.AUD)!.Value.Amount);
        Assert.AreEqual(56.78m, bag.GetBalance(CurrencyCode.USD)!.Value.Amount);
    }

    /// <summary>
    /// Verifies that the compact policy round-trips through write and read.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenRoundTripping_ShouldPreserveBalances()
    {
        MoneyBag original = MoneyBag.Empty
            .Add(new Money(100m, CurrencyCode.EUR))
            .Add(new Money(50m, CurrencyCode.GBP));
        JsonSerializerOptions options = Options(FinancialJsonPolicy.Compact);

        string json = JsonSerializer.Serialize(original, options);
        MoneyBag recovered = JsonSerializer.Deserialize<MoneyBag>(json, options)!;

        Assert.AreEqual(original, recovered);
    }
}
