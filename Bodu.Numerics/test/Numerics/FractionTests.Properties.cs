// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FractionTests.Properties.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Numerics;

public partial class FractionTests
{
    /// <summary>
    /// Verifies that the classification properties report known answers for a range of canonical values.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    [DataRow(0, 1, true, true, true, false, false, false)]
    [DataRow(1, 1, false, true, false, true, false, true)]
    [DataRow(-1, 1, false, true, false, true, true, false)]
    [DataRow(1, 2, false, false, true, true, false, true)]
    [DataRow(-1, 2, false, false, true, true, true, false)]
    [DataRow(3, 4, false, false, true, false, false, true)]
    [DataRow(-3, 4, false, false, true, false, true, false)]
    [DataRow(7, 4, false, false, false, false, false, true)]
    [DataRow(-7, 4, false, false, false, false, true, false)]
    [DataRow(5, 1, false, true, false, false, false, true)]
    [DataRow(-5, 1, false, true, false, false, true, false)]
    public void Properties_WhenInspected_ShouldReportKnownAnswers(
        int numerator,
        int denominator,
        bool isZero,
        bool isInteger,
        bool isProper,
        bool isUnit,
        bool isNegative,
        bool isPositive)
    {
        Fraction<int> value = new Fraction<int>(numerator, denominator);

        Assert.AreEqual(isZero, value.IsZero, nameof(value.IsZero));
        Assert.AreEqual(isInteger, value.IsInteger, nameof(value.IsInteger));
        Assert.AreEqual(isProper, value.IsProper, nameof(value.IsProper));
        Assert.AreEqual(isUnit, value.IsUnit, nameof(value.IsUnit));
        Assert.AreEqual(isNegative, value.IsNegative, nameof(value.IsNegative));
        Assert.AreEqual(isPositive, value.IsPositive, nameof(value.IsPositive));
    }

    /// <summary>
    /// Verifies that exactly one of <c>IsZero</c>, <c>IsNegative</c>, and <c>IsPositive</c> holds for any value.
    /// </summary>
    [TestMethod]
    [DataRow(0, 1)]
    [DataRow(3, 4)]
    [DataRow(-3, 4)]
    [DataRow(9, 2)]
    public void Properties_WhenInspectingSign_ShouldBeMutuallyExclusive(int numerator, int denominator)
    {
        Fraction<int> value = new Fraction<int>(numerator, denominator);

        int trueCount = (value.IsZero ? 1 : 0) + (value.IsNegative ? 1 : 0) + (value.IsPositive ? 1 : 0);

        Assert.AreEqual(1, trueCount);
    }

    /// <summary>
    /// Verifies that the extended classification properties report known answers for a range of values.
    /// </summary>
    [TestMethod]
    [DataRow(3, 1, true, true, false, true)]
    [DataRow(4, 1, true, true, true, false)]
    [DataRow(3, 4, false, false, false, false)]
    [DataRow(5, 2, false, true, false, false)]
    [DataRow(-3, 1, true, true, false, true)]
    [DataRow(0, 1, true, false, true, false)]
    [DataRow(-6, 3, true, true, true, false)]
    [DataRow(1, 1, true, true, false, true)]
    public void Properties_WhenInspectingExtendedClassification_ShouldReportKnownAnswers(
        int numerator,
        int denominator,
        bool isWhole,
        bool isImproper,
        bool isEvenInteger,
        bool isOddInteger)
    {
        Fraction<int> value = new Fraction<int>(numerator, denominator);

        Assert.AreEqual(isWhole, value.IsWhole, nameof(value.IsWhole));
        Assert.AreEqual(isWhole, value.IsInteger, nameof(value.IsInteger));
        Assert.AreEqual(isImproper, value.IsImproper, nameof(value.IsImproper));
        Assert.AreEqual(isEvenInteger, value.IsEvenInteger, nameof(value.IsEvenInteger));
        Assert.AreEqual(isOddInteger, value.IsOddInteger, nameof(value.IsOddInteger));
    }

    /// <summary>
    /// Verifies that a canonical fraction is never reducible and always reports as canonical.
    /// </summary>
    [TestMethod]
    public void Properties_WhenInspectingCanonicalForm_ShouldReportNeverReducible()
    {
        Fraction<int> value = new Fraction<int>(2, 4);

        Assert.IsFalse(value.IsReducible);
        Assert.IsTrue(value.IsCanonical);
    }

    /// <summary>
    /// Verifies that MinValue and MaxValue report the bounds of a fixed-width backing type.
    /// </summary>
    [TestMethod]
    public void MinMaxValue_WhenBackingTypeIsBounded_ShouldReportBackingTypeBounds()
    {
        Assert.AreEqual(new Fraction<int>(int.MinValue), Fraction<int>.MinValue);
        Assert.AreEqual(new Fraction<int>(int.MaxValue), Fraction<int>.MaxValue);
    }

    /// <summary>
    /// Verifies that MinValue throws NotSupportedException for an unbounded backing type.
    /// </summary>
    [TestMethod]
    public void MinValue_WhenBackingTypeIsUnbounded_ShouldThrowExactly()
    {
        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = Fraction<BigInteger>.MinValue;
        });
    }

    /// <summary>
    /// Verifies that MaxValue throws NotSupportedException for an unbounded backing type.
    /// </summary>
    [TestMethod]
    public void MaxValue_WhenBackingTypeIsUnbounded_ShouldThrowExactly()
    {
        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = Fraction<BigInteger>.MaxValue;
        });
    }
}
