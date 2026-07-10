// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateJsonConverterTests.Lenient.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates;

namespace Bodu.Financial.Serialization.Json;

public partial class ExchangeRateJsonConverterTests
{

    /// <summary>
    /// Verifies that the Lenient policy upper-cases lower-case ISO codes.
    /// </summary>
    [TestMethod]
    public void Lenient_WhenIsoCodesLowercase_ShouldNormalize()
    {
        string json = "{\"from\":\"usd\",\"to\":\"jpy\",\"date\":\"2024-01-15\",\"rate\":150.25,\"provider\":\"ecb\"}";

        ExchangeRate restored = JsonSerializer.Deserialize<ExchangeRate>(json, OptionsFor(FinancialJsonPolicy.Lenient));

        Assert.AreEqual(CurrencyCode.USD, restored.From);
        Assert.AreEqual(CurrencyCode.JPY, restored.To);
    }
}
