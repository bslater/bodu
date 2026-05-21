// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FractionTests.Comparison.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class FractionTests
{
    /// <summary>
    /// Verifies that <see cref="Fraction{T}.CompareTo(Fraction{T})" /> orders values across sign boundaries.
    /// </summary>
    [TestMethod]
    public void CompareTo_WhenOrderingAcrossSigns_ShouldReturnExpectedRelativeOrder()
    {
        Fraction<int> negative = new Fraction<int>(-1, 2);
        Fraction<int> zero = Fraction<int>.Zero;
        Fraction<int> positive = new Fraction<int>(1, 3);

        Assert.IsTrue(negative.CompareTo(zero) < 0);
        Assert.IsTrue(zero.CompareTo(positive) < 0);
        Assert.IsTrue(positive.CompareTo(negative) > 0);
        Assert.AreEqual(0, positive.CompareTo(new Fraction<int>(2, 6)));
    }

    /// <summary>
    /// Verifies that comparing with a <see langword="null" /> object sorts the value after <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void CompareTo_WhenComparedWithNullObject_ShouldReturnPositive()
    {
        Assert.IsTrue(new Fraction<int>(1, 2).CompareTo(null) > 0);
    }

    /// <summary>
    /// Verifies that comparing with an object of a foreign type throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void CompareTo_WhenComparedWithForeignType_ShouldThrowArgumentException()
    {
        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new Fraction<int>(1, 2).CompareTo("1/2");
        });
    }

    /// <summary>
    /// Verifies that sorting a sequence of fractions yields ascending numeric order.
    /// </summary>
    [TestMethod]
    public void CompareTo_WhenUsedToSort_ShouldYieldAscendingOrder()
    {
        Fraction<int>[] values =
        [
            new Fraction<int>(3, 4),
            new Fraction<int>(-1, 2),
            new Fraction<int>(1, 8),
            new Fraction<int>(2, 3),
        ];

        Array.Sort(values);

        CollectionAssert.AreEqual(
            new[]
            {
                new Fraction<int>(-1, 2),
                new Fraction<int>(1, 8),
                new Fraction<int>(2, 3),
                new Fraction<int>(3, 4),
            },
            values);
    }

    /// <summary>
    /// Verifies that the static Compare, Min, and Max return the expected ordering results.
    /// </summary>
    [TestMethod]
    public void CompareMinMax_WhenGivenTwoValues_ShouldReturnExpectedResults()
    {
        Fraction<int> low = new Fraction<int>(1, 3);
        Fraction<int> high = new Fraction<int>(1, 2);

        Assert.IsTrue(Fraction<int>.Compare(low, high) < 0);
        Assert.IsTrue(Fraction<int>.Compare(high, low) > 0);
        Assert.AreEqual(0, Fraction<int>.Compare(low, low));
        Assert.AreEqual(low, Fraction<int>.Min(low, high));
        Assert.AreEqual(high, Fraction<int>.Max(low, high));
    }

    /// <summary>
    /// Verifies that Clamp constrains a value to the inclusive bounds.
    /// </summary>
    [TestMethod]
    public void Clamp_WhenValueIsOutsideRange_ShouldConstrainToBounds()
    {
        Fraction<int> min = Fraction<int>.Zero;
        Fraction<int> max = Fraction<int>.One;

        Assert.AreEqual(max, Fraction<int>.Clamp(new Fraction<int>(3, 2), min, max));
        Assert.AreEqual(min, Fraction<int>.Clamp(new Fraction<int>(-1, 2), min, max));
        Assert.AreEqual(new Fraction<int>(1, 2), Fraction<int>.Clamp(new Fraction<int>(1, 2), min, max));
    }

    /// <summary>
    /// Verifies that Clamp rejects a range whose minimum exceeds its maximum.
    /// </summary>
    [TestMethod]
    public void Clamp_WhenMinExceedsMax_ShouldThrowArgumentException()
    {
        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Fraction<int>.Clamp(Fraction<int>.Zero, Fraction<int>.One, Fraction<int>.Zero);
        });
    }
}
