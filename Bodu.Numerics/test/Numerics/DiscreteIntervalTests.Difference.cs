// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DiscreteIntervalTests.Difference.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class DiscreteIntervalTests
{
    /// <summary>
    /// Verifies that removing an interior integer interval leaves the two flanking runs on the integer grid.
    /// </summary>
    [TestMethod]
    public void Difference_WhenOtherIsInterior_ShouldReturnTwoFlankingRuns()
    {
        DiscreteIntervalPair<int> result = DiscreteInterval<int>.Closed(0, 10).Difference(DiscreteInterval<int>.Closed(3, 5));

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(DiscreteInterval<int>.Closed(0, 2), result[0]);
        Assert.AreEqual(DiscreteInterval<int>.Closed(6, 10), result[1]);
    }

    /// <summary>
    /// Verifies that trimming one side yields a single run and that a covering subtrahend yields nothing.
    /// </summary>
    [TestMethod]
    public void Difference_WhenTrimmedOrCovered_ShouldReturnExpectedCount()
    {
        Assert.AreEqual(DiscreteInterval<int>.Closed(0, 7), DiscreteInterval<int>.Closed(0, 10).Difference(DiscreteInterval<int>.Closed(8, 20))[0]);
        Assert.AreEqual(0, DiscreteInterval<int>.Closed(0, 10).Difference(DiscreteInterval<int>.Closed(0, 10)).Count);
        Assert.AreEqual(DiscreteInterval<int>.Closed(0, 5), DiscreteInterval<int>.Closed(0, 5).Difference(DiscreteInterval<int>.Closed(10, 20))[0]);
    }

    /// <summary>
    /// Verifies that subtracting a finite interval from the whole integer line yields the two surrounding half-lines.
    /// </summary>
    [TestMethod]
    public void Difference_WhenSubtractingFromAll_ShouldReturnTwoHalfLines()
    {
        DiscreteIntervalPair<int> result = DiscreteInterval<int>.All.Difference(DiscreteInterval<int>.Closed(3, 5));

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(DiscreteInterval<int>.AtMost(2), result[0]);
        Assert.AreEqual(DiscreteInterval<int>.AtLeast(6), result[1]);
    }

    /// <summary>
    /// Verifies the symmetric difference of two partially overlapping integer intervals.
    /// </summary>
    [TestMethod]
    public void SymmetricDifference_WhenPartiallyOverlapping_ShouldReturnBothFlanks()
    {
        DiscreteIntervalPair<int> result = DiscreteInterval<int>.Closed(0, 5).SymmetricDifference(DiscreteInterval<int>.Closed(3, 8));

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(DiscreteInterval<int>.Closed(0, 2), result[0]);
        Assert.AreEqual(DiscreteInterval<int>.Closed(6, 8), result[1]);
    }

    /// <summary>
    /// Verifies that Difference agrees with the pointwise integer predicate across an integer universe.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Difference_OverIntegerDomain_ShouldSatisfyMembershipLaw()
    {
        for (int aLo = 0; aLo <= 4; aLo++)
        {
            for (int aHi = 0; aHi <= 4; aHi++)
            {
                for (int bLo = 0; bLo <= 4; bLo++)
                {
                    for (int bHi = 0; bHi <= 4; bHi++)
                    {
                        DiscreteInterval<int> a = DiscreteInterval<int>.Closed(aLo, aHi);
                        DiscreteInterval<int> b = DiscreteInterval<int>.Closed(bLo, bHi);
                        DiscreteIntervalPair<int> diff = a.Difference(b);

                        for (int x = -1; x <= 5; x++)
                        {
                            bool inDiff = false;
                            foreach (DiscreteInterval<int> piece in diff)
                                inDiff |= piece.Contains(x);

                            Assert.AreEqual(a.Contains(x) && !b.Contains(x), inDiff, $"a={a}, b={b}, x={x}");
                        }
                    }
                }
            }
        }
    }
}
