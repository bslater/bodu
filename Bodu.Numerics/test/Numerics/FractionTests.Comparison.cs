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
}
