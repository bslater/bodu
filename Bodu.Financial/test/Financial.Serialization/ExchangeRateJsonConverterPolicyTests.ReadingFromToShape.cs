// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateJsonConverterPolicyTests.ReadingFromToShape.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.ExchangeRates;

namespace Bodu.Financial.Serialization;

public partial class ExchangeRateJsonConverterPolicyTests
{

    /// <summary>
    /// Verifies that the read path accepts both the canonical <c>from</c>/<c>to</c> and the compact
    /// <c>pair</c> shapes interchangeably under any policy.
    /// </summary>
    [TestMethod]
    public void ReadingFromToShape_ShouldSucceedRegardlessOfWritePolicy()
    {
        string json = "{\"pair\":\"USD/JPY\",\"date\":\"2024-05-30\",\"rate\":156.42,\"provider\":\"ECB\"}";

        ExchangeRate result = JsonSerializer.Deserialize<ExchangeRate>(json, Options(FinancialJsonPolicy.Strict));

        Assert.AreEqual(SampleRate(), result);
    }
}
