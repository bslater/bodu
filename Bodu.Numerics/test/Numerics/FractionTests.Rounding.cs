// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FractionTests.Rounding.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class FractionTests
{
    /// <summary>
    /// Verifies that Floor, Ceiling, and Truncate round a value in each direction.
    /// </summary>
    [TestMethod]
    [DataRow(7, 4, 1, 2, 1)]
    [DataRow(-7, 4, -2, -1, -1)]
    [DataRow(8, 4, 2, 2, 2)]
    [DataRow(0, 1, 0, 0, 0)]
    [DataRow(-1, 4, -1, 0, 0)]
    public void FloorCeilingTruncate_WhenInvoked_ShouldRoundInEachDirection(
        int numerator,
        int denominator,
        int floor,
        int ceiling,
        int truncate)
    {
        Fraction<int> value = new Fraction<int>(numerator, denominator);

        Assert.AreEqual(new Fraction<int>(floor), value.Floor(), nameof(value.Floor));
        Assert.AreEqual(new Fraction<int>(ceiling), value.Ceiling(), nameof(value.Ceiling));
        Assert.AreEqual(new Fraction<int>(truncate), value.Truncate(), nameof(value.Truncate));
    }

    /// <summary>
    /// Verifies that Round resolves halfway values to the nearest even integer by default.
    /// </summary>
    [TestMethod]
    [DataRow(1, 2, 0)]
    [DataRow(3, 2, 2)]
    [DataRow(5, 2, 2)]
    [DataRow(7, 2, 4)]
    [DataRow(7, 4, 2)]
    [DataRow(-5, 2, -2)]
    public void Round_WhenUsingDefaultMode_ShouldRoundToNearestEven(int numerator, int denominator, int expected)
    {
        Assert.AreEqual(new Fraction<int>(expected), new Fraction<int>(numerator, denominator).Round());
    }

    /// <summary>
    /// Verifies that Round honors the supplied midpoint rule for a halfway value.
    /// </summary>
    [TestMethod]
    [DataRow(MidpointRounding.AwayFromZero, 3)]
    [DataRow(MidpointRounding.ToZero, 2)]
    [DataRow(MidpointRounding.ToNegativeInfinity, 2)]
    [DataRow(MidpointRounding.ToPositiveInfinity, 3)]
    [DataRow(MidpointRounding.ToEven, 2)]
    public void Round_WhenValueIsHalfway_ShouldHonorMidpointRule(MidpointRounding mode, int expected)
    {
        Assert.AreEqual(new Fraction<int>(expected), new Fraction<int>(5, 2).Round(mode));
    }

    /// <summary>
    /// Verifies that Round rejects an undefined midpoint rule.
    /// </summary>
    [TestMethod]
    public void Round_WhenModeIsUndefined_ShouldThrowExactly()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new Fraction<int>(5, 2).Round((MidpointRounding)99);
        });
    }

    /// <summary>
    /// Verifies that the integer and fractional parts recombine to the original value.
    /// </summary>
    [TestMethod]
    [DataRow(7, 4, 1, 3, 4)]
    [DataRow(-7, 4, -1, -3, 4)]
    [DataRow(3, 4, 0, 3, 4)]
    [DataRow(8, 4, 2, 0, 1)]
    public void MixedParts_WhenSplit_ShouldRecombineToOriginal(
        int numerator,
        int denominator,
        int whole,
        int fractionNumerator,
        int fractionDenominator)
    {
        Fraction<int> value = new Fraction<int>(numerator, denominator);

        Assert.AreEqual(whole, value.GetWholePart(), nameof(value.GetWholePart));
        Assert.AreEqual(new Fraction<int>(fractionNumerator, fractionDenominator), value.GetFractionalPart());

        (int Whole, Fraction<int> Fraction) parts = value.ToMixedParts();
        Assert.AreEqual(whole, parts.Whole);
        Assert.AreEqual(value, new Fraction<int>(parts.Whole) + parts.Fraction);
    }

    /// <summary>
    /// Verifies that the mixed-number deconstruction yields signed parts that recombine to the original value.
    /// </summary>
    [TestMethod]
    public void Deconstruct_WhenSplitIntoMixedParts_ShouldYieldSignedComponents()
    {
        (int whole, int numerator, int denominator) = new Fraction<int>(-7, 4);

        Assert.AreEqual(-1, whole);
        Assert.AreEqual(-3, numerator);
        Assert.AreEqual(4, denominator);
    }
}
