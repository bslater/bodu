// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequenceGeneration.Farey.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Extensions;

[TestClass]
public class FareyTests
{
    /// <summary>
    /// Verifies that <see cref="SequenceGenerator.Farey" /> throws <see cref="ArgumentOutOfRangeException" /> when the order is below the minimum of 1.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    public void Farey_WhenOrderIsLessThanOne_ShouldThrowExactly(int order)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = SequenceGenerator.Farey(order).ToList();
        });
    }

    /// <summary>
    /// Verifies that <see cref="SequenceGenerator.Farey" /> with order 1 returns the trivial sequence (0/1, 1/1).
    /// </summary>
    [TestMethod]
    public void Farey_WhenOrderIsOne_ShouldReturnTrivialSequence()
    {
        var actual = SequenceGenerator.Farey(1).ToArray();
        CollectionAssert.AreEqual(new[] { (0, 1), (1, 1) }, actual);
    }

    /// <summary>
    /// Verifies that <see cref="SequenceGenerator.Farey" /> with order 5 returns the canonical Farey sequence F5.
    /// </summary>
    [TestMethod]
    public void Farey_WhenOrderIsFive_ShouldReturnExpectedSequence()
    {
        var actual = SequenceGenerator.Farey(5).ToArray();
        var expected = new[]
        {
            (0, 1), (1, 5), (1, 4), (1, 3), (2, 5), (1, 2),
            (3, 5), (2, 3), (3, 4), (4, 5), (1, 1)
        };

        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that every emitted fraction is in lowest terms (gcd(numerator, denominator) == 1).
    /// </summary>
    [TestMethod]
    public void Farey_WhenEnumerated_ShouldYieldOnlyReducedFractions()
    {
        const int order = 7;

        foreach ((int num, int den) in SequenceGenerator.Farey(order))
        {
            Assert.AreEqual(1, Gcd(num, den), $"Fraction {num}/{den} is not in lowest terms.");
        }

        static int Gcd(int a, int b) => b == 0 ? a : Gcd(b, a % b);
    }

    /// <summary>
    /// Verifies that the emitted fractions are in strictly ascending rational order.
    /// </summary>
    [TestMethod]
    public void Farey_WhenEnumerated_ShouldBeStrictlyAscending()
    {
        var actual = SequenceGenerator.Farey(8).ToArray();
        for (int i = 1; i < actual.Length; i++)
        {
            double previous = (double)actual[i - 1].Numerator / actual[i - 1].Denominator;
            double current = (double)actual[i].Numerator / actual[i].Denominator;
            Assert.IsTrue(current > previous, $"Sequence is not strictly ascending at index {i}: {previous} >= {current}.");
        }
    }

    /// <summary>
    /// Verifies that the sequence always begins at 0/1 and ends at 1/1 regardless of order.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(6)]
    [DataRow(12)]
    public void Farey_WhenEnumerated_ShouldStartAtZeroAndEndAtOne(int order)
    {
        var actual = SequenceGenerator.Farey(order).ToArray();
        Assert.AreEqual((0, 1), actual[0]);
        Assert.AreEqual((1, 1), actual[^1]);
    }
}
