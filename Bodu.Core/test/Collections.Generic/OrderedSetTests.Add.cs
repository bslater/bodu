// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OrderedSetTests.Add.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

public partial class OrderedSetTests
{
    // --------------------------------------------------------
    // Add(T) — argument validation
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.Add(T)" /> rejects a <see langword="null" /> reference.
    /// </summary>
    [TestMethod]
    public void Add_WhenItemIsNull_ShouldThrowArgumentNullException()
    {
        OrderedSet<string> sut = new OrderedSet<string>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.Add(null!);
        });
    }

    // --------------------------------------------------------
    // Add(T) — single-item behaviour
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that a new item is appended and the return value is <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void Add_WhenItemIsUnique_ShouldAppendAndReturnTrue()
    {
        OrderedSet<int> sut = new OrderedSet<int>();

        bool added = sut.Add(99);

        Assert.IsTrue(added);
        Assert.AreEqual(1, sut.Count);
        Assert.AreEqual(99, sut[0]);
    }

    /// <summary>
    /// Verifies that a duplicate add returns <see langword="false" /> and does not change order or count.
    /// </summary>
    [TestMethod]
    public void Add_WhenItemIsDuplicate_ShouldReturnFalseAndPreserveOrder()
    {
        OrderedSet<int> sut = CreateSet(new[] { 1, 2, 3 });

        bool added = sut.Add(2);

        Assert.IsFalse(added);
        Assert.AreEqual(3, sut.Count);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, SnapshotByIndexer(sut));
    }

    /// <summary>
    /// Verifies that adding many items grows the storage to hold them and preserves insertion order.
    /// </summary>
    [TestMethod]
    public void Add_WhenManyItemsAdded_ShouldGrowAndPreserveOrder()
    {
        OrderedSet<int> sut = new OrderedSet<int>();

        for (int i = 0; i < 1000; i++)
            Assert.IsTrue(sut.Add(i));

        Assert.AreEqual(1000, sut.Count);
        for (int i = 0; i < 1000; i++)
            Assert.AreEqual(i, sut[i]);
    }

    // --------------------------------------------------------
    // Add(T) — comparer interaction
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that a custom comparer is honoured when detecting duplicates on <see cref="OrderedSet{T}.Add(T)" />.
    /// </summary>
    [TestMethod]
    public void Add_WhenCustomComparerProvided_ShouldDeduplicateAccordingly()
    {
        OrderedSet<string> sut = new OrderedSet<string>(StringComparer.OrdinalIgnoreCase);

        Assert.IsTrue(sut.Add("Hello"));
        Assert.IsFalse(sut.Add("HELLO"));

        Assert.AreEqual(1, sut.Count);
        Assert.AreEqual("Hello", sut[0]);
    }

    // --------------------------------------------------------
    // AddRange — argument validation
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.AddRange(IEnumerable{T})" /> rejects a <see langword="null" />
    /// collection.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenCollectionIsNull_ShouldThrowArgumentNullException()
    {
        OrderedSet<int> sut = new OrderedSet<int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.AddRange(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.AddRange(IEnumerable{T})" /> rejects a source that yields a
    /// <see langword="null" /> element.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenCollectionContainsNull_ShouldThrowArgumentNullException()
    {
        OrderedSet<string> sut = new OrderedSet<string>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.AddRange(new[] { "a", null!, "b" });
        });
    }

    // --------------------------------------------------------
    // AddRange — behaviour
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.AddRange(IEnumerable{T})" /> returns the number of newly-added
    /// elements and preserves insertion order.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenCollectionContainsDuplicatesAndNewItems_ShouldReturnNewItemCount()
    {
        OrderedSet<int> sut = CreateSet(new[] { 1, 2 });

        int added = sut.AddRange(new[] { 2, 3, 4, 4, 5 });

        Assert.AreEqual(3, added);
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, SnapshotByIndexer(sut));
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.AddRange(IEnumerable{T})" /> with an empty source returns zero
    /// and leaves the set unchanged.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenCollectionIsEmpty_ShouldReturnZero()
    {
        OrderedSet<int> sut = CreateSet(new[] { 1, 2 });

        int added = sut.AddRange(Array.Empty<int>());

        Assert.AreEqual(0, added);
        CollectionAssert.AreEqual(new[] { 1, 2 }, SnapshotByIndexer(sut));
    }
}
