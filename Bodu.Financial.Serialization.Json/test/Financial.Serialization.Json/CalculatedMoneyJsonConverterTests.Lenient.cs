// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalculatedMoneyJsonConverterTests.Lenient.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.Serialization.Json;

public partial class CalculatedMoneyJsonConverterTests
{
    /// <summary>
    /// Verifies that the lenient policy upper-cases a lowercase currency code and trims the whitespace around it before
    /// it resolves the currency.
    /// </summary>
    [TestMethod]
    public void Lenient_WhenCurrencyIsLowercaseAndPadded_ShouldNormalizeCode()
    {
        string json = "{\"amount\":12.3456789,\"currency\":\" usd \"}";

        CalculatedMoney result = JsonSerializer.Deserialize<CalculatedMoney>(json, OptionsFor(FinancialJsonPolicy.Lenient));

        Assert.AreEqual(new CalculatedMoney(12.3456789m, CurrencyCode.USD), result);
    }

    /// <summary>
    /// Verifies that the lenient policy rejects a currency code written with U+017F LATIN SMALL LETTER LONG S, which
    /// invariant uppercasing maps to <c>S</c> but which is not a lowercase ISO letter, with <see cref="JsonException" />.
    /// </summary>
    [TestMethod]
    public void Lenient_WhenCurrencyHasLongS_ShouldThrowJsonException()
    {
        string json = "{\"amount\":12.3456789,\"currency\":\"uſd\"}";

        _ = Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<CalculatedMoney>(json, OptionsFor(FinancialJsonPolicy.Lenient));
        });
    }
}
