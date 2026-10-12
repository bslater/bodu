// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyBagJsonConverterPolicyTests.StrictPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.Serialization.Json;

public partial class MoneyBagJsonConverterPolicyTests
{

    /// <summary>
    /// Verifies that the strict policy still emits the canonical wrapped form.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenSerializing_ShouldEmitBalancesWrappedForm()
    {
        MoneyBag bag = MoneyBag.Empty.Add(new Money(12.34m, CurrencyCode.AUD));

        string json = JsonSerializer.Serialize(bag, Options(FinancialJsonPolicy.Strict));

        Assert.AreEqual("{\"balances\":{\"AUD\":12.34}}", json);
    }

    /// <summary>
    /// Verifies that the strict policy rejects a balances map that repeats an ISO code with
    /// <see cref="JsonException" /> rather than summing the repeated balances.
    /// </summary>
    [TestMethod]
    public void Read_WhenStrictPolicyAndIsoCodeRepeated_ShouldThrowJsonException()
    {
        string json = "{\"balances\":{\"USD\":1,\"USD\":2}}";

        _ = Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<MoneyBag>(json, Options(FinancialJsonPolicy.Strict));
        });
    }

    /// <summary>
    /// Verifies that the strict policy rejects a repeated <c>"balances"</c> property with <see cref="JsonException" />
    /// rather than merging the two maps.
    /// </summary>
    [TestMethod]
    public void Read_WhenStrictPolicyAndBalancesPropertyRepeated_ShouldThrowJsonException()
    {
        string json = "{\"balances\":{\"USD\":1},\"Balances\":{\"EUR\":2}}";

        _ = Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<MoneyBag>(json, Options(FinancialJsonPolicy.Strict));
        });
    }
}
