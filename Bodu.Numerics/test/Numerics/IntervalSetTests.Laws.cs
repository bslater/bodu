// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalSetTests.Laws.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class IntervalSetTests
{
    // Sample integer points spanning and overflowing the endpoint universe {0..6}, so membership is probed inside,
    // at the edges, and outside every piece.
    private static readonly int[] SamplePoints = { -1, 0, 1, 2, 3, 4, 5, 6, 7 };

    /// <summary>
    /// Enumerates a representative universe of interval sets over <see cref="int" /> endpoints — empty, single-piece,
    /// disconnected, and unbounded shapes.
    /// </summary>
    /// <returns>The set universe.</returns>
    private static IEnumerable<IntervalSet<int>> SetUniverse()
    {
        yield return IntervalSet<int>.Empty;
        yield return IntervalSet<int>.Of(Interval<int>.Closed(1, 3));
        yield return IntervalSet<int>.Of(Interval<int>.Closed(2, 5));
        yield return IntervalSet<int>.Of(Interval<int>.Closed(1, 2), Interval<int>.Closed(4, 5));
        yield return IntervalSet<int>.Of(Interval<int>.ClosedOpen(0, 3), Interval<int>.Closed(4, 6));
        yield return IntervalSet<int>.Of(Interval<int>.All);
        yield return IntervalSet<int>.Of(Interval<int>.AtMost(2));
        yield return IntervalSet<int>.Of(Interval<int>.AtLeast(3));
    }

    /// <summary>
    /// Verifies, exhaustively over the set universe and a sample point grid, that
    /// <see cref="IntervalSet{T}.Union(IntervalSet{T})" />, <see cref="IntervalSet{T}.Intersect(IntervalSet{T})" />, and
    /// <see cref="IntervalSet{T}.Except(IntervalSet{T})" /> agree with the pointwise set predicates they implement.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void SetOperations_OverFiniteUniverse_ShouldSatisfyMembershipLaws()
    {
        IntervalSet<int>[] universe = SetUniverse().ToArray();

        foreach (IntervalSet<int> a in universe)
        {
            foreach (IntervalSet<int> b in universe)
            {
                IntervalSet<int> union = a.Union(b);
                IntervalSet<int> intersection = a.Intersect(b);
                IntervalSet<int> difference = a.Except(b);

                foreach (int x in SamplePoints)
                {
                    bool inA = a.Contains(x);
                    bool inB = b.Contains(x);

                    Assert.AreEqual(inA || inB, union.Contains(x), $"Union: a={a}, b={b}, x={x}");
                    Assert.AreEqual(inA && inB, intersection.Contains(x), $"Intersect: a={a}, b={b}, x={x}");
                    Assert.AreEqual(inA && !inB, difference.Contains(x), $"Except: a={a}, b={b}, x={x}");
                }
            }
        }
    }

    /// <summary>
    /// Verifies that <see cref="IntervalSet{T}.Complement" /> is the pointwise negation of membership over the line, and
    /// that applying it twice restores the original set.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Complement_OverFiniteUniverse_ShouldBePointwiseNegation()
    {
        foreach (IntervalSet<int> set in SetUniverse())
        {
            IntervalSet<int> complement = set.Complement();

            foreach (int x in SamplePoints)
                Assert.AreEqual(!set.Contains(x), complement.Contains(x), $"Complement: set={set}, x={x}");

            Assert.AreEqual(set, complement.Complement(), $"Double complement: set={set}");
        }
    }

    /// <summary>
    /// Verifies that <see cref="IntervalSet{T}.Union(IntervalSet{T})" /> is commutative, idempotent, and associative
    /// over the finite universe.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Union_OverFiniteUniverse_ShouldBeCommutativeIdempotentAndAssociative()
    {
        IntervalSet<int>[] universe = SetUniverse().ToArray();

        foreach (IntervalSet<int> a in universe)
        {
            Assert.AreEqual(a, a.Union(a), $"Idempotence: {a}");

            foreach (IntervalSet<int> b in universe)
            {
                Assert.AreEqual(a.Union(b), b.Union(a), $"Commutativity: {a}, {b}");

                foreach (IntervalSet<int> c in universe)
                    Assert.AreEqual(a.Union(b).Union(c), a.Union(b.Union(c)), $"Associativity: {a}, {b}, {c}");
            }
        }
    }

    /// <summary>
    /// Verifies that <see cref="IntervalSet{T}.Intersect(IntervalSet{T})" /> is commutative, idempotent, and associative
    /// over the finite universe.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Intersect_OverFiniteUniverse_ShouldBeCommutativeIdempotentAndAssociative()
    {
        IntervalSet<int>[] universe = SetUniverse().ToArray();

        foreach (IntervalSet<int> a in universe)
        {
            Assert.AreEqual(a, a.Intersect(a), $"Idempotence: {a}");

            foreach (IntervalSet<int> b in universe)
            {
                Assert.AreEqual(a.Intersect(b), b.Intersect(a), $"Commutativity: {a}, {b}");

                foreach (IntervalSet<int> c in universe)
                    Assert.AreEqual(a.Intersect(b).Intersect(c), a.Intersect(b.Intersect(c)), $"Associativity: {a}, {b}, {c}");
            }
        }
    }

    /// <summary>
    /// Verifies the distributive law <c>a ∩ (b ∪ c) == (a ∩ b) ∪ (a ∩ c)</c> over the finite universe.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Intersect_OverUnion_ShouldDistribute()
    {
        IntervalSet<int>[] universe = SetUniverse().ToArray();

        foreach (IntervalSet<int> a in universe)
        {
            foreach (IntervalSet<int> b in universe)
            {
                foreach (IntervalSet<int> c in universe)
                {
                    Assert.AreEqual(
                        a.Intersect(b.Union(c)),
                        a.Intersect(b).Union(a.Intersect(c)),
                        $"Distributivity: {a}, {b}, {c}");
                }
            }
        }
    }

    /// <summary>
    /// Verifies De Morgan's laws — <c>¬(a ∪ b) == ¬a ∩ ¬b</c> and <c>¬(a ∩ b) == ¬a ∪ ¬b</c> — over the finite universe,
    /// where complement is taken over the whole line.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void DeMorgan_OverFiniteUniverse_ShouldHold()
    {
        IntervalSet<int>[] universe = SetUniverse().ToArray();

        foreach (IntervalSet<int> a in universe)
        {
            foreach (IntervalSet<int> b in universe)
            {
                Assert.AreEqual(
                    a.Union(b).Complement(),
                    a.Complement().Intersect(b.Complement()),
                    $"De Morgan (union): {a}, {b}");

                Assert.AreEqual(
                    a.Intersect(b).Complement(),
                    a.Complement().Union(b.Complement()),
                    $"De Morgan (intersection): {a}, {b}");
            }
        }
    }

    /// <summary>
    /// Verifies that every set produced by a binary operation is in canonical normalized form — no empty pieces, and no
    /// two consecutive pieces that overlap or are adjacent (which would have merged during normalization).
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Normalization_AfterEveryOperation_ShouldHoldInvariant()
    {
        IntervalSet<int>[] universe = SetUniverse().ToArray();

        foreach (IntervalSet<int> a in universe)
        {
            AssertNormalized(a, $"seed {a}");

            foreach (IntervalSet<int> b in universe)
            {
                AssertNormalized(a.Union(b), $"{a} ∪ {b}");
                AssertNormalized(a.Intersect(b), $"{a} ∩ {b}");
                AssertNormalized(a.Except(b), $"{a} \\ {b}");
                AssertNormalized(a.Complement(), $"¬{a}");
            }
        }
    }

    /// <summary>
    /// Asserts that <paramref name="set" /> holds no empty pieces and that no two consecutive pieces overlap or are
    /// adjacent — i.e. no consecutive pair could be unioned into a single interval.
    /// </summary>
    /// <param name="set">The set to validate.</param>
    /// <param name="label">A human-readable label for the failure message.</param>
    private static void AssertNormalized(IntervalSet<int> set, string label)
    {
        for (int i = 0; i < set.Count; i++)
        {
            Assert.IsFalse(set[i].IsEmpty, $"{label}: empty piece at index {i}");

            if (i > 0)
            {
                Assert.IsFalse(
                    set[i - 1].TryUnion(set[i], out _),
                    $"{label}: pieces {i - 1} and {i} overlap or are adjacent and should have merged");
            }
        }
    }
}
