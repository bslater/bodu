// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedSetTests.Add.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

public partial class IndexedSetTests
{
    // --------------------------------------------------------
    // Add — argument validation
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.Add(T)" /> rejects a <see langword="null" /> reference.
    /// </summary>
    [TestMethod]
    public void Add_WhenItemIsNull_ShouldThrowArgumentNullException()
    {
        IndexedSet<string> sut = new IndexedSet<string>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.Add(null!);
        });
    }

    // --------------------------------------------------------
    // Add — behaviour
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that a new item is appended and the return value is <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void Add_WhenItemIsUnique_ShouldAppendAndReturnTrue()
    {
        IndexedSet<int> sut = new IndexedSet<int>();

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
        IndexedSet<int> sut = CreateSet(new[] { 1, 2, 3 });

        bool added = sut.Add(2);

        Assert.IsFalse(added);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, SnapshotByIndexer(sut));
    }

    /// <summary>
    /// Verifies that adding many items grows the storage to hold them and preserves insertion order.
    /// </summary>
    [TestMethod]
    public void Add_WhenManyItemsAdded_ShouldGrowAndPreserveOrder()
    {
        IndexedSet<int> sut = new IndexedSet<int>();

        for (int i = 0; i < 1000; i++)
            Assert.IsTrue(sut.Add(i));

        Assert.AreEqual(1000, sut.Count);
        for (int i = 0; i < 1000; i++)
            Assert.AreEqual(i, sut[i]);
    }

    /// <summary>
    /// Verifies that a custom comparer is honoured when detecting duplicates.
    /// </summary>
    [TestMethod]
    public void Add_WhenCustomComparerProvided_ShouldDeduplicateAccordingly()
    {
        IndexedSet<string> sut = new IndexedSet<string>(StringComparer.OrdinalIgnoreCase);

        Assert.IsTrue(sut.Add("Hello"));
        Assert.IsFalse(sut.Add("HELLO"));

        Assert.AreEqual(1, sut.Count);
        Assert.AreEqual("Hello", sut[0]);
    }

    // --------------------------------------------------------
    // AddRange — argument validation
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.AddRange(IEnumerable{T})" /> rejects a <see langword="null" /> source.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenCollectionIsNull_ShouldThrowArgumentNullException()
    {
        IndexedSet<int> sut = new IndexedSet<int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.AddRange(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.AddRange(IEnumerable{T})" /> rejects a source that yields a
    /// <see langword="null" /> element.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenCollectionContainsNull_ShouldThrowArgumentNullException()
    {
        IndexedSet<string> sut = new IndexedSet<string>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.AddRange(new[] { "a", null!, "b" });
        });
    }

    // --------------------------------------------------------
    // AddRange — behaviour
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.AddRange(IEnumerable{T})" /> returns the count of new items added
    /// while preserving insertion order.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenCollectionContainsDuplicatesAndNewItems_ShouldReturnNewItemCount()
    {
        IndexedSet<int> sut = CreateSet(new[] { 1, 2 });

        int added = sut.AddRange(new[] { 2, 3, 4, 4, 5 });

        Assert.AreEqual(3, added);
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, SnapshotByIndexer(sut));
    }

    /// <summary>
    /// Verifies that an empty source returns zero and leaves the set unchanged.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenCollectionIsEmpty_ShouldReturnZero()
    {
        IndexedSet<int> sut = CreateSet(new[] { 1, 2 });

        int added = sut.AddRange(Array.Empty<int>());

        Assert.AreEqual(0, added);
        CollectionAssert.AreEqual(new[] { 1, 2 }, SnapshotByIndexer(sut));
    }

    // --------------------------------------------------------
    // ICollection<T>.Add — explicit implementation (from IList<T>)
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that the inherited <see cref="ICollection{T}.Add(T)" /> appends a new item.
    /// </summary>
    [TestMethod]
    public void ICollectionAdd_WhenItemIsNew_ShouldAppendItem()
    {
        IndexedSet<int> sut = new IndexedSet<int>();
        ICollection<int> typed = sut;

        typed.Add(42);

        Assert.AreEqual(1, sut.Count);
        Assert.AreEqual(42, sut[0]);
    }

    /// <summary>
    /// Verifies that the inherited <see cref="ICollection{T}.Add(T)" /> discards the duplicate outcome and
    /// leaves the set unchanged.
    /// </summary>
    [TestMethod]
    public void ICollectionAdd_WhenItemIsDuplicate_ShouldLeaveSetUnchanged()
    {
        IndexedSet<int> sut = CreateSet(new[] { 1, 2, 3 });
        ICollection<int> typed = sut;

        typed.Add(2);

        Assert.AreEqual(3, sut.Count);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, SnapshotByIndexer(sut));
    }
}
