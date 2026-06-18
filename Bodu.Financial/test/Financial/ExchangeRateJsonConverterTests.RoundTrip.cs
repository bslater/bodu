// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateJsonConverterTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Serialization;

namespace Bodu.Financial;

public partial class ExchangeRateJsonConverterTests
{

    /// <summary>
    /// Verifies that the Strict and Compact policies both round-trip the rate's identifying fields.
    /// </summary>
    [TestMethod]
    [DataRow(FinancialJsonPolicy.Strict)]
    [DataRow(FinancialJsonPolicy.Compact)]
    public void RoundTrip_ShouldPreserveIdentifyingFields(FinancialJsonPolicy policy)
    {
        JsonSerializerOptions options = OptionsFor(policy);

        string json = JsonSerializer.Serialize(Sample(), options);
        ExchangeRate restored = JsonSerializer.Deserialize<ExchangeRate>(json, options);

        Assert.AreEqual("USD", restored.FromIsoCode);
        Assert.AreEqual("JPY", restored.ToIsoCode);
        Assert.AreEqual(new DateOnly(2024, 1, 15), restored.Date);
        Assert.AreEqual(150.25m, restored.Rate);
        Assert.AreEqual("ecb", restored.Provider);
    }
}
