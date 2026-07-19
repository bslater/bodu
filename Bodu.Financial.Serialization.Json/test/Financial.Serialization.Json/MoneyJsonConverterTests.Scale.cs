// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyJsonConverterTests.Scale.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.Serialization.Json;

public partial class MoneyJsonConverterTests
{
    /// <summary>
    /// Builds a <see cref="Money" /> carrying an explicit minor-unit scale (a unit price at a precision other than the
    /// currency's registered minor units) by settling a <see cref="CalculatedMoney" /> through a custom-scale context.
    /// </summary>
    /// <param name="amount">The unrounded amount to settle.</param>
    /// <param name="scale">The explicit minor-unit scale to settle to.</param>
    /// <returns>The settled value reporting <paramref name="scale" /> from <see cref="Money.MinorUnits" />.</returns>
    private static Money UnitPrice(decimal amount, int scale) =>
        new CalculatedMoney(amount, CurrencyCode.USD)
            .RoundToMoney(MonetaryContext.Default with { ScalePolicy = ScalePolicy.Custom, CustomScale = scale });

    /// <summary>
    /// Verifies that a value carrying an explicit scale emits a <c>scale</c> property under the Strict policy so the
    /// unit-price precision is recorded on the wire.
    /// </summary>
    [TestMethod]
    public void Strict_WhenValueCarriesExplicitScale_ShouldWriteScaleProperty()
    {
        string json = JsonSerializer.Serialize(UnitPrice(12.345678m, 6), OptionsFor(FinancialJsonPolicy.Strict));

        Assert.AreEqual("{\"amount\":12.345678,\"currency\":\"USD\",\"scale\":6}", json);
    }

    /// <summary>
    /// Verifies that ordinary money, whose reported minor units match the currency registry, keeps the historical
    /// two-field shape and does not emit a <c>scale</c> property.
    /// </summary>
    [TestMethod]
    public void Strict_WhenValueUsesRegistryPrecision_ShouldNotWriteScaleProperty()
    {
        string json = JsonSerializer.Serialize(new Money(19.99m, CurrencyCode.USD), OptionsFor(FinancialJsonPolicy.Strict));

        Assert.AreEqual("{\"amount\":19.99,\"currency\":\"USD\"}", json);
    }

    /// <summary>
    /// Verifies that a six-decimal-place unit price round-trips its precision: the deserialized value reports the same
    /// minor-unit scale rather than collapsing to the currency's registered two places.
    /// </summary>
    [TestMethod]
    public void Strict_WhenExplicitScaleRoundTripped_ShouldPreserveMinorUnits()
    {
        Money original = UnitPrice(12.345678m, 6);
        string json = JsonSerializer.Serialize(original, OptionsFor(FinancialJsonPolicy.Strict));

        Money restored = JsonSerializer.Deserialize<Money>(json, OptionsFor(FinancialJsonPolicy.Strict));

        Assert.AreEqual(6, restored.MinorUnits);
        Assert.AreEqual(original.Amount, restored.Amount);
        Assert.AreEqual(original, restored);
    }

    /// <summary>
    /// Verifies that a unit price whose value has fewer significant digits than its scale round-trips its trailing
    /// zeros: a six-place price of <c>12.5</c> is restored so that it formats with six fractional digits.
    /// </summary>
    [TestMethod]
    public void Strict_WhenExplicitScaleHasTrailingZeros_ShouldRestoreFractionalDigits()
    {
        Money original = UnitPrice(12.5m, 6);

        Money restored = JsonSerializer.Deserialize<Money>(
            JsonSerializer.Serialize(original, OptionsFor(FinancialJsonPolicy.Strict)),
            OptionsFor(FinancialJsonPolicy.Strict));

        Assert.AreEqual(6, restored.MinorUnits);
        Assert.AreEqual("USD 12.500000", restored.ToString("R"));
    }

    /// <summary>
    /// Verifies that an explicit <c>scale</c> supplied on the wire is honored when reconstructing a value, even when the
    /// amount itself carries a shorter decimal representation.
    /// </summary>
    [TestMethod]
    public void Strict_WhenScaleSuppliedOnWire_ShouldReconstructAtThatScale()
    {
        Money restored = JsonSerializer.Deserialize<Money>(
            "{\"amount\":12.5,\"currency\":\"USD\",\"scale\":4}",
            OptionsFor(FinancialJsonPolicy.Strict));

        Assert.AreEqual(4, restored.MinorUnits);
        Assert.AreEqual("USD 12.5000", restored.ToString("R"));
    }

    /// <summary>
    /// Verifies that the reader still accepts the two-field shape without a <c>scale</c> property, preserving backward
    /// compatibility with previously written payloads.
    /// </summary>
    [TestMethod]
    public void Strict_WhenScaleOmitted_ShouldUseRegistryPrecision()
    {
        Money restored = JsonSerializer.Deserialize<Money>(
            "{\"amount\":19.99,\"currency\":\"USD\"}",
            OptionsFor(FinancialJsonPolicy.Strict));

        Assert.AreEqual(2, restored.MinorUnits);
        Assert.AreEqual(new Money(19.99m, CurrencyCode.USD), restored);
    }

    /// <summary>
    /// Verifies that a <c>scale</c> value outside the supported 0 to 28 range is rejected with a
    /// <see cref="JsonException" />.
    /// </summary>
    [TestMethod]
    [DataRow("{\"amount\":1,\"currency\":\"USD\",\"scale\":-1}", DisplayName = "Scale below range")]
    [DataRow("{\"amount\":1,\"currency\":\"USD\",\"scale\":29}", DisplayName = "Scale above range")]
    public void Strict_WhenScaleOutOfRange_ShouldThrowJsonException(string json) =>
        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<Money>(json, OptionsFor(FinancialJsonPolicy.Strict)));

    /// <summary>
    /// Verifies that a non-integer or duplicate <c>scale</c> property is rejected with a <see cref="JsonException" />.
    /// </summary>
    [TestMethod]
    [DataRow("{\"amount\":1,\"currency\":\"USD\",\"scale\":\"6\"}", DisplayName = "Scale is a string")]
    [DataRow("{\"amount\":1,\"currency\":\"USD\",\"scale\":2.5}", DisplayName = "Scale is fractional")]
    [DataRow("{\"amount\":1,\"currency\":\"USD\",\"scale\":2,\"scale\":3}", DisplayName = "Duplicate scale")]
    public void Strict_WhenScaleMalformed_ShouldThrowJsonException(string json) =>
        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<Money>(json, OptionsFor(FinancialJsonPolicy.Strict)));
}
