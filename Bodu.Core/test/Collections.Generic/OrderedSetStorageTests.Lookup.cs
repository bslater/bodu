// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OrderedSetStorageTests.Lookup.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class OrderedSetStorageTests
{

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.Contains(T)" /> returns <see langword="false" /> for an
    /// absent element.
    /// </summary>
    [TestMethod]
    public void Contains_WhenItemIsAbsent_ShouldReturnFalse()
    {
        OrderedSetStorage<int> sut = CreateStorage([1, 2, 3]);

        Assert.IsFalse(sut.Contains(99));
    }

    // --------------------------------------------------------
    // Contains — argument validation
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.Contains(T)" /> rejects a <see langword="null" /> item.
    /// </summary>
    [TestMethod]
    public void Contains_WhenItemIsNull_ShouldThrowExactly()
    {
        OrderedSetStorage<string> sut = CreateStorage(["a"]);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.Contains(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.Contains(T)" /> returns <see langword="true" /> for a
    /// present element.
    /// </summary>
    [TestMethod]
    public void Contains_WhenItemIsPresent_ShouldReturnTrue()
    {
        OrderedSetStorage<int> sut = CreateStorage([1, 2, 3]);

        Assert.IsTrue(sut.Contains(2));
    }

    /// <summary>
    /// Verifies that hash collisions still resolve to the correct membership answer.
    /// </summary>
    [TestMethod]
    public void Contains_WhenItemsHashCollide_ShouldResolveCorrectly()
    {
        var sut = new OrderedSetStorage<HashCollider>(0, null);
        var a = new HashCollider("a");
        var b = new HashCollider("b");
        var c = new HashCollider("c");

        sut.Add(a);
        sut.Add(b);

        Assert.IsTrue(sut.Contains(a));
        Assert.IsTrue(sut.Contains(b));
        Assert.IsFalse(sut.Contains(c));
    }

    // --------------------------------------------------------
    // Contains — basic behaviour
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.Contains(T)" /> returns <see langword="false" /> on an
    /// empty storage.
    /// </summary>
    [TestMethod]
    public void Contains_WhenStorageIsEmpty_ShouldReturnFalse()
    {
        var sut = new OrderedSetStorage<int>(0, null);

        Assert.IsFalse(sut.Contains(99));
    }
    // --------------------------------------------------------
    // GetAt
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.GetAt(int)" /> rejects an out-of-range index.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(3)]
    [DataRow(int.MinValue)]
    [DataRow(int.MaxValue)]
    public void GetAt_WhenIndexIsOutOfRange_ShouldThrowExactly(int index)
    {
        OrderedSetStorage<int> sut = CreateStorage([1, 2, 3]);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = sut.GetAt(index);
        });
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.GetAt(int)" /> returns the element at the requested
    /// insertion-order index.
    /// </summary>
    [TestMethod]
    [DataRow(0, 10)]
    [DataRow(1, 20)]
    [DataRow(2, 30)]
    public void GetAt_WhenIndexIsValid_ShouldReturnElementAtIndex(int index, int expected)
    {
        OrderedSetStorage<int> sut = CreateStorage([10, 20, 30]);

        Assert.AreEqual(expected, sut.GetAt(index));
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.GetAt(int)" /> on an empty storage with index zero
    /// throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void GetAt_WhenStorageIsEmpty_ShouldThrowExactly()
    {
        var sut = new OrderedSetStorage<int>(0, null);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = sut.GetAt(0);
        });
    }

    // --------------------------------------------------------
    // IndexOf — argument validation
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.IndexOf(T)" /> rejects a <see langword="null" /> item.
    /// </summary>
    [TestMethod]
    public void IndexOf_WhenItemIsNull_ShouldThrowExactly()
    {
        OrderedSetStorage<string> sut = CreateStorage(["a"]);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.IndexOf(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.IndexOf(T)" /> correctly resolves indices for
    /// hash-colliding items.
    /// </summary>
    [TestMethod]
    public void IndexOf_WhenItemsHashCollide_ShouldReturnCorrectIndex()
    {
        var sut = new OrderedSetStorage<HashCollider>(0, null);
        var a = new HashCollider("a");
        var b = new HashCollider("b");
        var c = new HashCollider("c");

        sut.Add(a);
        sut.Add(b);
        sut.Add(c);

        Assert.AreEqual(0, sut.IndexOf(a));
        Assert.AreEqual(1, sut.IndexOf(b));
        Assert.AreEqual(2, sut.IndexOf(c));
    }

    // --------------------------------------------------------
    // IndexOf — basic behaviour
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.IndexOf(T)" /> returns <c>-1</c> on an empty storage.
    /// </summary>
    [TestMethod]
    public void IndexOf_WhenStorageIsEmpty_ShouldReturnMinusOne()
    {
        var sut = new OrderedSetStorage<int>(0, null);

        Assert.AreEqual(-1, sut.IndexOf(99));
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.IndexOf(T)" /> returns the insertion-order index of a
    /// present element.
    /// </summary>
    [TestMethod]
    [DataRow(10, 0)]
    [DataRow(20, 1)]
    [DataRow(30, 2)]
    [DataRow(99, -1)]
    public void IndexOf_WhenStoragePopulated_ShouldReturnExpectedIndex(int item, int expected)
    {
        OrderedSetStorage<int> sut = CreateStorage([10, 20, 30]);

        Assert.AreEqual(expected, sut.IndexOf(item));
    }

}
