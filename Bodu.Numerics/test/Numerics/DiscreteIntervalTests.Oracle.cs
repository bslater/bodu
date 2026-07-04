// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DiscreteIntervalTests.Oracle.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Numerics;

public partial class DiscreteIntervalTests
{
    /// <summary>The small integer domain the oracle sweeps membership over.</summary>
    private static readonly int[] Domain = [-3, -2, -1, 0, 1, 2, 3];

    /// <summary>
    /// Verifies that <see cref="DiscreteInterval{T}.Contains(int)" /> agrees with a brute-force integer set for every
    /// bracket shape over the small domain — the exhaustive check that catches successor/predecessor off-by-one bugs in
    /// canonicalization.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void Contains_ForEveryShapeOverSmallDomain_ShouldMatchBruteForceSet()
    {
        foreach (int a in Domain)
        {
            foreach (int b in Domain)
            {
                AssertMembership(DiscreteInterval<int>.Closed(a, b), x => a <= x && x <= b, $"[{a}, {b}]");
                AssertMembership(DiscreteInterval<int>.Open(a, b), x => a < x && x < b, $"({a}, {b})");
                AssertMembership(DiscreteInterval<int>.ClosedOpen(a, b), x => a <= x && x < b, $"[{a}, {b})");
                AssertMembership(DiscreteInterval<int>.OpenClosed(a, b), x => a < x && x <= b, $"({a}, {b}]");
            }
        }
    }

    /// <summary>
    /// Verifies that <see cref="DiscreteInterval{T}.Intersect(DiscreteInterval{T})" /> matches the set intersection for
    /// every pair of closed intervals over the small domain.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void Intersect_ForEveryPairOverSmallDomain_ShouldMatchBruteForceIntersection() =>
        AssertBinary((a, b) => Members(a.Intersect(b)), (sa, sb) => sa.Intersect(sb));

    /// <summary>
    /// Verifies that <see cref="DiscreteInterval{T}.Difference(DiscreteInterval{T})" /> matches the set difference for
    /// every pair of closed intervals over the small domain.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void Difference_ForEveryPairOverSmallDomain_ShouldMatchBruteForceExcept() =>
        AssertBinary((a, b) => Members(a.Difference(b)), (sa, sb) => sa.Except(sb));

    /// <summary>
    /// Verifies that <see cref="DiscreteInterval{T}.SymmetricDifference(DiscreteInterval{T})" /> matches the set
    /// symmetric difference for every pair of closed intervals over the small domain.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void SymmetricDifference_ForEveryPairOverSmallDomain_ShouldMatchBruteForceSymmetricExcept() =>
        AssertBinary(
            (a, b) => Members(a.SymmetricDifference(b)),
            (sa, sb) => sa.Except(sb).Union(sb.Except(sa)));

    /// <summary>
    /// Verifies that whenever <see cref="DiscreteInterval{T}.TryUnion(DiscreteInterval{T}, out DiscreteInterval{T})" />
    /// succeeds, the single-run result equals the set union of the operands over the small domain.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void TryUnion_WhenContiguous_ShouldMatchBruteForceUnion()
    {
        foreach (int a1 in Domain)
        {
            foreach (int b1 in Domain)
            {
                foreach (int a2 in Domain)
                {
                    foreach (int b2 in Domain)
                    {
                        DiscreteInterval<int> x = DiscreteInterval<int>.Closed(a1, b1);
                        DiscreteInterval<int> y = DiscreteInterval<int>.Closed(a2, b2);

                        if (x.TryUnion(y, out DiscreteInterval<int> union))
                        {
                            HashSet<int> expected = BruteForce(a1, b1);
                            expected.UnionWith(BruteForce(a2, b2));
                            Assert.IsTrue(expected.SetEquals(Members(union)), $"[{a1},{b1}] ∪ [{a2},{b2}]");
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Sweeps every ordered pair of closed intervals over the small domain, comparing the actual member set produced by
    /// <paramref name="actual" /> against the brute-force set produced by <paramref name="oracle" />.
    /// </summary>
    /// <param name="actual">Applies the interval operation and projects the result to its member set.</param>
    /// <param name="oracle">Computes the expected member set from the operand integer sets.</param>
    private static void AssertBinary(
        Func<DiscreteInterval<int>, DiscreteInterval<int>, HashSet<int>> actual,
        Func<HashSet<int>, HashSet<int>, IEnumerable<int>> oracle)
    {
        foreach (int a1 in Domain)
        {
            foreach (int b1 in Domain)
            {
                foreach (int a2 in Domain)
                {
                    foreach (int b2 in Domain)
                    {
                        var expected = oracle(BruteForce(a1, b1), BruteForce(a2, b2)).ToHashSet();
                        HashSet<int> result = actual(DiscreteInterval<int>.Closed(a1, b1), DiscreteInterval<int>.Closed(a2, b2));

                        Assert.IsTrue(expected.SetEquals(result), $"[{a1},{b1}] op [{a2},{b2}] — expected {{{string.Join(",", expected.Order())}}}, got {{{string.Join(",", result.Order())}}}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Asserts that <paramref name="interval" /> contains exactly the domain integers selected by
    /// <paramref name="oracle" />.
    /// </summary>
    /// <param name="interval">The interval under test.</param>
    /// <param name="oracle">The brute-force membership predicate.</param>
    /// <param name="label">A human-readable label for the failure message.</param>
    private static void AssertMembership(DiscreteInterval<int> interval, Func<int, bool> oracle, string label)
    {
        foreach (int x in Domain)
            Assert.AreEqual(oracle(x), interval.Contains(x), $"{label} membership of {x}");
    }

    /// <summary>
    /// Returns the set of domain integers the closed interval <c>[a, b]</c> contains.
    /// </summary>
    /// <param name="a">The inclusive lower bound.</param>
    /// <param name="b">The inclusive upper bound.</param>
    /// <returns>The brute-force integer set restricted to the domain.</returns>
    private static HashSet<int> BruteForce(int a, int b) =>
        Domain.Where(x => a <= x && x <= b).ToHashSet();

    /// <summary>
    /// Projects a discrete interval to its member set over the domain.
    /// </summary>
    /// <param name="interval">The interval to project.</param>
    /// <returns>The set of domain integers the interval contains.</returns>
    private static HashSet<int> Members(DiscreteInterval<int> interval) =>
        Domain.Where(interval.Contains).ToHashSet();

    /// <summary>
    /// Projects a discrete interval pair to the union of its runs' member sets over the domain.
    /// </summary>
    /// <param name="pair">The pair to project.</param>
    /// <returns>The set of domain integers covered by either run.</returns>
    private static HashSet<int> Members(DiscreteIntervalPair<int> pair)
    {
        var set = new HashSet<int>();
        foreach (DiscreteInterval<int> run in pair)
        {
            foreach (int x in Domain)
            {
                if (run.Contains(x))
                    set.Add(x);
            }
        }

        return set;
    }
}
