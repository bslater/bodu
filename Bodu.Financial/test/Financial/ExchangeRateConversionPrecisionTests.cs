// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateConversionPrecisionTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;

namespace Bodu.Financial;

/// <summary>
/// Verifies that inverted exchange rates convert by dividing by the originally observed rate rather than multiplying by
/// a pre-rounded reciprocal, so the conversion is precise and survives inversion and serialization.
/// </summary>
[TestClass]
public sealed class ExchangeRateConversionPrecisionTests
{
    /// <summary>
    /// The observation date used across the cases.
    /// </summary>
    private static readonly DateOnly Date = new(2024, 5, 30);

    /// <summary>
    /// Verifies that converting the observed amount through an inverted rate divides exactly, where multiplying by the
    /// rounded reciprocal would not yield the exact result.
    /// </summary>
    [TestMethod]
    public void Convert_WhenInvertedFromObservedRate_ShouldDivideExactly()
    {
        var inverted = ExchangeRate.FromObservedRate("JPY", "USD", Date, 156.42m, "ECB", isInverted: true);

        // 156.42 JPY -> USD must be exactly 1; amount * (1 / 156.42) would round to 0.999...9.
        Assert.AreEqual(1m, inverted.Convert(156.42m));
        Assert.AreNotEqual(1m, 156.42m * (1m / 156.42m));
    }

    /// <summary>
    /// Verifies that inverting a typed rate twice returns the original rate exactly, including its reported multiplier.
    /// </summary>
    [TestMethod]
    public void Inverse_WhenAppliedTwice_ShouldReturnOriginalExactly()
    {
        var original = ExchangeRate<USD, JPY>.From(156.42m, Date, "ECB");

        ExchangeRate<USD, JPY> roundTrip = original.Inverse().Inverse();

        Assert.AreEqual(original, roundTrip);
        Assert.AreEqual(original.Rate, roundTrip.Rate);
    }

    /// <summary>
    /// Verifies that an inverted rate keeps its precise division semantics across a JSON round-trip.
    /// </summary>
    [TestMethod]
    public void JsonRoundTrip_WhenInvertedFromObservedRate_ShouldPreservePreciseConversion()
    {
        var inverted = ExchangeRate.FromObservedRate("JPY", "USD", Date, 156.42m, "ECB", isInverted: true);

        var json = JsonSerializer.Serialize(inverted);
        ExchangeRate recovered = JsonSerializer.Deserialize<ExchangeRate>(json);

        Assert.AreEqual(inverted, recovered);
        Assert.AreEqual(1m, recovered.Convert(156.42m));
    }
}
