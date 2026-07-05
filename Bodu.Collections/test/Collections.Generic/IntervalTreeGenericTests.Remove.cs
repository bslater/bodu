// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTreeGenericTests.Remove.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class IntervalTreeGenericTests
{
    /// <summary>
    /// Verifies that <see cref="IntervalTree{TKey, TValue}.Remove(TKey, TKey)" /> removes the first stored entry of
    /// the exact interval and leaves later entries intact.
    /// </summary>
    [TestMethod]
    public void Remove_WhenIntervalCarriesMultipleValues_ShouldRemoveFirstStoredEntry()
    {
        var sut = CreateTree((10, 12, "design review"), (10, 12, "1:1"));

        Assert.IsTrue(sut.Remove(10, 12));

        Assert.AreEqual(1, sut.Count);
        CollectionAssert.AreEqual(new[] { (10, 12, "1:1") }, sut.ToList());
    }

    /// <summary>
    /// Verifies that <see cref="IntervalTree{TKey, TValue}.Remove(TKey, TKey)" /> deletes the node when the last
    /// entry of the interval is removed.
    /// </summary>
    [TestMethod]
    public void Remove_WhenLastEntryOfInterval_ShouldDeleteInterval()
    {
        var sut = CreateTree((10, 12, "only"), (20, 25, "other"));

        Assert.IsTrue(sut.Remove(10, 12));

        Assert.IsFalse(sut.Contains(10, 12));
        CollectionAssert.AreEqual(new[] { (20, 25, "other") }, sut.ToList());
    }

    /// <summary>
    /// Verifies that <see cref="IntervalTree{TKey, TValue}.Remove(TKey, TKey)" /> returns <see langword="false" />
    /// for an interval that is not stored.
    /// </summary>
    [TestMethod]
    public void Remove_WhenIntervalAbsent_ShouldReturnFalse()
    {
        var sut = CreateTree((10, 12, "x"));

        Assert.IsFalse(sut.Remove(10, 13));
        Assert.AreEqual(1, sut.Count);
    }

    /// <summary>
    /// Verifies that <see cref="IntervalTree{TKey, TValue}.Remove(TKey, TKey, TValue)" /> removes only the entry
    /// whose value matches under the default equality comparer.
    /// </summary>
    [TestMethod]
    public void Remove_WhenValueSpecified_ShouldRemoveMatchingEntry()
    {
        var sut = CreateTree((10, 12, "design review"), (10, 12, "1:1"), (10, 12, "retro"));

        Assert.IsTrue(sut.Remove(10, 12, "1:1"));

        Assert.AreEqual(2, sut.Count);
        CollectionAssert.AreEqual(new[] { (10, 12, "design review"), (10, 12, "retro") }, sut.ToList());
    }

    /// <summary>
    /// Verifies that <see cref="IntervalTree{TKey, TValue}.Remove(TKey, TKey, TValue)" /> removes one entry per
    /// call when the interval carries equal values.
    /// </summary>
    [TestMethod]
    public void Remove_WhenValueDuplicated_ShouldRemoveOneEntryPerCall()
    {
        var sut = CreateTree((10, 12, "shift"), (10, 12, "shift"));

        Assert.IsTrue(sut.Remove(10, 12, "shift"));
        Assert.AreEqual(1, sut.Count);

        Assert.IsTrue(sut.Remove(10, 12, "shift"));
        Assert.AreEqual(0, sut.Count);

        Assert.IsFalse(sut.Remove(10, 12, "shift"));
    }

    /// <summary>
    /// Verifies that <see cref="IntervalTree{TKey, TValue}.Remove(TKey, TKey, TValue)" /> returns
    /// <see langword="false" /> when the interval exists but carries no matching value.
    /// </summary>
    [TestMethod]
    public void Remove_WhenValueNotCarried_ShouldReturnFalse()
    {
        var sut = CreateTree((10, 12, "design review"));

        Assert.IsFalse(sut.Remove(10, 12, "1:1"));
        Assert.AreEqual(1, sut.Count);
    }

    /// <summary>
    /// Verifies that both remove overloads throw <see cref="ArgumentException" /> for a reversed endpoint pair and
    /// <see cref="ArgumentNullException" /> for a <see langword="null" /> endpoint.
    /// </summary>
    [TestMethod]
    public void Remove_WhenArgumentsInvalid_ShouldThrow()
    {
        var intTree = CreateTree((1, 5, "x"));
        var stringTree = new IntervalTree<string, int>();

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            intTree.Remove(5, 1);
        });
        Assert.AreEqual("low", ex.ParamName);

        var exValue = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            intTree.Remove(5, 1, "x");
        });
        Assert.AreEqual("low", exValue.ParamName);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            stringTree.Remove(null!, "b");
        });

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            stringTree.Remove("a", null!, 1);
        });
    }
}
