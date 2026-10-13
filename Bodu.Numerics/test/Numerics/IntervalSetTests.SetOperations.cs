// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalSetTests.SetOperations.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Numerics;

public partial class IntervalSetTests
{
    /// <summary>
    /// Verifies that the union with a single interval coalesces overlapping and adjacent pieces, and that the empty set
    /// is the union identity.
    /// </summary>
    [TestMethod]
    public void Union_WithInterval_ShouldCoalesceAndActAsIdentity()
    {
        IntervalSet<int> set = IntervalSet<int>.Of(Interval<int>.Closed(1, 3), Interval<int>.Closed(8, 9));

        Assert.AreEqual(
            IntervalSet<int>.Of(Interval<int>.Closed(1, 5), Interval<int>.Closed(8, 9)),
            set.Union(Interval<int>.Closed(2, 5)));

        Assert.AreEqual(
            IntervalSet<int>.Of(Interval<int>.Closed(1, 5)),
            IntervalSet<int>.Empty.Union(Interval<int>.Closed(1, 5)));
    }

    /// <summary>
    /// Verifies that the union of two sets normalizes the combined pieces.
    /// </summary>
    [TestMethod]
    public void Union_WithSet_ShouldNormalizeCombinedPieces()
    {
        IntervalSet<int> a = IntervalSet<int>.Of(Interval<int>.Closed(1, 5));
        IntervalSet<int> b = IntervalSet<int>.Of(Interval<int>.Closed(4, 8));

        Assert.AreEqual(IntervalSet<int>.Of(Interval<int>.Closed(1, 8)), a.Union(b));
    }

    /// <summary>
    /// Verifies that the intersection keeps only the shared members, over both an interval and a set.
    /// </summary>
    [TestMethod]
    public void Intersect_WithIntervalAndSet_ShouldReturnSharedMembers()
    {
        IntervalSet<int> set = IntervalSet<int>.Of(Interval<int>.Closed(1, 5), Interval<int>.Closed(8, 12));

        Assert.AreEqual(
            IntervalSet<int>.Of(Interval<int>.Closed(4, 5), Interval<int>.Closed(8, 10)),
            set.Intersect(Interval<int>.Closed(4, 10)));

        Assert.AreEqual(
            IntervalSet<int>.Of(Interval<int>.Closed(4, 5)),
            set.Intersect(IntervalSet<int>.Of(Interval<int>.Closed(4, 5))));

        Assert.IsTrue(set.Intersect(IntervalSet<int>.Empty).IsEmpty);
    }

    /// <summary>
    /// Verifies that removing an interior interval splits the affected piece.
    /// </summary>
    [TestMethod]
    public void Except_WhenRemovingInterior_ShouldSplitThePiece()
    {
        IntervalSet<int> set = IntervalSet<int>.Of(Interval<int>.Closed(1, 10)).Except(Interval<int>.Closed(3, 5));

        Assert.AreEqual(2, set.Count);
        Assert.AreEqual(Interval<int>.ClosedOpen(1, 3), set[0]);
        Assert.AreEqual(Interval<int>.OpenClosed(5, 10), set[1]);
    }

    /// <summary>
    /// Verifies that subtracting a whole set removes every one of its members.
    /// </summary>
    [TestMethod]
    public void Except_WithSet_ShouldRemoveEveryMember()
    {
        IntervalSet<int> set = IntervalSet<int>.Of(Interval<int>.Closed(1, 20));
        IntervalSet<int> remove = IntervalSet<int>.Of(Interval<int>.Closed(3, 5), Interval<int>.Closed(10, 12));

        Assert.AreEqual(
            IntervalSet<int>.Of(
                Interval<int>.ClosedOpen(1, 3),
                Interval<int>.Open(5, 10),
                Interval<int>.OpenClosed(12, 20)),
            set.Except(remove));
    }

    /// <summary>
    /// Verifies that the complement of a bounded set is the two surrounding half-lines.
    /// </summary>
    [TestMethod]
    public void Complement_WhenBoundedSet_ShouldReturnHalfLines()
    {
        IntervalSet<int> complement = IntervalSet<int>.Of(Interval<int>.Closed(1, 5)).Complement();

        Assert.AreEqual(2, complement.Count);
        Assert.AreEqual(Interval<int>.LessThan(1), complement[0]);
        Assert.AreEqual(Interval<int>.GreaterThan(5), complement[1]);
    }

    /// <summary>
    /// Verifies that the complement of the empty set is the whole line.
    /// </summary>
    [TestMethod]
    public void Complement_WhenEmptySet_ShouldReturnWholeLine()
    {
        Assert.AreEqual(IntervalSet<int>.Of(Interval<int>.All), IntervalSet<int>.Empty.Complement());
    }

    /// <summary>
    /// Verifies that the double complement returns the original set.
    /// </summary>
    [TestMethod]
    public void Complement_WhenAppliedTwice_ShouldReturnOriginal()
    {
        IntervalSet<int> set = IntervalSet<int>.Of(Interval<int>.Closed(1, 5), Interval<int>.Closed(10, 20));

        Assert.AreEqual(set, set.Complement().Complement());
    }

    /// <summary>
    /// Verifies that both Union overloads over <see cref="double" /> hold exactly the values of a catalog interval
    /// and another catalog interval or set, in disjoint, non-adjacent pieces.
    /// </summary>
    [TestMethod]
    public void Union_WhenOperandsReachInfinity_ForDouble_ShouldAgreeWithPointMembership() =>
        AssertSetUnionAgreesWithMembership<double>();

    /// <summary>
    /// Verifies that both Union overloads over <see cref="float" /> hold exactly the values of a catalog interval and
    /// another catalog interval or set, in disjoint, non-adjacent pieces.
    /// </summary>
    [TestMethod]
    public void Union_WhenOperandsReachInfinity_ForSingle_ShouldAgreeWithPointMembership() =>
        AssertSetUnionAgreesWithMembership<float>();

    /// <summary>
    /// Verifies that both Union overloads over <see cref="Half" /> hold exactly the values of a catalog interval and
    /// another catalog interval or set, in disjoint, non-adjacent pieces.
    /// </summary>
    [TestMethod]
    public void Union_WhenOperandsReachInfinity_ForHalf_ShouldAgreeWithPointMembership() =>
        AssertSetUnionAgreesWithMembership<Half>();

    /// <summary>
    /// Verifies that both Intersect overloads over <see cref="double" /> hold exactly the values a one-piece set shares
    /// with another catalog interval or set, in disjoint, non-adjacent pieces.
    /// </summary>
    [TestMethod]
    public void Intersect_WhenOperandsReachInfinity_ForDouble_ShouldAgreeWithPointMembership() =>
        AssertSetIntersectAgreesWithMembership<double>();

    /// <summary>
    /// Verifies that both Intersect overloads over <see cref="float" /> hold exactly the values a one-piece set shares
    /// with another catalog interval or set, in disjoint, non-adjacent pieces.
    /// </summary>
    [TestMethod]
    public void Intersect_WhenOperandsReachInfinity_ForSingle_ShouldAgreeWithPointMembership() =>
        AssertSetIntersectAgreesWithMembership<float>();

    /// <summary>
    /// Verifies that both Intersect overloads over <see cref="Half" /> hold exactly the values a one-piece set shares
    /// with another catalog interval or set, in disjoint, non-adjacent pieces.
    /// </summary>
    [TestMethod]
    public void Intersect_WhenOperandsReachInfinity_ForHalf_ShouldAgreeWithPointMembership() =>
        AssertSetIntersectAgreesWithMembership<Half>();

    /// <summary>
    /// Verifies that both Except overloads over <see cref="double" /> hold exactly the values of a one-piece set that
    /// another catalog interval or set lacks, in disjoint, non-adjacent pieces that each hold a value.
    /// </summary>
    [TestMethod]
    public void Except_WhenOperandsReachInfinity_ForDouble_ShouldAgreeWithPointMembership() =>
        AssertSetExceptAgreesWithMembership<double>();

    /// <summary>
    /// Verifies that both Except overloads over <see cref="float" /> hold exactly the values of a one-piece set that
    /// another catalog interval or set lacks, in disjoint, non-adjacent pieces that each hold a value.
    /// </summary>
    [TestMethod]
    public void Except_WhenOperandsReachInfinity_ForSingle_ShouldAgreeWithPointMembership() =>
        AssertSetExceptAgreesWithMembership<float>();

    /// <summary>
    /// Verifies that both Except overloads over <see cref="Half" /> hold exactly the values of a one-piece set that
    /// another catalog interval or set lacks, in disjoint, non-adjacent pieces that each hold a value.
    /// </summary>
    [TestMethod]
    public void Except_WhenOperandsReachInfinity_ForHalf_ShouldAgreeWithPointMembership() =>
        AssertSetExceptAgreesWithMembership<Half>();

    /// <summary>
    /// Verifies that Complement over <see cref="double" /> holds exactly the values of the whole line that a set of two
    /// catalog intervals lacks, in disjoint, non-adjacent pieces that each hold a value.
    /// </summary>
    [TestMethod]
    public void Complement_WhenOperandsReachInfinity_ForDouble_ShouldAgreeWithPointMembership() =>
        AssertSetComplementAgreesWithMembership<double>();

    /// <summary>
    /// Verifies that Complement over <see cref="float" /> holds exactly the values of the whole line that a set of two
    /// catalog intervals lacks, in disjoint, non-adjacent pieces that each hold a value.
    /// </summary>
    [TestMethod]
    public void Complement_WhenOperandsReachInfinity_ForSingle_ShouldAgreeWithPointMembership() =>
        AssertSetComplementAgreesWithMembership<float>();

    /// <summary>
    /// Verifies that Complement over <see cref="Half" /> holds exactly the values of the whole line that a set of two
    /// catalog intervals lacks, in disjoint, non-adjacent pieces that each hold a value.
    /// </summary>
    [TestMethod]
    public void Complement_WhenOperandsReachInfinity_ForHalf_ShouldAgreeWithPointMembership() =>
        AssertSetComplementAgreesWithMembership<Half>();

    /// <summary>
    /// Asserts that a set built by <paramref name="operation" /> from each pair of catalog intervals holds the values
    /// <paramref name="expectedMask" /> selects, in disjoint, non-adjacent pieces that each hold a value.
    /// </summary>
    /// <typeparam name="T">The floating-point endpoint type.</typeparam>
    /// <param name="name">The operation name used in failure messages.</param>
    /// <param name="operation">Builds the set under test from the two catalog intervals.</param>
    /// <param name="expectedMask">Computes the expected probe mask from the operands' probe masks.</param>
    private static void AssertSetOperationAgreesWithMembership<T>(
        string name,
        Func<Interval<T>, Interval<T>, IntervalSet<T>> operation,
        Func<int, int, int> expectedMask)
        where T : IFloatingPointIeee754<T>
    {
        Interval<T>[] catalog = IntervalMembershipOracle.Catalog<T>();
        var failures = new List<string>();

        foreach (Interval<T> a in catalog)
        {
            int aMask = IntervalMembershipOracle.Mask(a);
            foreach (Interval<T> b in catalog)
            {
                IntervalSet<T> set = operation(a, b);
                int expected = expectedMask(aMask, IntervalMembershipOracle.Mask(b));
                string? defect = IntervalMembershipOracle.FindPieceDefect(IntervalMembershipOracle.Pieces(set), requireGap: true);
                if (IntervalMembershipOracle.Mask(set) != expected || defect is not null)
                    failures.Add($"{name}({a}, {b}) returned {set} {defect}");
            }
        }

        Assert.AreEqual(0, failures.Count, string.Join("; ", failures.Take(8)));
    }

    /// <summary>
    /// Asserts that <see cref="IntervalSet{T}.Union(Interval{T})" /> and <see cref="IntervalSet{T}.Union(IntervalSet{T})" />
    /// agree with point membership over the catalog.
    /// </summary>
    /// <typeparam name="T">The floating-point endpoint type.</typeparam>
    private static void AssertSetUnionAgreesWithMembership<T>()
        where T : IFloatingPointIeee754<T>
    {
        AssertSetOperationAgreesWithMembership<T>("Union(Interval)", (a, b) => IntervalSet<T>.Of(a).Union(b), (a, b) => a | b);
        AssertSetOperationAgreesWithMembership<T>("Union(Set)", (a, b) => IntervalSet<T>.Of(a).Union(IntervalSet<T>.Of(b)), (a, b) => a | b);
    }

    /// <summary>
    /// Asserts that <see cref="IntervalSet{T}.Intersect(Interval{T})" /> and
    /// <see cref="IntervalSet{T}.Intersect(IntervalSet{T})" /> agree with point membership over the catalog.
    /// </summary>
    /// <typeparam name="T">The floating-point endpoint type.</typeparam>
    private static void AssertSetIntersectAgreesWithMembership<T>()
        where T : IFloatingPointIeee754<T>
    {
        AssertSetOperationAgreesWithMembership<T>("Intersect(Interval)", (a, b) => IntervalSet<T>.Of(a).Intersect(b), (a, b) => a & b);
        AssertSetOperationAgreesWithMembership<T>("Intersect(Set)", (a, b) => IntervalSet<T>.Of(a).Intersect(IntervalSet<T>.Of(b)), (a, b) => a & b);
    }

    /// <summary>
    /// Asserts that <see cref="IntervalSet{T}.Except(Interval{T})" /> and <see cref="IntervalSet{T}.Except(IntervalSet{T})" />
    /// agree with point membership over the catalog.
    /// </summary>
    /// <typeparam name="T">The floating-point endpoint type.</typeparam>
    private static void AssertSetExceptAgreesWithMembership<T>()
        where T : IFloatingPointIeee754<T>
    {
        AssertSetOperationAgreesWithMembership<T>("Except(Interval)", (a, b) => IntervalSet<T>.Of(a).Except(b), (a, b) => a & ~b);
        AssertSetOperationAgreesWithMembership<T>("Except(Set)", (a, b) => IntervalSet<T>.Of(a).Except(IntervalSet<T>.Of(b)), (a, b) => a & ~b);
    }

    /// <summary>
    /// Asserts that <see cref="IntervalSet{T}.Complement" /> of a set of two catalog intervals agrees with point
    /// membership relative to <see cref="Interval{T}.All" />.
    /// </summary>
    /// <typeparam name="T">The floating-point endpoint type.</typeparam>
    private static void AssertSetComplementAgreesWithMembership<T>()
        where T : IFloatingPointIeee754<T>
    {
        int wholeLine = IntervalMembershipOracle.Mask(Interval<T>.All);
        AssertSetOperationAgreesWithMembership<T>("Complement", (a, b) => IntervalSet<T>.Of(a, b).Complement(), (a, b) => wholeLine & ~(a | b));
    }
}
