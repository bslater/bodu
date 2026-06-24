// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DisjointSetTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Collections.Generic.Graphs;

/// <summary>
/// Contains unit tests for the integer-indexed <see cref="DisjointSet" /> type.
/// </summary>
[TestClass]
public sealed class DisjointSetTests
{
    /// <summary>
    /// Provides known-answer scenarios exercising union sequences and connectivity outcomes.
    /// </summary>
    public static IEnumerable<object[]> UnionScenarios =>
    [
        [new DisjointSetKat("single pair merge", 3, [(0, 1)], 2, [(0, 1, true), (0, 2, false), (1, 2, false)])],
        [new DisjointSetKat("transitive chain", 4, [(0, 1), (1, 2), (2, 3)], 1, [(0, 3, true), (1, 3, true)])],
        [new DisjointSetKat("two components", 6, [(0, 1), (2, 3), (4, 5)], 3, [(0, 1, true), (0, 2, false), (4, 5, true)])],
        [new DisjointSetKat("redundant union", 3, [(0, 1), (0, 1), (1, 0)], 2, [(0, 1, true), (0, 2, false)])],
        [new DisjointSetKat("self union no-op", 4, [(2, 2), (0, 0)], 4, [(0, 1, false), (2, 3, false)])],
        [new DisjointSetKat("star merge", 5, [(0, 1), (0, 2), (0, 3), (0, 4)], 1, [(1, 4, true), (2, 3, true)])],
        [new DisjointSetKat("all singletons", 3, [], 3, [(0, 1, false), (1, 2, false)])],
    ];

    /// <summary>
    /// Verifies that unioning two elements connects them and reduces the set count.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Union_WhenElementsDiffer_ShouldConnectAndReduceSetCount()
    {
        var sut = new DisjointSet(3);

        Assert.IsTrue(sut.Union(0, 2));
        Assert.IsTrue(sut.AreConnected(0, 2));
        Assert.AreEqual(2, sut.SetCount);
    }

    /// <summary>
    /// Verifies that applying a union scenario yields the expected set count and connectivity matrix.
    /// </summary>
    /// <param name="kat">The scenario supplying the unions and expected outcomes.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(UnionScenarios),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Union_WhenScenarioApplied_ShouldMatchExpectedPartition(DisjointSetKat kat)
    {
        var sut = new DisjointSet(kat.Count);

        foreach (var (a, b) in kat.Unions)
            sut.Union(a, b);

        Assert.AreEqual(kat.ExpectedSetCount, sut.SetCount);
        foreach (var (a, b, connected) in kat.Connectivity)
            Assert.AreEqual(connected, sut.AreConnected(a, b), $"AreConnected({a}, {b})");
    }

    /// <summary>
    /// Verifies that a newly constructed set places every element in its own singleton set.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenConstructed_ShouldStartWithAllSingletons()
    {
        var sut = new DisjointSet(5);

        Assert.AreEqual(5, sut.Count);
        Assert.AreEqual(5, sut.SetCount);
        for (var i = 0; i < 5; i++)
            Assert.AreEqual(1, sut.SizeOf(i));
    }

    /// <summary>
    /// Verifies that a count of zero produces an empty structure.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCountIsZero_ShouldBeEmpty()
    {
        var sut = new DisjointSet(0);

        Assert.AreEqual(0, sut.Count);
        Assert.AreEqual(0, sut.SetCount);
    }

    /// <summary>
    /// Verifies that a negative count throws <see cref="ArgumentOutOfRangeException" /> naming <c>count</c>.
    /// </summary>
    /// <param name="count">The invalid element count.</param>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(-100)]
    public void Ctor_WhenCountIsNegative_ShouldThrowForCount(int count)
    {
        Assert.AreEqual(
            "count",
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new DisjointSet(count)).ParamName);
    }

    /// <summary>
    /// Verifies that an out-of-range element index throws <see cref="ArgumentOutOfRangeException" /> naming <c>x</c>.
    /// </summary>
    /// <param name="x">The invalid element index.</param>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(5)]
    [DataRow(6)]
    public void Find_WhenIndexOutOfRange_ShouldThrowForX(int x)
    {
        var sut = new DisjointSet(5);

        Assert.AreEqual(
            "x",
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => sut.Find(x)).ParamName);
    }

    /// <summary>
    /// Verifies that an isolated element is its own representative.
    /// </summary>
    [TestMethod]
    public void Find_WhenElementIsSingleton_ShouldReturnItself()
    {
        var sut = new DisjointSet(4);

        Assert.AreEqual(3, sut.Find(3));
    }

    /// <summary>
    /// Verifies that unioning two elements already in the same set returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void Union_WhenAlreadyConnected_ShouldReturnFalse()
    {
        var sut = new DisjointSet(3);
        sut.Union(0, 1);

        Assert.IsFalse(sut.Union(1, 0));
        Assert.AreEqual(2, sut.SetCount);
    }

    /// <summary>
    /// Verifies that unioning an element with itself returns <see langword="false" /> and changes nothing.
    /// </summary>
    [TestMethod]
    public void Union_WhenSelf_ShouldReturnFalse()
    {
        var sut = new DisjointSet(3);

        Assert.IsFalse(sut.Union(1, 1));
        Assert.AreEqual(3, sut.SetCount);
    }

    /// <summary>
    /// Verifies that <see cref="DisjointSet.SizeOf(int)" /> reports the merged subset size.
    /// </summary>
    [TestMethod]
    public void SizeOf_WhenElementsMerged_ShouldReturnSubsetSize()
    {
        var sut = new DisjointSet(5);
        sut.Union(0, 1);
        sut.Union(1, 2);

        Assert.AreEqual(3, sut.SizeOf(0));
        Assert.AreEqual(3, sut.SizeOf(2));
        Assert.AreEqual(1, sut.SizeOf(4));
    }

    /// <summary>
    /// Verifies that <see cref="DisjointSet.Reset" /> returns every element to its own singleton set.
    /// </summary>
    [TestMethod]
    public void Reset_WhenCalled_ShouldRestoreSingletons()
    {
        var sut = new DisjointSet(4);
        sut.Union(0, 1);
        sut.Union(2, 3);

        sut.Reset();

        Assert.AreEqual(4, sut.SetCount);
        Assert.IsFalse(sut.AreConnected(0, 1));
    }

    /// <summary>
    /// Verifies that a long chain of unions connects every element without overflowing the stack.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void Union_WhenChainIsLarge_ShouldConnectAllElements()
    {
        const int n = 200_000;
        var sut = new DisjointSet(n);

        for (var i = 1; i < n; i++)
            sut.Union(i - 1, i);

        Assert.AreEqual(1, sut.SetCount);
        Assert.IsTrue(sut.AreConnected(0, n - 1));
        Assert.AreEqual(n, sut.SizeOf(0));
    }
}
