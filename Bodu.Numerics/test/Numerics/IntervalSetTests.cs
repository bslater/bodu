// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalSetTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Numerics;

/// <summary>
/// Verifies the <see cref="IntervalSet{T}" /> normalized disconnected-range type - its construction, membership,
/// enumeration, and formatting. The N-ary set algebra and equality are covered in the sibling partials.
/// </summary>
[TestClass]
public partial class IntervalSetTests
{
    /// <summary>
    /// Verifies that overlapping and adjacent inputs are coalesced into disjoint, non-adjacent pieces on construction.
    /// </summary>
    [TestMethod]
    public void Of_WhenInputsOverlapOrTouch_ShouldCoalesce()
    {
        IntervalSet<int> set = IntervalSet<int>.Of(
            Interval<int>.Closed(1, 3),
            Interval<int>.Closed(2, 5),
            Interval<int>.Closed(8, 9));

        Assert.AreEqual(2, set.Count);
        Assert.AreEqual(Interval<int>.Closed(1, 5), set[0]);
        Assert.AreEqual(Interval<int>.Closed(8, 9), set[1]);
    }

    /// <summary>
    /// Verifies that a point excluded by both neighbors keeps the pieces separate.
    /// </summary>
    [TestMethod]
    public void Of_WhenPointExcludedByBoth_ShouldStaySeparate()
    {
        IntervalSet<int> set = IntervalSet<int>.Of(
            Interval<int>.ClosedOpen(1, 2),
            Interval<int>.OpenClosed(2, 3));

        Assert.AreEqual(2, set.Count);
        Assert.IsFalse(set.Contains(2));
    }

    /// <summary>
    /// Verifies that empty inputs are dropped and an all-empty input yields the empty set.
    /// </summary>
    [TestMethod]
    public void Of_WhenInputsAreEmpty_ShouldIgnoreThem()
    {
        Assert.IsTrue(IntervalSet<int>.Of(Interval<int>.Empty, Interval<int>.Empty).IsEmpty);
        Assert.AreEqual(
            IntervalSet<int>.Of(Interval<int>.Closed(1, 5)),
            IntervalSet<int>.Of(Interval<int>.Empty, Interval<int>.Closed(1, 5)));
    }

    /// <summary>
    /// Verifies that <see cref="IntervalSet{T}.From(IEnumerable{Interval{T}})" /> normalizes a sequence the same way as
    /// the params overload.
    /// </summary>
    [TestMethod]
    public void From_WhenGivenSequence_ShouldNormalizeLikeOf()
    {
        var pieces = new List<Interval<int>>
        {
            Interval<int>.Closed(8, 9),
            Interval<int>.Closed(1, 3),
            Interval<int>.Closed(2, 5),
        };

        Assert.AreEqual(
            IntervalSet<int>.Of(Interval<int>.Closed(1, 5), Interval<int>.Closed(8, 9)),
            IntervalSet<int>.From(pieces));
    }

    /// <summary>
    /// Verifies that <see cref="IntervalSet{T}.Of(Interval{T}[])" /> throws <see cref="ArgumentNullException" /> for a
    /// <see langword="null" /> array.
    /// </summary>
    [TestMethod]
    public void Of_WhenIntervalsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = IntervalSet<int>.Of(null!);
        });

        Assert.AreEqual("intervals", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="IntervalSet{T}.From(IEnumerable{Interval{T}})" /> throws
    /// <see cref="ArgumentNullException" /> for a <see langword="null" /> sequence.
    /// </summary>
    [TestMethod]
    public void From_WhenSequenceNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = IntervalSet<int>.From(null!);
        });

        Assert.AreEqual("intervals", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="IntervalSet{T}.Contains(T)" /> and <see cref="IntervalSet{T}.Encloses(Interval{T})" />
    /// reflect membership across the disconnected pieces.
    /// </summary>
    [TestMethod]
    public void ContainsAndEncloses_WhenAcrossPieces_ShouldReflectMembership()
    {
        IntervalSet<int> set = IntervalSet<int>.Of(Interval<int>.Closed(1, 5), Interval<int>.Closed(8, 9));

        Assert.IsTrue(set.Contains(3));
        Assert.IsFalse(set.Contains(6));
        Assert.IsTrue(set.Encloses(Interval<int>.Closed(2, 4)));
        Assert.IsFalse(set.Encloses(Interval<int>.Closed(4, 8)));   // spans the gap
        Assert.IsTrue(set.Encloses(Interval<int>.Empty));            // the empty interval is enclosed by any set
    }

    /// <summary>
    /// Verifies that the indexer returns pieces lowest-first and throws for an out-of-range index.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenIndexOutOfRange_ShouldThrowArgumentOutOfRangeException()
    {
        IntervalSet<int> set = IntervalSet<int>.Of(Interval<int>.Closed(1, 5), Interval<int>.Closed(8, 9));

        Assert.AreEqual(Interval<int>.Closed(1, 5), set[0]);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = set[2];
        });
    }

    /// <summary>
    /// Verifies that enumeration yields the disjoint pieces in ascending order.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenEnumerated_ShouldYieldPiecesInOrder()
    {
        IntervalSet<int> set = IntervalSet<int>.Of(Interval<int>.Closed(8, 9), Interval<int>.Closed(1, 5));

        var enumerated = new List<Interval<int>>();
        foreach (Interval<int> piece in set)
            enumerated.Add(piece);

        CollectionAssert.AreEqual(new[] { Interval<int>.Closed(1, 5), Interval<int>.Closed(8, 9) }, enumerated);
    }

    /// <summary>
    /// Verifies that the empty set reports zero pieces and the empty-set glyph.
    /// </summary>
    [TestMethod]
    public void Empty_WhenInspected_ShouldReportNoPieces()
    {
        IntervalSet<int> empty = IntervalSet<int>.Empty;

        Assert.IsTrue(empty.IsEmpty);
        Assert.AreEqual(0, empty.Count);
        Assert.AreEqual("∅", empty.ToString());
    }

    /// <summary>
    /// Verifies that a non-empty set renders its pieces joined by the union symbol.
    /// </summary>
    [TestMethod]
    public void ToString_WhenNonEmpty_ShouldJoinPiecesWithUnionSymbol()
    {
        IntervalSet<int> set = IntervalSet<int>.Of(Interval<int>.Closed(1, 5), Interval<int>.Closed(8, 9));

        Assert.AreEqual("[1, 5] ∪ [8, 9]", set.ToString());
    }

    /// <summary>
    /// Verifies that Of over <see cref="double" /> builds, from every pair of catalog intervals, a set that holds
    /// exactly their values in disjoint, non-adjacent pieces, so an unbounded side and a bounded infinite endpoint
    /// coalesce only where they meet.
    /// </summary>
    [TestMethod]
    public void Of_WhenOperandsReachInfinity_ForDouble_ShouldAgreeWithPointMembership() =>
        AssertOfAgreesWithMembership<double>();

    /// <summary>
    /// Verifies that Of over <see cref="float" /> builds, from every pair of catalog intervals, a set that holds
    /// exactly their values in disjoint, non-adjacent pieces, so an unbounded side and a bounded infinite endpoint
    /// coalesce only where they meet.
    /// </summary>
    [TestMethod]
    public void Of_WhenOperandsReachInfinity_ForSingle_ShouldAgreeWithPointMembership() =>
        AssertOfAgreesWithMembership<float>();

    /// <summary>
    /// Verifies that Of over <see cref="Half" /> builds, from every pair of catalog intervals, a set that holds
    /// exactly their values in disjoint, non-adjacent pieces, so an unbounded side and a bounded infinite endpoint
    /// coalesce only where they meet.
    /// </summary>
    [TestMethod]
    public void Of_WhenOperandsReachInfinity_ForHalf_ShouldAgreeWithPointMembership() =>
        AssertOfAgreesWithMembership<Half>();

    /// <summary>
    /// Verifies that Encloses over <see cref="double" /> reports every catalog interval a one-piece set holds
    /// entirely, and only those, so an unbounded side does not enclose a bounded infinite endpoint.
    /// </summary>
    [TestMethod]
    public void Encloses_WhenOperandsReachInfinity_ForDouble_ShouldAgreeWithPointMembership() =>
        AssertEnclosesAgreesWithMembership<double>();

    /// <summary>
    /// Verifies that Encloses over <see cref="float" /> reports every catalog interval a one-piece set holds
    /// entirely, and only those, so an unbounded side does not enclose a bounded infinite endpoint.
    /// </summary>
    [TestMethod]
    public void Encloses_WhenOperandsReachInfinity_ForSingle_ShouldAgreeWithPointMembership() =>
        AssertEnclosesAgreesWithMembership<float>();

    /// <summary>
    /// Verifies that Encloses over <see cref="Half" /> reports every catalog interval a one-piece set holds entirely,
    /// and only those, so an unbounded side does not enclose a bounded infinite endpoint.
    /// </summary>
    [TestMethod]
    public void Encloses_WhenOperandsReachInfinity_ForHalf_ShouldAgreeWithPointMembership() =>
        AssertEnclosesAgreesWithMembership<Half>();

    /// <summary>
    /// Asserts that <see cref="IntervalSet{T}.Of(Interval{T}[])" /> builds, from every pair of catalog intervals, a
    /// set that holds exactly their values in disjoint, non-adjacent pieces.
    /// </summary>
    /// <typeparam name="T">The floating-point endpoint type.</typeparam>
    private static void AssertOfAgreesWithMembership<T>()
        where T : IFloatingPointIeee754<T>
    {
        Interval<T>[] catalog = IntervalMembershipOracle.Catalog<T>();
        var failures = new List<string>();

        foreach (Interval<T> a in catalog)
        {
            int aMask = IntervalMembershipOracle.Mask(a);
            foreach (Interval<T> b in catalog)
            {
                var set = IntervalSet<T>.Of(a, b);
                string? defect = IntervalMembershipOracle.FindPieceDefect(IntervalMembershipOracle.Pieces(set), requireGap: true);
                if (IntervalMembershipOracle.Mask(set) != (aMask | IntervalMembershipOracle.Mask(b)) || defect is not null)
                    failures.Add($"Of({a}, {b}) returned {set} {defect}");
            }
        }

        Assert.AreEqual(0, failures.Count, string.Join("; ", failures.Take(8)));
    }

    /// <summary>
    /// Asserts that <see cref="IntervalSet{T}.Encloses(Interval{T})" /> on a one-piece set reports every catalog
    /// interval whose values the set holds, and only those.
    /// </summary>
    /// <typeparam name="T">The floating-point endpoint type.</typeparam>
    private static void AssertEnclosesAgreesWithMembership<T>()
        where T : IFloatingPointIeee754<T>
    {
        Interval<T>[] catalog = IntervalMembershipOracle.Catalog<T>();
        var failures = new List<string>();

        foreach (Interval<T> a in catalog)
        {
            int aMask = IntervalMembershipOracle.Mask(a);
            var set = IntervalSet<T>.Of(a);
            foreach (Interval<T> b in catalog)
            {
                bool expected = (IntervalMembershipOracle.Mask(b) & ~aMask) == 0;
                if (set.Encloses(b) != expected)
                    failures.Add($"{set}.Encloses({b}) returned {!expected}");
            }
        }

        Assert.AreEqual(0, failures.Count, string.Join("; ", failures.Take(8)));
    }

    /// <summary>
    /// Verifies that building a set from two catalog intervals over <see cref="double" />, <see cref="float" /> and
    /// <see cref="Half" /> gives an equal set in either order, including where an unbounded side meets an open endpoint
    /// at the same infinity.
    /// </summary>
    [TestMethod]
    public void Of_WhenOperandsReachInfinity_ShouldNotDependOnOperandOrder()
    {
        AssertOfIsCommutative<double>();
        AssertOfIsCommutative<float>();
        AssertOfIsCommutative<Half>();
    }

    /// <summary>
    /// Asserts that <see cref="IntervalSet{T}.Of(Interval{T}[])" /> builds equal sets from every pair of catalog
    /// intervals in either order.
    /// </summary>
    /// <typeparam name="T">The floating-point endpoint type.</typeparam>
    private static void AssertOfIsCommutative<T>()
        where T : IFloatingPointIeee754<T>
    {
        Interval<T>[] catalog = IntervalMembershipOracle.Catalog<T>();
        var failures = new List<string>();

        foreach (Interval<T> a in catalog)
        {
            foreach (Interval<T> b in catalog)
            {
                var forward = IntervalSet<T>.Of(a, b);
                var reverse = IntervalSet<T>.Of(b, a);
                if (!forward.Equals(reverse))
                    failures.Add($"Of({a}, {b}) is {forward}, but Of({b}, {a}) is {reverse}");
            }
        }

        Assert.AreEqual(0, failures.Count, string.Join("; ", failures.Take(8)));
    }
}
