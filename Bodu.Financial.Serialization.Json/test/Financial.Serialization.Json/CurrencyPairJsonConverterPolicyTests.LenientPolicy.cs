// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyPairJsonConverterPolicyTests.LenientPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates;

namespace Bodu.Financial.Serialization.Json;

public partial class CurrencyPairJsonConverterPolicyTests
{

    /// <summary>
    /// Verifies that the lenient policy normalizes lowercase ISO codes during object-form read.
    /// </summary>
    [TestMethod]
    public void LenientPolicy_WhenReadingLowercaseCurrency_ShouldSucceed()
    {
        string json = "{\"from\":\"usd\",\"to\":\"jpy\"}";

        CurrencyPair pair = JsonSerializer.Deserialize<CurrencyPair>(json, Options(FinancialJsonPolicy.Lenient));

        Assert.AreEqual(new CurrencyPair(CurrencyCode.USD, CurrencyCode.JPY), pair);
    }

    /// <summary>
    /// Verifies that the lenient policy rejects a code written with U+017F LATIN SMALL LETTER LONG S, which invariant
    /// uppercasing maps to <c>S</c> but which is not a lowercase ISO letter, with <see cref="JsonException" />.
    /// </summary>
    [TestMethod]
    public void LenientPolicy_WhenCurrencyHasLongS_ShouldThrowJsonException()
    {
        string json = "{\"from\":\"uſd\",\"to\":\"jpy\"}";

        _ = Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<CurrencyPair>(json, Options(FinancialJsonPolicy.Lenient));
        });
    }
}
