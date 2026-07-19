// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyJsonConverterTests.Compact.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.Serialization.Json;

public partial class MoneyJsonConverterTests
{

    /// <summary>
    /// Verifies that the Compact policy round-trips through the <c>"amount ISO"</c> string form.
    /// </summary>
    [TestMethod]
    public void Compact_WhenRoundTripped_ShouldPreserveValue()
    {
        JsonSerializerOptions options = OptionsFor(FinancialJsonPolicy.Compact);

        string json = JsonSerializer.Serialize(new Money(19.99m, CurrencyCode.USD), options);

        Assert.AreEqual("\"19.99 USD\"", json);
        Assert.AreEqual(new Money(19.99m, CurrencyCode.USD), JsonSerializer.Deserialize<Money>(json, options));
    }

    /// <summary>
    /// Verifies that a six-decimal-place unit price round-trips its precision through the compact string form: the
    /// amount is written with six places and read back reporting six minor units rather than the registry's two.
    /// </summary>
    [TestMethod]
    public void Compact_WhenExplicitScaleRoundTripped_ShouldPreserveMinorUnits()
    {
        JsonSerializerOptions options = OptionsFor(FinancialJsonPolicy.Compact);
        Money original = UnitPrice(12.345678m, 6);

        string json = JsonSerializer.Serialize(original, options);
        Money restored = JsonSerializer.Deserialize<Money>(json, options);

        Assert.AreEqual("\"12.345678 USD\"", json);
        Assert.AreEqual(6, restored.MinorUnits);
        Assert.AreEqual(original, restored);
    }

    /// <summary>
    /// Verifies that a compact six-place unit price whose value has fewer significant digits than its scale restores
    /// its trailing zeros, so it still formats with six fractional digits.
    /// </summary>
    [TestMethod]
    public void Compact_WhenExplicitScaleHasTrailingZeros_ShouldRestoreFractionalDigits()
    {
        JsonSerializerOptions options = OptionsFor(FinancialJsonPolicy.Compact);

        string json = JsonSerializer.Serialize(UnitPrice(12.5m, 6), options);
        Money restored = JsonSerializer.Deserialize<Money>(json, options);

        Assert.AreEqual("\"12.500000 USD\"", json);
        Assert.AreEqual(6, restored.MinorUnits);
        Assert.AreEqual("USD 12.500000", restored.ToString("R"));
    }

    /// <summary>
    /// Verifies that a compact amount printed at the currency's registered precision reports the registry minor units,
    /// so ordinary money is unaffected by the scale inference.
    /// </summary>
    [TestMethod]
    public void Compact_WhenRegistryPrecision_ShouldReportRegistryMinorUnits()
    {
        Money restored = JsonSerializer.Deserialize<Money>("\"19.99 USD\"", OptionsFor(FinancialJsonPolicy.Compact));

        Assert.AreEqual(2, restored.MinorUnits);
        Assert.AreEqual(new Money(19.99m, CurrencyCode.USD), restored);
    }

    /// <summary>
    /// Verifies that the Compact policy rejects a non-string token and an unparseable string.
    /// </summary>
    [TestMethod]
    [DataRow("123", DisplayName = "Compact token is a number")]
    [DataRow("\"not money\"", DisplayName = "Compact string is not money")]
    public void Compact_WhenPayloadInvalid_ShouldThrowJsonException(string json) =>
        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<Money>(json, OptionsFor(FinancialJsonPolicy.Compact)));
}
