// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalculatedMoneyJsonConverterTests.Strict.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.Serialization.Json;

public partial class CalculatedMoneyJsonConverterTests
{
    /// <summary>
    /// Verifies that a high-precision unit price serializes to the canonical object shape with its every significant
    /// digit intact, without settling to the currency's registered minor units.
    /// </summary>
    [TestMethod]
    public void Strict_WhenSerialized_ShouldWriteFullPrecision()
    {
        string json = JsonSerializer.Serialize(new CalculatedMoney(12.3456789m, CurrencyCode.USD), OptionsFor(FinancialJsonPolicy.Strict));

        Assert.AreEqual("{\"amount\":12.3456789,\"currency\":\"USD\"}", json);
    }

    /// <summary>
    /// Verifies that a value carrying trailing zeros beyond the currency's minor units keeps them on the wire, because
    /// <see cref="CalculatedMoney" /> is never rounded on construction.
    /// </summary>
    [TestMethod]
    public void Strict_WhenAmountHasTrailingZeros_ShouldPreserveThemOnTheWire()
    {
        string json = JsonSerializer.Serialize(new CalculatedMoney(12.500000m, CurrencyCode.USD), OptionsFor(FinancialJsonPolicy.Strict));

        Assert.AreEqual("{\"amount\":12.500000,\"currency\":\"USD\"}", json);
    }

    /// <summary>
    /// Verifies that a six-decimal-place unit price round-trips exactly, preserving both value and the trailing zeros
    /// that fix the number of decimal places.
    /// </summary>
    [TestMethod]
    public void Strict_WhenRoundTripped_ShouldPreservePrecisionAndTrailingZeros()
    {
        var original = new CalculatedMoney(12.500000m, CurrencyCode.USD);

        CalculatedMoney restored = JsonSerializer.Deserialize<CalculatedMoney>(
            JsonSerializer.Serialize(original, OptionsFor(FinancialJsonPolicy.Strict)),
            OptionsFor(FinancialJsonPolicy.Strict));

        Assert.AreEqual(original, restored);
        Assert.AreEqual("12.500000", restored.Amount.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Verifies that the canonical object form rejects malformed payloads with a <see cref="JsonException" />.
    /// </summary>
    [TestMethod]
    [DataRow("[1,2]", DisplayName = "Not an object")]
    [DataRow("{\"amount\":true,\"currency\":\"USD\"}", DisplayName = "Amount is boolean")]
    [DataRow("{\"currency\":\"USD\"}", DisplayName = "Missing amount")]
    [DataRow("{\"amount\":1}", DisplayName = "Missing currency")]
    [DataRow("{\"amount\":1,\"currency\":\"US\"}", DisplayName = "Currency wrong length")]
    [DataRow("{\"amount\":1,\"currency\":\"ZZZ\"}", DisplayName = "Unknown currency")]
    public void Strict_WhenObjectPayloadMalformed_ShouldThrowJsonException(string json) =>
        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<CalculatedMoney>(json, OptionsFor(FinancialJsonPolicy.Strict)));
}
