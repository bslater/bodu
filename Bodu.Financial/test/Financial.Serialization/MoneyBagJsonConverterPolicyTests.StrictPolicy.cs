// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyBagJsonConverterPolicyTests.StrictPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;

namespace Bodu.Financial.Serialization;

public partial class MoneyBagJsonConverterPolicyTests
{

    /// <summary>
    /// Verifies that the strict policy still emits the canonical wrapped form.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenSerializing_ShouldEmitBalancesWrappedForm()
    {
        MoneyBag bag = MoneyBag.Empty.Add(new Money(12.34m, "AUD"));

        string json = JsonSerializer.Serialize(bag, Options(FinancialJsonPolicy.Strict));

        Assert.AreEqual("{\"balances\":{\"AUD\":12.34}}", json);
    }
}
