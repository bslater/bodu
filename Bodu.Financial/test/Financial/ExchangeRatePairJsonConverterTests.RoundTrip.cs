// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRatePairJsonConverterTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Serialization;

namespace Bodu.Financial;

public partial class ExchangeRatePairJsonConverterTests
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

        string json = JsonSerializer.Serialize(new ExchangeRatePair("USD", "JPY"), options);
        ExchangeRatePair restored = JsonSerializer.Deserialize<ExchangeRatePair>(json, options);

        Assert.AreEqual("USD", restored.FromIsoCode);
        Assert.AreEqual("JPY", restored.ToIsoCode);
    }
}
