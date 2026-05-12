// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OrderedSetStorageTests.Versioning.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

public partial class OrderedSetStorageTests
{
    // --------------------------------------------------------
    // Version is bumped on mutating operations
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.Add(T)" /> bumps the version when the item is new.
    /// </summary>
    [TestMethod]
    public void Add_WhenItemIsNew_ShouldBumpVersion()
    {
        OrderedSetStorage<int> sut = new OrderedSetStorage<int>(0, null);
        int before = sut._version;

        sut.Add(1);

        Assert.AreNotEqual(before, sut._version);
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.Add(T)" /> does not bump the version when the item is a duplicate.
    /// </summary>
    [TestMethod]
    public void Add_WhenItemIsDuplicate_ShouldNotBumpVersion()
    {
        OrderedSetStorage<int> sut = CreateStorage(new[] { 1, 2, 3 });
        int before = sut._version;

        sut.Add(2);

        Assert.AreEqual(before, sut._version);
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.Remove(T)" /> bumps the version when an item is removed.
    /// </summary>
    [TestMethod]
    public void Remove_WhenItemPresent_ShouldBumpVersion()
    {
        OrderedSetStorage<int> sut = CreateStorage(new[] { 1, 2 });
        int before = sut._version;

        sut.Remove(1);

        Assert.AreNotEqual(before, sut._version);
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.RemoveAt(int)" /> bumps the version when called.
    /// </summary>
    [TestMethod]
    public void RemoveAt_WhenCalled_ShouldBumpVersion()
    {
        OrderedSetStorage<int> sut = CreateStorage(new[] { 1, 2 });
        int before = sut._version;

        sut.RemoveAt(0);

        Assert.AreNotEqual(before, sut._version);
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.TryInsert(int, T)" /> bumps the version when the item is new.
    /// </summary>
    [TestMethod]
    public void TryInsert_WhenItemIsNew_ShouldBumpVersion()
    {
        OrderedSetStorage<int> sut = CreateStorage(new[] { 1, 2 });
        int before = sut._version;

        sut.TryInsert(1, 99);

        Assert.AreNotEqual(before, sut._version);
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.Move(int, int)" /> bumps the version when the indices differ.
    /// </summary>
    [TestMethod]
    public void Move_WhenIndicesDiffer_ShouldBumpVersion()
    {
        OrderedSetStorage<int> sut = CreateStorage(new[] { 1, 2, 3 });
        int before = sut._version;

        sut.Move(0, 2);

        Assert.AreNotEqual(before, sut._version);
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.ReplaceAt(int, T)" /> bumps the version when the value
    /// at the index actually changes.
    /// </summary>
    [TestMethod]
    public void ReplaceAt_WhenValueChanges_ShouldBumpVersion()
    {
        OrderedSetStorage<int> sut = CreateStorage(new[] { 1, 2, 3 });
        int before = sut._version;

        sut.ReplaceAt(0, 99);

        Assert.AreNotEqual(before, sut._version);
    }

    // --------------------------------------------------------
    // Version is not bumped on read-only operations
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.Contains(T)" /> does not bump the version.
    /// </summary>
    [TestMethod]
    public void Contains_WhenCalled_ShouldNotBumpVersion()
    {
        OrderedSetStorage<int> sut = CreateStorage(new[] { 1, 2, 3 });
        int before = sut._version;

        _ = sut.Contains(2);
        _ = sut.Contains(99);

        Assert.AreEqual(before, sut._version);
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.IndexOf(T)" /> does not bump the version.
    /// </summary>
    [TestMethod]
    public void IndexOf_WhenCalled_ShouldNotBumpVersion()
    {
        OrderedSetStorage<int> sut = CreateStorage(new[] { 1, 2, 3 });
        int before = sut._version;

        _ = sut.IndexOf(2);
        _ = sut.IndexOf(99);

        Assert.AreEqual(before, sut._version);
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.GetAt(int)" /> does not bump the version.
    /// </summary>
    [TestMethod]
    public void GetAt_WhenCalled_ShouldNotBumpVersion()
    {
        OrderedSetStorage<int> sut = CreateStorage(new[] { 1, 2, 3 });
        int before = sut._version;

        _ = sut.GetAt(0);

        Assert.AreEqual(before, sut._version);
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.ToArray" /> does not bump the version.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenCalled_ShouldNotBumpVersion()
    {
        OrderedSetStorage<int> sut = CreateStorage(new[] { 1, 2, 3 });
        int before = sut._version;

        _ = sut.ToArray();

        Assert.AreEqual(before, sut._version);
    }
}
