// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyPairJsonConverterPolicyTests.LenientPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates;

namespace Bodu.Financial.Serialization;

public partial class CurrencyPairJsonConverterPolicyTests
{

    /// <summary>
    /// Verifies that the lenient policy normalises lowercase ISO codes during object-form read.
    /// </summary>
    [TestMethod]
    public void LenientPolicy_WhenReadingLowercaseCurrency_ShouldSucceed()
    {
        string json = "{\"from\":\"usd\",\"to\":\"jpy\"}";

        CurrencyPair pair = JsonSerializer.Deserialize<CurrencyPair>(json, Options(FinancialJsonPolicy.Lenient));

        Assert.AreEqual(new CurrencyPair(CurrencyCode.USD, CurrencyCode.JPY), pair);
    }
}
