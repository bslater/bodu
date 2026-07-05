// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OrderedSetStorageTests.Add.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class OrderedSetStorageTests
{

    /// <summary>
    /// Verifies that a duplicate add is a no-op that returns <see langword="false" /> and does not change
    /// element ordering.
    /// </summary>
    [TestMethod]
    public void Add_WhenItemIsDuplicate_ShouldReturnFalseAndNotChangeOrder()
    {
        OrderedSetStorage<int> sut = CreateStorage([1, 2, 3]);

        bool added = sut.Add(2);

        Assert.IsFalse(added);
        Assert.AreEqual(3, sut.Count);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, Snapshot(sut));
    }
    // --------------------------------------------------------
    // Add — argument validation
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that adding a <see langword="null" /> reference is rejected with <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Add_WhenItemIsNull_ShouldThrowExactly()
    {
        var sut = new OrderedSetStorage<string>(0, null);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            sut.Add(null!);
        });
    }

    // --------------------------------------------------------
    // Add — single-item behaviour
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that adding a previously unseen item appends it to the end of the order and returns
    /// <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void Add_WhenItemIsUnique_ShouldAppendAndReturnTrue()
    {
        var sut = new OrderedSetStorage<int>(0, null);

        bool added = sut.Add(7);

        Assert.IsTrue(added);
        Assert.AreEqual(1, sut.Count);
        Assert.AreEqual(7, sut.GetAt(0));
    }

    // --------------------------------------------------------
    // Add — sequential behaviour
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that multiple successive adds maintain insertion order.
    /// </summary>
    [TestMethod]
    public void Add_WhenItemsAreSequential_ShouldPreserveInsertionOrder()
    {
        var sut = new OrderedSetStorage<int>(0, null);

        for (int i = 10; i < 20; i++)
            Assert.IsTrue(sut.Add(i));

        Assert.AreEqual(10, sut.Count);
        for (int i = 0; i < 10; i++)
            Assert.AreEqual(10 + i, sut.GetAt(i));
    }

    /// <summary>
    /// Verifies that hash collisions are handled correctly when many items share the same bucket.
    /// </summary>
    [TestMethod]
    public void Add_WhenItemsHashCollide_ShouldKeepUniqueByReferenceIdentity()
    {
        var sut = new OrderedSetStorage<HashCollider>(0, null);
        var a = new HashCollider("a");
        var b = new HashCollider("b");
        var c = new HashCollider("c");

        Assert.IsTrue(sut.Add(a));
        Assert.IsTrue(sut.Add(b));
        Assert.IsTrue(sut.Add(c));
        Assert.IsFalse(sut.Add(a));
        Assert.IsFalse(sut.Add(b));
        Assert.IsFalse(sut.Add(c));

        Assert.AreEqual(3, sut.Count);
        CollectionAssert.AreEqual(new[] { a, b, c }, Snapshot(sut));
    }

    /// <summary>
    /// Verifies that adding items into a zero-capacity storage triggers element-array growth.
    /// </summary>
    [TestMethod]
    public void Add_WhenStorageGrowsFromZeroCapacity_ShouldGrowToHoldAllItems()
    {
        var sut = new OrderedSetStorage<int>(0, null);

        for (int i = 0; i < 100; i++)
            Assert.IsTrue(sut.Add(i));

        Assert.AreEqual(100, sut.Count);
        Assert.IsGreaterThanOrEqualTo(100, sut.Capacity);
    }

    // --------------------------------------------------------
    // Add — reference-type semantics
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that the default comparer treats two distinct reference instances with equal value as the
    /// same element by overriding <see cref="object.Equals(object)" />.
    /// </summary>
    [TestMethod]
    public void Add_WhenValueTypeEqualityIsSatisfied_ShouldDeduplicate()
    {
        var sut = new OrderedSetStorage<string>(0, null);

        Assert.IsTrue(sut.Add("alpha"));
        Assert.IsFalse(sut.Add(new string("alpha".ToCharArray())));
        Assert.AreEqual(1, sut.Count);
    }

    // --------------------------------------------------------
    // AddRange — counting behaviour
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.AddRange(IEnumerable{T})" /> returns the number of new
    /// elements added, ignoring duplicates relative to the existing contents and within the source.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenCollectionContainsDuplicatesAndNewItems_ShouldReturnNewItemCount()
    {
        OrderedSetStorage<int> sut = CreateStorage([1, 2, 3]);

        int added = sut.AddRange([2, 3, 4, 5, 5]);

        Assert.AreEqual(2, added);
        Assert.AreEqual(5, sut.Count);
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, Snapshot(sut));
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.AddRange(IEnumerable{T})" /> rejects a collection
    /// that yields a <see langword="null" /> element.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenCollectionContainsNull_ShouldThrowExactly()
    {
        var sut = new OrderedSetStorage<string>(0, null);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            sut.AddRange(["ok", null!, "later"]);
        });
    }

    /// <summary>
    /// Verifies that an empty source collection results in zero additions and no version bump.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenCollectionIsEmpty_ShouldReturnZero()
    {
        OrderedSetStorage<int> sut = CreateStorage([1, 2]);
        int versionBefore = sut._version;

        int added = sut.AddRange([]);

        Assert.AreEqual(0, added);
        Assert.AreEqual(versionBefore, sut._version);
        Assert.AreEqual(2, sut.Count);
    }

    // --------------------------------------------------------
    // AddRange — argument validation
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.AddRange(IEnumerable{T})" /> rejects a <see langword="null" /> collection.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenCollectionIsNull_ShouldThrowExactly()
    {
        var sut = new OrderedSetStorage<int>(0, null);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            sut.AddRange(null!);
        });
    }

}
