// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyOfTCurrencyTests.MinorUnitsAndComparison.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class MoneyOfTCurrencyTests
{
    // ---------------------------------------------------------------------------------------------------------------
    // FromMinorUnits / ToMinorUnits / TryToMinorUnits
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}.FromMinorUnits(long)" /> reconstructs the expected amount
    /// across the 0/2/3 minor-unit categories.
    /// </summary>
    [TestMethod]
    public void FromMinorUnits_WhenCalled_ShouldReconstructExpectedAmount()
    {
        Assert.AreEqual(new Money<USD>(19.99m), Money<USD>.FromMinorUnits(1999));
        Assert.AreEqual(new Money<JPY>(1234m), Money<JPY>.FromMinorUnits(1234));
        Assert.AreEqual(new Money<BHD>(12.345m), Money<BHD>.FromMinorUnits(12345));
        Assert.AreEqual(new Money<USD>(0m), Money<USD>.FromMinorUnits(0));
        Assert.AreEqual(new Money<USD>(-0.01m), Money<USD>.FromMinorUnits(-1));
    }

    /// <summary>
    /// Verifies the inverse: <see cref="Money{TCurrency}.ToMinorUnits" /> returns the integer minor-unit count
    /// for an amount already at the currency's precision.
    /// </summary>
    [TestMethod]
    public void ToMinorUnits_WhenCalled_ShouldReturnExactCount()
    {
        Assert.AreEqual(1999L, new Money<USD>(19.99m).ToMinorUnits());
        Assert.AreEqual(1234L, new Money<JPY>(1234m).ToMinorUnits());
        Assert.AreEqual(12345L, new Money<BHD>(12.345m).ToMinorUnits());
        Assert.AreEqual(0L, Money<USD>.Zero.ToMinorUnits());
        Assert.AreEqual(-100L, new Money<USD>(-1m).ToMinorUnits());
    }

    /// <summary>
    /// Verifies the round-trip across both directions.
    /// </summary>
    [TestMethod]
    [DataRow(0L)]
    [DataRow(1L)]
    [DataRow(1999L)]
    [DataRow(-50_000L)]
    [DataRow(1_000_000_000_000L)]
    public void MinorUnits_WhenRoundTripped_ShouldRecoverOriginal(long minorUnits)
    {
        var recovered = Money<USD>.FromMinorUnits(minorUnits).ToMinorUnits();

        Assert.AreEqual(minorUnits, recovered);
    }

    /// <summary>
    /// Verifies <see cref="Money{TCurrency}.TryToMinorUnits(out long)" /> succeeds for normal-range values.
    /// </summary>
    [TestMethod]
    public void TryToMinorUnits_WhenInRange_ShouldReturnTrueAndExpectedValue()
    {
        var ok = new Money<USD>(19.99m).TryToMinorUnits(out var result);

        Assert.IsTrue(ok);
        Assert.AreEqual(1999L, result);
    }

    /// <summary>
    /// Verifies <see cref="Money{TCurrency}.TryToMinorUnits(out long)" /> returns false rather than throwing
    /// when the scaled value would overflow <see cref="long" />.
    /// </summary>
    [TestMethod]
    public void TryToMinorUnits_WhenScaledValueOverflowsLong_ShouldReturnFalse()
    {
        var huge = new Money<USD>(decimal.MaxValue / 2m);

        var ok = huge.TryToMinorUnits(out var result);

        Assert.IsFalse(ok);
        Assert.AreEqual(0L, result);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Min / Max / Clamp
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}.Min" /> returns the lesser of two same-currency values.
    /// </summary>
    [TestMethod]
    public void Min_WhenLeftLessThanRight_ShouldReturnLeft()
    {
        var a = new Money<USD>(5m);
        var b = new Money<USD>(10m);

        Assert.AreEqual(a, Money<USD>.Min(a, b));
        Assert.AreEqual(a, Money<USD>.Min(b, a));
    }

    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}.Min" /> handles equal operands by returning the first.
    /// </summary>
    [TestMethod]
    public void Min_WhenOperandsEqual_ShouldReturnLeft()
    {
        var a = new Money<USD>(5m);
        var b = new Money<USD>(5m);

        Assert.AreEqual(a, Money<USD>.Min(a, b));
    }

    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}.Max" /> returns the larger of two same-currency values.
    /// </summary>
    [TestMethod]
    public void Max_WhenLeftGreaterThanRight_ShouldReturnLeft()
    {
        var a = new Money<USD>(10m);
        var b = new Money<USD>(5m);

        Assert.AreEqual(a, Money<USD>.Max(a, b));
        Assert.AreEqual(a, Money<USD>.Max(b, a));
    }

    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}.Clamp" /> returns the value unchanged when inside the range.
    /// </summary>
    [TestMethod]
    public void Clamp_WhenValueInsideRange_ShouldReturnValueUnchanged()
    {
        var value = new Money<USD>(50m);
        var min = new Money<USD>(0m);
        var max = new Money<USD>(100m);

        Assert.AreEqual(value, Money<USD>.Clamp(value, min, max));
    }

    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}.Clamp" /> snaps below-range values to the lower bound and
    /// above-range values to the upper bound.
    /// </summary>
    [TestMethod]
    public void Clamp_WhenValueOutsideRange_ShouldSnapToBound()
    {
        var min = new Money<USD>(0m);
        var max = new Money<USD>(100m);

        Assert.AreEqual(min, Money<USD>.Clamp(new Money<USD>(-50m), min, max));
        Assert.AreEqual(max, Money<USD>.Clamp(new Money<USD>(200m), min, max));
    }

    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}.Clamp" /> throws when the bounds are inverted.
    /// </summary>
    [TestMethod]
    public void Clamp_WhenMinGreaterThanMax_ShouldThrowArgumentException()
    {
        var min = new Money<USD>(100m);
        var max = new Money<USD>(0m);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Money<USD>.Clamp(new Money<USD>(50m), min, max);
        });
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Parser grammar — formal rejection grid for shapes that previously slipped through the heuristic check.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that the parser rejects malformed token shapes — ISO concatenated with no space, embedded
    /// symbols, and non-numeric tokens — that the prior heuristic "contains letter" check sometimes accepted
    /// for the bare-numeric path.
    /// </summary>
    [TestMethod]
    [DataRow("USD19.99")]
    [DataRow("19.99USD")]
    [DataRow("$19.99")]
    [DataRow("USD$19.99")]
    [DataRow("USD 19.99 USD")]
    [DataRow("USD")]
    [DataRow("NaN")]
    [DataRow("Infinity")]
    public void TryParse_WhenInputMalformed_ShouldReturnFalse(string input)
    {
        var ok = Money<USD>.TryParse(input, System.Globalization.CultureInfo.InvariantCulture, out _);

        Assert.IsFalse(ok, $"Expected '{input}' to fail parsing.");
    }
}
