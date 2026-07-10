// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyPairJsonConverterTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates;

namespace Bodu.Financial.Serialization.Json;

public partial class CurrencyPairJsonConverterTests
{

    /// <summary>
    /// Verifies that the Strict and Compact policies both round-trip a pair's source and destination codes.
    /// </summary>
    [TestMethod]
    [DataRow(FinancialJsonPolicy.Strict)]
    [DataRow(FinancialJsonPolicy.Compact)]
    public void RoundTrip_ShouldPreserveFromAndTo(FinancialJsonPolicy policy)
    {
        JsonSerializerOptions options = OptionsFor(policy);

        string json = JsonSerializer.Serialize(new CurrencyPair(CurrencyCode.USD, CurrencyCode.JPY), options);
        CurrencyPair restored = JsonSerializer.Deserialize<CurrencyPair>(json, options);

        Assert.AreEqual(CurrencyCode.USD, restored.From);
        Assert.AreEqual(CurrencyCode.JPY, restored.To);
    }
}
