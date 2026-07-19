// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalculatedMoneyJsonConverterTests.Compact.cs" company="Bodu Pty. Ltd.">
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
    /// Verifies that the Compact policy serializes a high-precision value as a single <c>"amount ISO"</c> string with
    /// its full precision, including trailing zeros, preserved verbatim.
    /// </summary>
    [TestMethod]
    public void Compact_WhenSerialized_ShouldWriteAmountAndIsoString()
    {
        string json = JsonSerializer.Serialize(new CalculatedMoney(12.500000m, CurrencyCode.USD), OptionsFor(FinancialJsonPolicy.Compact));

        Assert.AreEqual("\"12.500000 USD\"", json);
    }

    /// <summary>
    /// Verifies that the compact string form round-trips a high-precision unit price, preserving value and trailing
    /// zeros.
    /// </summary>
    [TestMethod]
    public void Compact_WhenRoundTripped_ShouldPreservePrecision()
    {
        var original = new CalculatedMoney(12.345678m, CurrencyCode.USD);

        CalculatedMoney restored = JsonSerializer.Deserialize<CalculatedMoney>(
            JsonSerializer.Serialize(original, OptionsFor(FinancialJsonPolicy.Compact)),
            OptionsFor(FinancialJsonPolicy.Compact));

        Assert.AreEqual(original, restored);
        Assert.AreEqual("12.345678", restored.Amount.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Verifies that the compact reader rejects a non-string token and malformed compact strings with a
    /// <see cref="JsonException" />.
    /// </summary>
    [TestMethod]
    [DataRow("12.5", DisplayName = "Not a string")]
    [DataRow("\"12.5\"", DisplayName = "Missing currency")]
    [DataRow("\"12.5 ZZZ\"", DisplayName = "Unknown currency")]
    [DataRow("\"x USD\"", DisplayName = "Amount not numeric")]
    public void Compact_WhenPayloadMalformed_ShouldThrowJsonException(string json) =>
        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<CalculatedMoney>(json, OptionsFor(FinancialJsonPolicy.Compact)));
}
