// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableSetTests.Enumeration.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableSetTests
{
    /// <summary>
    /// Verifies that the struct enumerator walks every element in ascending comparer order.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenSetHasElements_ShouldWalkAscending()
    {
        var sut = CreateSet(3, 1, 4, 1, 5, 9, 2, 6);

        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 6, 9 }, SnapshotByEnumerator(sut));
    }

    /// <summary>
    /// Verifies that enumerating an empty set yields nothing.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenSetIsEmpty_ShouldYieldNothing()
    {
        var sut = new NavigableSet<int>();

        Assert.AreEqual(0, SnapshotByEnumerator(sut).Count);
    }

    /// <summary>
    /// Verifies that adding an element mid-enumeration invalidates the enumerator, causing
    /// <see cref="InvalidOperationException" /> on the next advance.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenAddDuringEnumeration_ShouldThrowInvalidOperationException()
    {
        var sut = CreateSet(1, 2, 3);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (int item in sut)
            {
                if (item == 2)
                    sut.Add(99);
            }
        });
    }

    /// <summary>
    /// Verifies that removing an element mid-enumeration invalidates the enumerator, causing
    /// <see cref="InvalidOperationException" /> on the next advance.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenRemoveDuringEnumeration_ShouldThrowInvalidOperationException()
    {
        var sut = CreateSet(1, 2, 3);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (int item in sut)
            {
                if (item == 1)
                    sut.Remove(3);
            }
        });
    }

    /// <summary>
    /// Verifies that clearing the set mid-enumeration invalidates the enumerator, causing
    /// <see cref="InvalidOperationException" /> on the next advance.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenClearDuringEnumeration_ShouldThrowInvalidOperationException()
    {
        var sut = CreateSet(1, 2, 3);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (int item in sut)
                sut.Clear();
        });
    }

    /// <summary>
    /// Verifies that a duplicate <see cref="NavigableSet{T}.Add" /> (a rejected mutation) does not invalidate an
    /// in-flight enumerator.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenDuplicateAddDuringEnumeration_ShouldNotThrow()
    {
        var sut = CreateSet(1, 2, 3);
        var seen = new List<int>();

        foreach (int item in sut)
        {
            sut.Add(2);
            seen.Add(item);
        }

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, seen);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.Enumerator.Reset" /> restarts the walk from the smallest element.
    /// </summary>
    [TestMethod]
    public void Reset_WhenCalledMidWalk_ShouldRestartFromSmallestElement()
    {
        var sut = CreateSet(1, 2, 3);
        NavigableSet<int>.Enumerator enumerator = sut.GetEnumerator();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.IsTrue(enumerator.MoveNext());
        enumerator.Reset();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(1, enumerator.Current);
    }

    /// <summary>
    /// Verifies that the interface-typed enumerator surfaces the same ascending order as the struct enumerator.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenAccessedThroughIEnumerable_ShouldWalkAscending()
    {
        var sut = CreateSet(2, 1, 3);
        IEnumerable<int> boxed = sut;

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, boxed.ToList());
    }
}
