// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableSetTests.Clear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableSetTests
{
    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.Clear" /> removes every element and resets the count.
    /// </summary>
    [TestMethod]
    public void Clear_WhenSetHasElements_ShouldEmptySet()
    {
        var sut = CreateSet(1, 2, 3);

        sut.Clear();

        Assert.AreEqual(0, sut.Count);
        Assert.IsFalse(sut.Contains(1));
        Assert.AreEqual(0, SnapshotByEnumerator(sut).Count);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.Clear" /> on an empty set is a no-op that leaves the set usable.
    /// </summary>
    [TestMethod]
    public void Clear_WhenSetIsEmpty_ShouldLeaveSetEmpty()
    {
        var sut = new NavigableSet<int>();

        sut.Clear();

        Assert.AreEqual(0, sut.Count);
    }

    /// <summary>
    /// Verifies that a cleared set accepts new elements and re-sorts them correctly.
    /// </summary>
    [TestMethod]
    public void Clear_WhenFollowedByAdds_ShouldAcceptNewElements()
    {
        var sut = CreateSet(9, 8, 7);

        sut.Clear();
        sut.Add(3);
        sut.Add(1);
        sut.Add(2);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, sut.ToList());
    }
}
