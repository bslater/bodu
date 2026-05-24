// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedSetTests.IList.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class IndexedSetTests
{

    /// <summary>
    /// Verifies that the <see cref="IList{T}" /> indexer setter reaches the underlying replace path.
    /// </summary>
    [TestMethod]
    public void IListT_Indexer_Set_WhenCalled_ShouldReplaceElement()
    {
        IndexedSet<int> sut = CreateSet([1, 2, 3]);
        IList<int> typed = sut;

        typed[1] = 99;

        CollectionAssert.AreEqual(new[] { 1, 99, 3 }, SnapshotByIndexer(sut));
    }

    /// <summary>
    /// Verifies that the <see cref="IList{T}.Insert(int, T)" /> contract reaches the underlying
    /// <see cref="IndexedSet{T}.Insert(int, T)" /> implementation.
    /// </summary>
    [TestMethod]
    public void IListT_Insert_WhenCalled_ShouldDelegateToInsert()
    {
        IndexedSet<int> sut = CreateSet([1, 2, 3]);
        IList<int> typed = sut;

        typed.Insert(1, 99);

        CollectionAssert.AreEqual(new[] { 1, 99, 2, 3 }, SnapshotByIndexer(sut));
    }

    /// <summary>
    /// Verifies that the <see cref="IList{T}.RemoveAt(int)" /> contract reaches the underlying
    /// <see cref="IndexedSet{T}.RemoveAt(int)" /> implementation.
    /// </summary>
    [TestMethod]
    public void IListT_RemoveAt_WhenCalled_ShouldDelegateToRemoveAt()
    {
        IndexedSet<int> sut = CreateSet([1, 2, 3]);
        IList<int> typed = sut;

        typed.RemoveAt(0);

        CollectionAssert.AreEqual(new[] { 2, 3 }, SnapshotByIndexer(sut));
    }
    // --------------------------------------------------------
    // IList<T> contract — typed surface
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}" /> can be assigned to <see cref="IList{T}" /> and exposes the
    /// expected indexer, count, and read/write surface.
    /// </summary>
    [TestMethod]
    public void IListT_WhenAssignedToInterface_ShouldExposeFullContract()
    {
        IList<int> typed = CreateSet([10, 20, 30]);

        Assert.HasCount(3, typed);
        Assert.AreEqual(10, typed[0]);
        Assert.AreEqual(0, typed.IndexOf(10));
        Assert.Contains(20, typed);
        Assert.IsFalse(typed.IsReadOnly);
    }

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}" /> can be assigned to <see cref="IReadOnlyList{T}" /> and
    /// exposes the expected read surface.
    /// </summary>
    [TestMethod]
    public void IReadOnlyListT_WhenAssignedToInterface_ShouldExposeReadSurface()
    {
        IReadOnlyList<int> typed = CreateSet([10, 20, 30]);

        Assert.HasCount(3, typed);
        Assert.AreEqual(10, typed[0]);
        Assert.AreEqual(20, typed[1]);
        Assert.AreEqual(30, typed[2]);
    }

}
