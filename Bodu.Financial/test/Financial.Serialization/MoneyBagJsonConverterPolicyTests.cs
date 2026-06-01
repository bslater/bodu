// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyBagJsonConverterPolicyTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;

namespace Bodu.Financial.Serialization;

/// <summary>
/// Verifies that <see cref="MoneyBagJsonConverter" /> honours each <see cref="FinancialJsonPolicy" />.
/// </summary>
[TestClass]
public class MoneyBagJsonConverterPolicyTests
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
    /// Verifies that the compact policy emits a flat ISO-to-amount map with no <c>"balances"</c> wrapper.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenSerializing_ShouldEmitFlatMapWithoutBalancesWrapper()
    {
        MoneyBag bag = MoneyBag.Empty
            .Add(new Money(12.34m, "AUD"))
            .Add(new Money(56.78m, "USD"));

        var json = JsonSerializer.Serialize(bag, Options(FinancialJsonPolicy.Compact));

        Assert.AreEqual("{\"AUD\":12.34,\"USD\":56.78}", json);
    }

    /// <summary>
    /// Verifies that the compact policy reads the flat ISO-to-amount map.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenReading_ShouldSucceed()
    {
        var json = "{\"AUD\":12.34,\"USD\":56.78}";

        MoneyBag bag = JsonSerializer.Deserialize<MoneyBag>(json, Options(FinancialJsonPolicy.Compact))!;

        Assert.AreEqual(2, bag.Count);
        Assert.AreEqual(12.34m, bag.GetBalance("AUD")!.Value.Amount);
        Assert.AreEqual(56.78m, bag.GetBalance("USD")!.Value.Amount);
    }

    /// <summary>
    /// Verifies that the compact policy round-trips through write and read.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenRoundTripping_ShouldPreserveBalances()
    {
        MoneyBag original = MoneyBag.Empty
            .Add(new Money(100m, "EUR"))
            .Add(new Money(50m, "GBP"));
        JsonSerializerOptions options = Options(FinancialJsonPolicy.Compact);

        var json = JsonSerializer.Serialize(original, options);
        MoneyBag recovered = JsonSerializer.Deserialize<MoneyBag>(json, options)!;

        Assert.AreEqual(original, recovered);
    }

    /// <summary>
    /// Verifies that the strict policy still emits the canonical wrapped form.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenSerializing_ShouldEmitBalancesWrappedForm()
    {
        MoneyBag bag = MoneyBag.Empty.Add(new Money(12.34m, "AUD"));

        var json = JsonSerializer.Serialize(bag, Options(FinancialJsonPolicy.Strict));

        Assert.AreEqual("{\"balances\":{\"AUD\":12.34}}", json);
    }
}
