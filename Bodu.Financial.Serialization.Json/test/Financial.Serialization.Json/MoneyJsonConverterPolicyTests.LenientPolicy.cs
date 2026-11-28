// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyJsonConverterPolicyTests.LenientPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.Serialization.Json;

public partial class MoneyJsonConverterPolicyTests
{

    /// <summary>
    /// Verifies that the lenient policy accepts lowercase currency in the object form and normalizes it.
    /// </summary>
    [TestMethod]
    public void LenientPolicy_WhenReadingLowercaseCurrency_ShouldSucceed()
    {
        string json = "{\"amount\":19.99,\"currency\":\"usd\"}";

        Money result = JsonSerializer.Deserialize<Money>(json, Options(FinancialJsonPolicy.Lenient));

        Assert.AreEqual(new Money(19.99m, CurrencyCode.USD), result);
    }

    /// <summary>
    /// Verifies that the lenient policy rejects a currency code written with U+017F LATIN SMALL LETTER LONG S, which
    /// invariant uppercasing maps to <c>S</c> but which is not a lowercase ISO letter, with <see cref="JsonException" />.
    /// </summary>
    [TestMethod]
    public void LenientPolicy_WhenCurrencyHasLongS_ShouldThrowJsonException()
    {
        string json = "{\"amount\":19.99,\"currency\":\"uſd\"}";

        _ = Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Money>(json, Options(FinancialJsonPolicy.Lenient));
        });
    }
}
