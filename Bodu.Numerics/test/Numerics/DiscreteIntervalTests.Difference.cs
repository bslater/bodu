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
    /// Verifies that removing a run that overhangs one end trims this interval to a single flanking run.
    /// </summary>
    [TestMethod]
    public void Difference_WhenOtherTrimsOneSide_ShouldReturnOneRun()
    {
        DiscreteIntervalPair<int> result = DiscreteInterval<int>.Closed(0, 10).Difference(DiscreteInterval<int>.Closed(8, 20));

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(DiscreteInterval<int>.Closed(0, 7), result[0]);
    }

    /// <summary>
    /// Verifies that subtracting a covering (or equal) run yields no runs.
    /// </summary>
    [TestMethod]
    public void Difference_WhenOtherCoversThis_ShouldReturnEmpty()
    {
        Assert.AreEqual(0, DiscreteInterval<int>.Closed(0, 10).Difference(DiscreteInterval<int>.Closed(0, 10)).Count);
        Assert.AreEqual(0, DiscreteInterval<int>.Closed(2, 8).Difference(DiscreteInterval<int>.Closed(0, 10)).Count);
    }

    /// <summary>
    /// Verifies that subtracting a disjoint run leaves this interval unchanged.
    /// </summary>
    [TestMethod]
    public void Difference_WhenDisjoint_ShouldReturnThisUnchanged()
    {
        DiscreteIntervalPair<int> result = DiscreteInterval<int>.Closed(0, 5).Difference(DiscreteInterval<int>.Closed(10, 20));

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(DiscreteInterval<int>.Closed(0, 5), result[0]);
    }

    /// <summary>
    /// Verifies that the difference of an empty interval is empty.
    /// </summary>
    [TestMethod]
    public void Difference_WhenThisIsEmpty_ShouldReturnEmpty()
    {
        Assert.AreEqual(0, DiscreteInterval<int>.Empty.Difference(DiscreteInterval<int>.Closed(0, 10)).Count);
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
    /// Verifies that subtracting an interval anchored at the domain minimum from the whole line yields only the upper
    /// remainder: the cut below <c>T.MinValue</c> must not wrap to <c>T.MaxValue</c> and produce a bogus left piece.
    /// </summary>
    [TestMethod]
    public void Difference_WhenOtherStartsAtDomainMinimum_ShouldReturnOnlyUpperRemainder()
    {
        DiscreteIntervalPair<int> result = DiscreteInterval<int>.All.Difference(DiscreteInterval<int>.Closed(int.MinValue, 10));

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(DiscreteInterval<int>.AtLeast(11), result[0]);
    }

    /// <summary>
    /// Verifies that subtracting an interval anchored at the domain maximum from the whole line yields only the lower
    /// remainder: the cut above <c>T.MaxValue</c> must not wrap to <c>T.MinValue</c> and produce a bogus right piece.
    /// </summary>
    [TestMethod]
    public void Difference_WhenOtherEndsAtDomainMaximum_ShouldReturnOnlyLowerRemainder()
    {
        DiscreteIntervalPair<int> result = DiscreteInterval<int>.All.Difference(DiscreteInterval<int>.Closed(-10, int.MaxValue));

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(DiscreteInterval<int>.AtMost(-11), result[0]);
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
    /// Verifies that the symmetric difference of equal integer intervals is empty.
    /// </summary>
    [TestMethod]
    public void SymmetricDifference_WhenEqual_ShouldReturnEmpty()
    {
        Assert.AreEqual(0, DiscreteInterval<int>.Closed(0, 5).SymmetricDifference(DiscreteInterval<int>.Closed(0, 5)).Count);
    }

    /// <summary>
    /// Verifies that the symmetric difference with an interior run matches the plain difference (the interior
    /// contributes nothing of its own).
    /// </summary>
    [TestMethod]
    public void SymmetricDifference_WhenOtherIsInterior_ShouldMatchDifference()
    {
        DiscreteIntervalPair<int> result = DiscreteInterval<int>.Closed(0, 10).SymmetricDifference(DiscreteInterval<int>.Closed(3, 5));

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(DiscreteInterval<int>.Closed(0, 2), result[0]);
        Assert.AreEqual(DiscreteInterval<int>.Closed(6, 10), result[1]);
    }

    /// <summary>
    /// Verifies that the symmetric difference of two disjoint runs is both runs, ordered lowest-first.
    /// </summary>
    [TestMethod]
    public void SymmetricDifference_WhenDisjoint_ShouldReturnBothOrdered()
    {
        DiscreteIntervalPair<int> result = DiscreteInterval<int>.Closed(5, 7).SymmetricDifference(DiscreteInterval<int>.Closed(0, 2));

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(DiscreteInterval<int>.Closed(0, 2), result[0]);
        Assert.AreEqual(DiscreteInterval<int>.Closed(5, 7), result[1]);
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
