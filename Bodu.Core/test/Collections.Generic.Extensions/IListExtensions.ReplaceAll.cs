// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IListExtensions.ReplaceAll.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu.Collections.Generic.Extensions;

[TestClass]
public sealed partial class IListExtensionsTests_ReplaceAll
{
    /// <summary>
    /// Verifies that <c>ReplaceAll(oldItem, newItem)</c> replaces every matching slot and returns the count.
    /// </summary>
    [TestMethod]
    public void ReplaceAll_WithDefaultEquality_WhenMultipleMatches_ShouldReplaceAllAndReturnCount()
    {
        var list = new List<int> { 1, 2, 1, 3, 1 };

        int replaced = list.ReplaceAll(1, 9);

        Assert.AreEqual(3, replaced);
        CollectionAssert.AreEqual(new[] { 9, 2, 9, 3, 9 }, list);
    }

    /// <summary>
    /// Verifies that <c>ReplaceAll(oldItem, newItem)</c> returns <c>0</c> and leaves the list unchanged when no element matches.
    /// </summary>
    [TestMethod]
    public void ReplaceAll_WithDefaultEquality_WhenNoMatches_ShouldReturnZeroAndLeaveListUnchanged()
    {
        var list = new List<int> { 1, 2, 3 };

        int replaced = list.ReplaceAll(99, 9);

        Assert.AreEqual(0, replaced);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, list);
    }

    /// <summary>
    /// Verifies that <c>ReplaceAll(oldItem, newItem)</c> on an empty list returns <c>0</c> without throwing.
    /// </summary>
    [TestMethod]
    public void ReplaceAll_WhenListIsEmpty_ShouldReturnZero()
    {
        var list = new List<int>();

        int replaced = list.ReplaceAll(1, 2);

        Assert.AreEqual(0, replaced);
    }

    /// <summary>
    /// Verifies that <c>ReplaceAll</c> treats <see langword="null"/> equality consistently when the element type is a reference type.
    /// </summary>
    [TestMethod]
    public void ReplaceAll_WithDefaultEquality_WhenReplacingNullReferences_ShouldReplaceAll()
    {
        var list = new List<string?> { "a", null, "b", null };

        int replaced = list.ReplaceAll(null, "z");

        Assert.AreEqual(2, replaced);
        CollectionAssert.AreEqual(new[] { "a", "z", "b", "z" }, list);
    }

    /// <summary>
    /// Verifies that <c>ReplaceAll(oldItem, newItem, comparer)</c> uses the supplied comparer.
    /// </summary>
    [TestMethod]
    public void ReplaceAll_WithCustomComparer_ShouldUseComparerForMatching()
    {
        var list = new List<string> { "abc", "ABC", "xyz" };

        int replaced = list.ReplaceAll("abc", "ZZZ", StringComparer.OrdinalIgnoreCase);

        Assert.AreEqual(2, replaced);
        CollectionAssert.AreEqual(new[] { "ZZZ", "ZZZ", "xyz" }, list);
    }

    /// <summary>
    /// Verifies that passing a <see langword="null"/> comparer falls back to <see cref="EqualityComparer{T}.Default"/>.
    /// </summary>
    [TestMethod]
    public void ReplaceAll_WithNullComparer_ShouldFallBackToDefaultEquality()
    {
        var list = new List<int> { 1, 2, 1 };

        int replaced = list.ReplaceAll(1, 9, comparer: null);

        Assert.AreEqual(2, replaced);
        CollectionAssert.AreEqual(new[] { 9, 2, 9 }, list);
    }

    /// <summary>
    /// Verifies that <c>ReplaceAll(newItem, predicate)</c> replaces every predicate match.
    /// </summary>
    [TestMethod]
    public void ReplaceAll_WithPredicate_WhenMultipleMatches_ShouldReplaceAllAndReturnCount()
    {
        var list = new List<int> { 1, 4, 5, 8, 9 };

        int replaced = list.ReplaceAll(0, x => x % 2 == 0);

        Assert.AreEqual(2, replaced);
        CollectionAssert.AreEqual(new[] { 1, 0, 5, 0, 9 }, list);
    }

    /// <summary>
    /// Verifies that <c>ReplaceAll(newItem, predicate)</c> does not loop indefinitely when the predicate also
    /// matches <paramref name="newItem"/>, because each slot is tested exactly once.
    /// </summary>
    [TestMethod]
    public void ReplaceAll_WithPredicate_WhenPredicateMatchesNewItem_ShouldNotInfiniteLoop()
    {
        var list = new List<int> { 1, 2, 3 };

        int replaced = list.ReplaceAll(99, _ => true);

        Assert.AreEqual(3, replaced);
        CollectionAssert.AreEqual(new[] { 99, 99, 99 }, list);
    }

    /// <summary>
    /// Verifies that <c>ReplaceAll(oldItem, newItem)</c> throws <see cref="ArgumentNullException"/> when the list is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void ReplaceAll_WithDefaultEquality_WhenListIsNull_ShouldThrowArgumentNullException()
    {
        IList<int>? list = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = list!.ReplaceAll(1, 2);
        });
    }

    /// <summary>
    /// Verifies that <c>ReplaceAll(newItem, predicate)</c> throws <see cref="ArgumentNullException"/> when the list is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void ReplaceAll_WithPredicate_WhenListIsNull_ShouldThrowArgumentNullException()
    {
        IList<int>? list = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = list!.ReplaceAll(0, x => true);
        });
    }

    /// <summary>
    /// Verifies that <c>ReplaceAll(newItem, predicate)</c> throws <see cref="ArgumentNullException"/> when the predicate is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void ReplaceAll_WithPredicate_WhenPredicateIsNull_ShouldThrowArgumentNullException()
    {
        IList<int> list = new List<int> { 1 };
        Func<int, bool>? predicate = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = list.ReplaceAll(0, predicate!);
        });
    }
}
