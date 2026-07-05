// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DisjointSetGenericTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Graphs;

/// <summary>
/// Contains unit tests for the element-keyed <see cref="DisjointSet{T}" /> type.
/// </summary>
[TestClass]
public sealed class DisjointSetGenericTests
{
    /// <summary>
    /// Verifies that unioning two elements connects them and reduces the set count.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Union_WhenElementsDiffer_ShouldConnectAndReduceSetCount()
    {
        var sut = new DisjointSet<string>();
        sut.Add("a");
        sut.Add("b");
        sut.Add("c");

        Assert.IsTrue(sut.Union("a", "c"));
        Assert.IsTrue(sut.AreConnected("a", "c"));
        Assert.AreEqual(2, sut.SetCount);
    }

    /// <summary>
    /// Verifies that constructing from a sequence adds each element as a singleton.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSeededFromItems_ShouldAddSingletons()
    {
        var sut = new DisjointSet<int>([1, 2, 3, 3]);

        Assert.AreEqual(3, sut.Count);
        Assert.AreEqual(3, sut.SetCount);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> item sequence throws <see cref="ArgumentNullException" /> naming <c>items</c>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenItemsIsNull_ShouldThrowForItems()
    {
        Assert.AreEqual(
            "items",
            Assert.ThrowsExactly<ArgumentNullException>(() => new DisjointSet<int>((IEnumerable<int>)null!)).ParamName);
    }

    /// <summary>
    /// Verifies that adding a duplicate element returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void MakeSet_WhenElementAlreadyPresent_ShouldReturnFalse()
    {
        var sut = new DisjointSet<int>();

        Assert.IsTrue(sut.MakeSet(1));
        Assert.IsFalse(sut.MakeSet(1));
        Assert.AreEqual(1, sut.Count);
    }

    /// <summary>
    /// Verifies that <see cref="DisjointSet{T}.Contains(T)" /> reports membership.
    /// </summary>
    [TestMethod]
    public void Contains_WhenElementPresent_ShouldReturnTrue()
    {
        var sut = new DisjointSet<int>([7]);

        Assert.IsTrue(sut.Contains(7));
        Assert.IsFalse(sut.Contains(8));
    }

    /// <summary>
    /// Verifies that finding an absent element throws <see cref="ArgumentException" /> naming <c>item</c>.
    /// </summary>
    [TestMethod]
    public void Find_WhenElementAbsent_ShouldThrowForItem()
    {
        var sut = new DisjointSet<int>();

        Assert.AreEqual(
            "item",
            Assert.ThrowsExactly<ArgumentException>(() => sut.Find(99)).ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="DisjointSet{T}.TryFind(T, out T)" /> reports success and the representative.
    /// </summary>
    [TestMethod]
    public void TryFind_WhenElementPresent_ShouldReturnTrueAndRepresentative()
    {
        var sut = new DisjointSet<int>([1, 2]);
        sut.Union(1, 2);

        Assert.IsTrue(sut.TryFind(2, out int rep));
        Assert.AreEqual(sut.Find(1), rep);
        Assert.IsFalse(sut.TryFind(3, out _));
    }

    /// <summary>
    /// Verifies that unioning across two existing components connects their members transitively.
    /// </summary>
    [TestMethod]
    public void Union_WhenComponentsMerge_ShouldConnectTransitively()
    {
        var sut = new DisjointSet<int>([1, 2, 3, 4]);
        sut.Union(1, 2);
        sut.Union(3, 4);
        sut.Union(2, 3);

        Assert.IsTrue(sut.AreConnected(1, 4));
        Assert.AreEqual(1, sut.SetCount);
        Assert.AreEqual(4, sut.SizeOf(1));
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> argument to <see cref="DisjointSet{T}.Union(T, T)" /> throws naming the first argument.
    /// </summary>
    [TestMethod]
    public void Union_WhenFirstArgumentIsNull_ShouldThrowForA()
    {
        var sut = new DisjointSet<string>(["x"]);

        Assert.AreEqual(
            "a",
            Assert.ThrowsExactly<ArgumentNullException>(() => sut.Union(null!, "x")).ParamName);
    }

    /// <summary>
    /// Verifies that unioning an absent element throws <see cref="ArgumentException" /> naming the offending argument.
    /// </summary>
    [TestMethod]
    public void Union_WhenSecondElementAbsent_ShouldThrowForB()
    {
        var sut = new DisjointSet<string>(["x"]);

        Assert.AreEqual(
            "b",
            Assert.ThrowsExactly<ArgumentException>(() => sut.Union("x", "missing")).ParamName);
    }

    /// <summary>
    /// Verifies that a custom equality comparer governs element identity across add, contains, and union.
    /// </summary>
    [TestMethod]
    public void Comparer_WhenCaseInsensitive_ShouldTreatEqualKeysAsSameElement()
    {
        var sut = new DisjointSet<string>(StringComparer.OrdinalIgnoreCase);
        sut.Add("Alpha");

        Assert.IsTrue(sut.Contains("alpha"));
        Assert.IsFalse(sut.Add("ALPHA"));
        Assert.AreEqual(1, sut.Count);
    }

    /// <summary>
    /// Verifies that <see cref="DisjointSet{T}.Clear" /> empties the structure.
    /// </summary>
    [TestMethod]
    public void Clear_WhenPopulated_ShouldRemoveAllElements()
    {
        var sut = new DisjointSet<int>([1, 2, 3]);
        sut.Union(1, 2);

        sut.Clear();

        Assert.AreEqual(0, sut.Count);
        Assert.AreEqual(0, sut.SetCount);
        Assert.IsFalse(sut.Contains(1));
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> element throws <see cref="ArgumentNullException" /> across the value-checking members.
    /// </summary>
    [TestMethod]
    public void Members_WhenElementIsNull_ShouldThrowForItem()
    {
        var sut = new DisjointSet<string>();

        Assert.AreEqual("item", Assert.ThrowsExactly<ArgumentNullException>(() => sut.MakeSet(null!)).ParamName);
        Assert.AreEqual("item", Assert.ThrowsExactly<ArgumentNullException>(() => sut.Contains(null!)).ParamName);
        Assert.AreEqual("item", Assert.ThrowsExactly<ArgumentNullException>(() => sut.Find(null!)).ParamName);
        Assert.AreEqual("item", Assert.ThrowsExactly<ArgumentNullException>(() => sut.SizeOf(null!)).ParamName);
    }
}
