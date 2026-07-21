// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyJsonConverterTests.PriorArtDefects.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.Serialization.Json;

public partial class MoneyJsonConverterTests
{
    /// <summary>
    /// Verifies that amounts serialize as plain decimal literals, never scientific notation — the wire-format defect
    /// where 100.00 EUR serialized as <c>"1E+2"</c> through BigDecimal's exponent form
    /// (zalando/jackson-datatype-money#64).
    /// </summary>
    [TestMethod]
    public void PriorArtDefects_WhenSerializingRoundAmounts_ShouldNeverUseScientificNotation()
    {
        JsonSerializerOptions options = OptionsFor(FinancialJsonPolicy.Strict);

        Assert.AreEqual("{\"amount\":100.00,\"currency\":\"EUR\"}", JsonSerializer.Serialize(new Money(100.00m, CurrencyCode.EUR), options));
        Assert.AreEqual(
            "{\"amount\":1000000.00,\"currency\":\"EUR\"}",
            JsonSerializer.Serialize(new Money(1000000.00m, CurrencyCode.EUR), options));
    }

    /// <summary>
    /// Verifies that a tiny high-precision amount serializes as a plain positional literal — the exponent-form
    /// counterpart for sub-unit values (<c>1E-7</c>-style output), which would break consumers parsing fixed-point
    /// wire formats.
    /// </summary>
    [TestMethod]
    public void PriorArtDefects_WhenSerializingTinyCalculatedAmount_ShouldUsePositionalNotation()
    {
        string json = JsonSerializer.Serialize(
            new CalculatedMoney(0.0000001m, CurrencyCode.USD),
            OptionsFor(FinancialJsonPolicy.Strict));

        Assert.AreEqual("{\"amount\":0.0000001,\"currency\":\"USD\"}", json);
    }
}
