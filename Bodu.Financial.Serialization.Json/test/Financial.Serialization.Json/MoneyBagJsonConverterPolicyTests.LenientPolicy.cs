// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyBagJsonConverterPolicyTests.LenientPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.Serialization.Json;

public partial class MoneyBagJsonConverterPolicyTests
{
    /// <summary>
    /// Verifies that the lenient policy trims and upper-cases an ISO code key before resolving the currency.
    /// </summary>
    /// <param name="key">The ISO code key as written in the payload.</param>
    [TestMethod]
    [DataRow("usd")]
    [DataRow(" USD")]
    [DataRow("Usd ")]
    public void Read_WhenLenientPolicyAndIsoCodeIsLowercaseOrPadded_ShouldNormalizeCode(string key)
    {
        string json = "{\"balances\":{\"" + key + "\":1.5}}";

        MoneyBag bag = JsonSerializer.Deserialize<MoneyBag>(json, Options(FinancialJsonPolicy.Lenient))!;

        Assert.AreEqual(MoneyBag.Empty.Add(new Money(1.5m, CurrencyCode.USD)), bag);
    }

    /// <summary>
    /// Verifies that the lenient policy, which shares the strict shape, rejects a balances map that repeats an ISO
    /// code with <see cref="JsonException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenLenientPolicyAndIsoCodeRepeated_ShouldThrowJsonException()
    {
        string json = "{\"balances\":{\"USD\":1,\"USD\":2}}";

        _ = Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<MoneyBag>(json, Options(FinancialJsonPolicy.Lenient));
        });
    }

    /// <summary>
    /// Verifies that the lenient policy rejects two ISO code keys that name the same currency once normalized with
    /// <see cref="JsonException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenLenientPolicyAndCodesCollideAfterNormalization_ShouldThrowJsonException()
    {
        string json = "{\"balances\":{\"usd\":1,\" USD\":2}}";

        _ = Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<MoneyBag>(json, Options(FinancialJsonPolicy.Lenient));
        });
    }
}
