// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateJsonConverterPolicyTests.LenientPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bodu.Financial.Serialization;

public partial class ExchangeRateJsonConverterPolicyTests
{

    /// <summary>
    /// Verifies that the lenient policy normalises lowercase ISO codes on read.
    /// </summary>
    [TestMethod]
    public void LenientPolicy_WhenReadingLowercaseCurrency_ShouldSucceed()
    {
        string json = "{\"from\":\"usd\",\"to\":\"jpy\",\"date\":\"2024-05-30\",\"rate\":156.42,\"provider\":\"ECB\",\"isInverted\":false}";

        ExchangeRate result = JsonSerializer.Deserialize<ExchangeRate>(json, Options(FinancialJsonPolicy.Lenient));

        Assert.AreEqual(SampleRate(), result);
    }
}
